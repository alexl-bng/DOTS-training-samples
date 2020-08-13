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
                ComponentType.ReadOnly<LocalToWorld>()
            }
        });
        
        m_ecb = World.GetExistingSystem<EndSimulationEntityCommandBufferSystem>();
    }
    
    protected override void OnUpdate()
    {
        var rockEntities = m_rockQuery.ToEntityArrayAsync(Allocator.TempJob, out var rockEntitiesHandle);
        var rockLocations = m_rockQuery.ToComponentDataArrayAsync<LocalToWorld>(Allocator.TempJob, out var rockLocationsHandle);
        m_rockQuery.CompleteDependency();
        var deltaTime = Time.DeltaTime;
        Dependency = JobHandle.CombineDependencies(Dependency, rockEntitiesHandle, rockLocationsHandle);
        var ecb = m_ecb.CreateCommandBuffer();
        
        Entities
            .WithName("farmer_movement")
            .WithAll<Farmer, Path>()
            //.WithDisposeOnCompletion(rockTranslations)
            //.WithDisposeOnCompletion(rockScales)
            //.WithDisposeOnCompletion(rockEntities)
            .ForEach((Entity entity, ref Path path, in LocalToWorld ltw) =>
            {
                if (math.abs(path.targetPosition.x - ltw.Position.x) < 0.1 &&
                    math.abs(path.targetPosition.z - ltw.Position.z) < 0.1)
                {
                    if (!HasComponent<PathComplete>(entity))
                    {
                        ecb.AddComponent(entity, new PathComplete());
                    }
                }
                else
                {
                    if (HasComponent<PathComplete>(entity))
                    {
                        ecb.RemoveComponent<PathComplete>(entity);
                    }

                    // TODO: account for smoothing
                    Vector3 nextLocation = ltw.Position;
                    
                    // TODO: account for grid bounds
                    if (math.abs(path.targetPosition.x - ltw.Position.x) > 0.1)
                    {    // move along X
                        nextLocation =
                            ltw.Position + new float3((path.targetPosition.x > ltw.Position.x ? 1f : -1f) * (deltaTime * path.speed)
                                , 0, 0);
                    }
                    else if (math.abs(path.targetPosition.z - ltw.Position.z) > 0.1) 
                    {    // move along Z
                        nextLocation =
                            ltw.Position + new float3(0, 0, (path.targetPosition.z > ltw.Position.z ? 1f : -1f) * (deltaTime * path.speed));
                    }
                
                    if (FarmerUtils.IsObstructionPresent(rockEntities, rockLocations, nextLocation, ref ecb))//, out Entity rockEntity))
                    {
                        ecb.RemoveComponent<WorkerIntent_None>(entity);
                        ecb.AddComponent(entity, new WorkerIntent_Break());
                    }
                    else
                    {
                        if (HasComponent<WorkerIntent_Break>(entity))
                        {
                            ecb.RemoveComponent<WorkerIntent_Break>(entity);
                        }
                        
                        ecb.SetComponent(entity, new Translation()
                        {
                            Value = nextLocation
                        });
                    }
                
                    var distanceFromSource = math.distance(path.sourcePosition, path.targetPosition);
                    path.progress = distanceFromSource < 0.1f
                        ? 100.0f
                        : ((math.distance(ltw.Position, path.targetPosition) / distanceFromSource) * 100.0f);
                }
                
                // draw a straight line from the current position to the target
                Debug.DrawLine(new Vector3(ltw.Position.x, 0.1f, ltw.Position.z),
                    new Vector3(path.targetPosition.x, 0.1f, path.targetPosition.z), Color.red);
            }
        ).Run();
        
        rockLocations.Dispose();
        rockEntities.Dispose();
    }

    
}

public class FarmerUtils
{
    public static bool IsObstructionPresent(NativeArray<Entity> entities, NativeArray<LocalToWorld> locations, float3 position, ref EntityCommandBuffer ecb)
    {
        for (int i = 0; i < entities.Length; i++)
        {
            LocalToWorld location = locations[i];
            
            if (math.abs(location.Position.x - position.x) < 0.01 &&
                math.abs(location.Position.z - position.z) < 0.01)
            {
                ecb.AddComponent(entities[i], new WorkerIntent_Break());
                return true;
            }
        }

        return false;
    }
}