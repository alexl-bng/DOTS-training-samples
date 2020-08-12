using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public class FarmerMovementSystem : SystemBase
{
    private EntityCommandBufferSystem m_ecb;

    protected override void OnCreate()
    {
        m_ecb = World.GetExistingSystem<EndSimulationEntityCommandBufferSystem>();
    }
    
    protected override void OnUpdate()
    {
        var deltaTime = Time.DeltaTime;
        var ecb = m_ecb.CreateCommandBuffer();

        Entities
            .WithName("farmer_movement")
            .WithAll<Farmer, Path>()
            .WithoutBurst()
            .ForEach((Entity entity, Path path, in LocalToWorld ltw) =>
            {
                // TODO: account for smoothing
                if (math.abs(path.targetPosition.x - ltw.Position.x) > 0.01)
                {    // move along X
                    ecb.AddComponent(entity, new Translation()
                    {
                        Value = ltw.Position + new float3(path.targetPosition.x > ltw.Position.x ? 1f : -1f, 0, 0)
                    });
                }
                else if (math.abs(path.targetPosition.z - ltw.Position.z) > 0.01) 
                {    // move along Z
                    ecb.AddComponent(entity, new Translation()
                    {
                        Value = ltw.Position + new float3(0, 0, path.targetPosition.z > ltw.Position.z ? 1f : -1f)
                    });    
                }
                
                var distanceFromSource = Vector3.Distance(path.sourcePosition, path.targetPosition);
                path.progress = Vector3.Distance(ltw.Position, path.targetPosition) / distanceFromSource * 100.0f;
                
                // draw a straight line from the current position to the target
                Debug.DrawLine(new Vector3(ltw.Position.x, 0.1f, ltw.Position.z),
                    new Vector3(path.targetPosition.x, 0.1f, path.targetPosition.z), Color.red);
            }
        ).Run();
    }
}
