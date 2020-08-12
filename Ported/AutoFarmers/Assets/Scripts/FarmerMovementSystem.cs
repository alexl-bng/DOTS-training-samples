using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public class FarmerMovementSystem : SystemBase
{
    private EntityCommandBufferSystem m_ecb;
    private EntityQuery m_rockQuery;
    
    protected override void OnCreate()
    {
        m_rockQuery = GetEntityQuery(new EntityQueryDesc
        {
            All = new[]
            {
                ComponentType.ReadOnly<Rock>(),
                ComponentType.ReadOnly<Translation>(),
                ComponentType.ReadOnly<Scale>()
            }
        });
        
        m_ecb = World.GetExistingSystem<EndSimulationEntityCommandBufferSystem>();
    }
    
    protected override void OnUpdate()
    {
        var rockTranslations = m_rockQuery.ToComponentDataArrayAsync<Translation>(Allocator.Temp, out var rockTranslationsHandle);
        var rockScales = m_rockQuery.ToComponentDataArrayAsync<Scale>(Allocator.Temp, out var rockScalesHandle);
        var deltaTime = Time.DeltaTime;
        Dependency = JobHandle.CombineDependencies(Dependency, rockTranslationsHandle, rockScalesHandle);
        var ecb = m_ecb.CreateCommandBuffer();
        
        Entities
            .WithName("farmer_movement")
            .WithAll<Farmer, Path>()
            .WithoutBurst()
            .WithDisposeOnCompletion(rockTranslations)
            .WithDisposeOnCompletion(rockScales)
            .ForEach((Entity entity, ref Path path, in LocalToWorld ltw) =>
            {
                if (math.abs(path.targetPosition.x - ltw.Position.x) < 0.01 &&
                    math.abs(path.targetPosition.z - ltw.Position.z) < 0.01)
                {
                    ecb.AddComponent(entity, new PathComplete());
                }
                else
                {
                    ecb.RemoveComponent<PathComplete>(entity);
                    // TODO: account for smoothing
                    Vector3 nextLocation = ltw.Position;
                    
                    if (math.abs(path.targetPosition.x - ltw.Position.x) > 0.01)
                    {    // move along X
                        nextLocation =
                            ltw.Position + new float3(path.targetPosition.x > ltw.Position.x ? 1f : -1f, 0, 0);
                    }
                    else if (math.abs(path.targetPosition.z - ltw.Position.z) > 0.01) 
                    {    // move along Z
                        nextLocation =
                            ltw.Position + new float3(0, 0, path.targetPosition.z > ltw.Position.z ? 1f : -1f);
                    }
                
                    if (IsObstructionPresent(rockTranslations, rockScales, nextLocation))
                    {
                        ecb.AddComponent(entity, new WorkerIntent_Break());
                    }
                    else
                    {
                        ecb.AddComponent(entity, new Translation()
                        {
                            Value = nextLocation
                        });
                    }
                
                    var distanceFromSource = Vector3.Distance(path.sourcePosition, path.targetPosition);
                    path.progress = (Vector3.Distance(ltw.Position, path.targetPosition) / distanceFromSource) * 100.0f;
                }
                
                // draw a straight line from the current position to the target
                Debug.DrawLine(new Vector3(ltw.Position.x, 0.1f, ltw.Position.z),
                    new Vector3(path.targetPosition.x, 0.1f, path.targetPosition.z), Color.red);
            }
        ).Run();
    }

    private bool IsObstructionPresent(NativeArray<Translation> translations, NativeArray<Scale> scales, float3 position)
    {
        for (int i = 0; i < translations.Length; i++)
        {
            Translation tl = translations[i];
            Scale scale = scales[i];
            
            if (math.abs(tl.Value.x - position.x) < 0.01 &&
                math.abs(tl.Value.z - position.z) < 0.01)
            {
                return true;
            }
        }
        
        return false;
    }
}
