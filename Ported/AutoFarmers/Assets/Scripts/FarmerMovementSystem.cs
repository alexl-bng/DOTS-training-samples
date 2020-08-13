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
                ComponentType.ReadOnly<LocalToWorld>(),
                ComponentType.ReadOnly<GridOccupant>()
            }
        });
        
        m_ecb = World.GetExistingSystem<EndSimulationEntityCommandBufferSystem>();
    }
    
    protected override void OnUpdate()
    {
        var rockEntities = m_rockQuery.ToEntityArrayAsync(Allocator.TempJob, out var rockEntitiesHandle);
        var rockLocations = m_rockQuery.ToComponentDataArrayAsync<LocalToWorld>(Allocator.TempJob, out var rockLocationsHandle);
        var rockOccupants = m_rockQuery.ToComponentDataArrayAsync<GridOccupant>(Allocator.TempJob, out var rockOccupantsHandle);

        m_rockQuery.CompleteDependency();
        var deltaTime = Time.DeltaTime;
        Dependency = JobHandle.CombineDependencies(Dependency, rockEntitiesHandle);
		Dependency = JobHandle.CombineDependencies(Dependency, rockLocationsHandle);
		Dependency = JobHandle.CombineDependencies(Dependency, rockOccupantsHandle);
        var ecb = m_ecb.CreateCommandBuffer().AsParallelWriter();
        
        Entities
            .WithName("farmer_movement")
            .WithAll<Farmer, Path>()
			.WithDisposeOnCompletion(rockLocations)
			.WithDisposeOnCompletion(rockOccupants)
			.WithDisposeOnCompletion(rockEntities)
			.ForEach((int entityInQueryIndex, Entity entity, ref Path path, in LocalToWorld ltw) =>
            {
                if (math.abs(path.targetPosition.x - ltw.Position.x) < 0.1 &&
                    math.abs(path.targetPosition.z - ltw.Position.z) < 0.1)
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
                
                    if (FarmerUtils.IsObstructionPresent(ref rockEntities, ref rockLocations, ref rockOccupants, nextLocation, ref ecb, entityInQueryIndex))
                    {
                        ecb.RemoveComponent<WorkerIntent_None>(entityInQueryIndex, entity);
                        
                        if (!HasComponent<WorkerIntent_Break>(entity))
                        {
                            ecb.AddComponent(entityInQueryIndex, entity, new WorkerIntent_Break());
							path.sourcePosition = ltw.Position;
                        }
                    }
                    else
                    {
                        if (HasComponent<WorkerIntent_Break>(entity))
                        {
                            ecb.RemoveComponent<WorkerIntent_Break>(entityInQueryIndex, entity);
							
							ecb.SetComponent(entityInQueryIndex, entity, new Translation()
                            {
                                Value = path.sourcePosition
                            });
                        }
                        
                        ecb.SetComponent(entityInQueryIndex, entity, new Translation()
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
        ).ScheduleParallel();
		
		m_ecb.AddJobHandleForProducer(Dependency);
	}

    
}

public class FarmerUtils
{
    public static bool IsObstructionPresent(ref NativeArray<Entity> entities, ref NativeArray<LocalToWorld> locations, 
        ref NativeArray<GridOccupant> occupants, float3 position, ref EntityCommandBuffer.ParallelWriter ecb, int entityInQueryIndex)
    {
        for (int i = 0; i < entities.Length; i++)
        {
            LocalToWorld location = locations[i];
            GridOccupant occupant = occupants[i];
            float4 bounds;
            
            bounds.w = location.Position.x;
            bounds.x = location.Position.x + occupant.GridSize.x;
            bounds.y = location.Position.z;
            bounds.z = location.Position.z + occupant.GridSize.y;
            
            if (bounds.w <= position.x && 
                position.x <= bounds.x && 
                bounds.y <= position.z && 
                position.z <= bounds.z)
            {
                ecb.AddComponent(entityInQueryIndex, entities[i], new WorkerIntent_Break());
                return true;
            }
        }

        return false;
    }
}