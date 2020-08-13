using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public class DroneMovementSystem : SystemBase
{
    private EntityCommandBufferSystem m_ecb;

    protected override void OnCreate()
    {
        m_ecb = World.GetExistingSystem<EndSimulationEntityCommandBufferSystem>();
    }

    protected override void OnUpdate()
    {
        var ecb = m_ecb.CreateCommandBuffer().AsParallelWriter();
        var deltaTime = Time.DeltaTime;
        
        Entities
            .WithName("drone_movement")
            .WithAll<Drone, Path>()
            .ForEach((int entityInQueryIndex, Entity entity, ref Path path, in LocalToWorld ltw) =>
                {
                    // Drones make a direct bee line to their target
                    // TODO: account for smoothing
                    if (math.abs(path.targetPosition.x - ltw.Position.x) < 0.1f &&
                        math.abs(path.targetPosition.z - ltw.Position.z) < 0.1f)
                    {
                        if (!HasComponent<PathComplete>(entity))
                        {
                            ecb.AddComponent(entityInQueryIndex, entity, new PathComplete());    
                        }
                    }
                    else
                    {
                        if (HasComponent<PathComplete>(entity))
                        {
                            ecb.RemoveComponent<PathComplete>(entityInQueryIndex, entity);
                        }
                        
                        ecb.AddComponent(entityInQueryIndex, entity,  new Translation()
                        {
                            Value = Vector3.MoveTowards(ltw.Position, path.targetPosition, path.speed * deltaTime)
                        });

                        var distanceFromSource = math.distance(path.sourcePosition, path.targetPosition);
                        path.progress = math.distance(ltw.Position, path.targetPosition) / distanceFromSource * 100.0f;
                        
                        // draw a straight line from the current position to the target
                        Debug.DrawLine(new Vector3(ltw.Position.x, 0.1f, ltw.Position.z),
                            new Vector3(path.targetPosition.x, 0.1f, path.targetPosition.z), Color.red);
                    }
                }).ScheduleParallel();
        
        m_ecb.AddJobHandleForProducer(Dependency);
    }
}