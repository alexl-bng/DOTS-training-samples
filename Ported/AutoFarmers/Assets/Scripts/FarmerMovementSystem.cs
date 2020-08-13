﻿using System.Collections;
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
        Grid grid = GetSingleton<Grid>();
        Entity gridEntity = GetSingletonEntity<Grid>();
        BufferFromEntity<GridSectionReference> sectionRefBuffer = GetBufferFromEntity<GridSectionReference>(true);
        BufferFromEntity<GridTile> tileBufferMap = GetBufferFromEntity<GridTile>(true);

        var deltaTime = Time.DeltaTime;
        var ecb = m_ecb.CreateCommandBuffer().AsParallelWriter();
        
        Entities
            .WithName("farmer_movement")
            .WithAll<Farmer, Path>()
            .WithReadOnly(sectionRefBuffer)
            .WithReadOnly(tileBufferMap)
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
                
                    int2 nextPosInt2 = new int2((int) nextLocation.x, (int) nextLocation.z);
                    
                    int sectionRefId = grid.GetSectionId(nextPosInt2);
                    int tileIndex = grid.GetTileIndex(nextPosInt2);
                    Entity sectionEntity = sectionRefBuffer[gridEntity][sectionRefId].SectionEntity;
                    DynamicBuffer<GridTile> tileBuffer = tileBufferMap[sectionEntity];
                    GridTile tile = tileBuffer[tileIndex];

                    if (tile.OccupationType == OccupationType.Rock)
                    {
                        ecb.RemoveComponent<WorkerIntent_None>(entityInQueryIndex, entity);
                        
                        if (!HasComponent<WorkerIntent_Break>(entity))
                        {
                            ecb.AddComponent(entityInQueryIndex, entity, new WorkerIntent_Break());
							path.sourcePosition = ltw.Position;
                            ecb.AddComponent(entityInQueryIndex, tile.OccupyingEntity, new WorkerIntent_Break());
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
