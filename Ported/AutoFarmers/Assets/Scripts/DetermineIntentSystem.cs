﻿using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Jobs;
using UnityEngine;

public class DetermineIntentSystem_Farmer : SystemBase
{
	private EntityQuery m_plantQuery;
	private EntityQuery m_gridQuery;

	protected override void OnCreate()
	{
		m_plantQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new[]
			{
				ComponentType.ReadOnly<Plant>(),
				ComponentType.ReadOnly<PlantStateGrown>(),
				ComponentType.ReadOnly<Translation>()
			}
		});


		m_gridQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new[]
			{
				ComponentType.ReadOnly<Grid>()
			}
		});


	}


	protected override void OnUpdate()
	{
		int numGrids = m_gridQuery.CalculateEntityCount();
		if (numGrids == 0) return;
		EntityCommandBufferSystem ecbSystem = World.GetExistingSystem<EndInitializationEntityCommandBufferSystem>();
		EntityCommandBuffer ecb = ecbSystem.CreateCommandBuffer();

		Grid grid = GetSingleton<Grid>();
		Entity gridEntity = GetSingletonEntity<Grid>();
		BufferFromEntity<GridSectionReference> sectionRefBuffer = GetBufferFromEntity<GridSectionReference>();
		BufferFromEntity<GridTile> tileBuffer = GetBufferFromEntity<GridTile>();

		NativeArray<Translation> plantTranslations = m_plantQuery.ToComponentDataArrayAsync<Translation>(Allocator.TempJob, out JobHandle plantTranslationHandle);
		NativeArray<Entity> plantEntities = m_plantQuery.ToEntityArrayAsync(Allocator.TempJob, out JobHandle plantEntitiesHandle);

		Dependency = JobHandle.CombineDependencies(Dependency, plantTranslationHandle, plantEntitiesHandle);

		JobHandle foreachHandle = Entities
			.WithAll<Farmer, WorkerIntent_None>()
			.WithDisposeOnCompletion(plantTranslations)
			.WithDisposeOnCompletion(plantEntities)
			.ForEach((
				Entity entity,
				ref Path path,
				ref RandomNumberGenerator rng,
				in Translation workerTranslation) =>
		{
			//UnityEngine.Debug.Log("thinking...");
			bool switchedToState = false;
			int nextStateIndex = rng.rng.NextInt(0, 3);
			switch (nextStateIndex)
			{
				case 0:
					//UnityEngine.Debug.Log("Harvesting...");
					switchedToState = WorkerIntentUtils.SwitchToHarvestIntent(ecb, entity, plantTranslations, plantEntities);
					break;
				case 1:
					//UnityEngine.Debug.Log("sowing...");
					switchedToState = WorkerIntentUtils.SwitchToSowIntent(ecb, entity, workerTranslation, ref grid, gridEntity, ref sectionRefBuffer, ref tileBuffer, rng, ref path);
					break;
				case 2:
					//UnityEngine.Debug.Log("plowing...");
					switchedToState = WorkerIntentUtils.SwitchToPlowIntent(ecb, entity, workerTranslation, ref grid, gridEntity, ref sectionRefBuffer, ref tileBuffer, rng, ref path);
					break;
				case 3:
					//UnityEngine.Debug.Log("breaking...");
					switchedToState = WorkerIntentUtils.SwitchToBreakIntent(ecb, entity);
					break;

			}

			if (switchedToState)
			{
				ecb.RemoveComponent<WorkerIntent_None>(entity);
			}
			
		}).Schedule(Dependency);
		
		Dependency = foreachHandle;

		ecbSystem.AddJobHandleForProducer(Dependency);
	}
}


public class DetermineIntentSystem_Drone: SystemBase
{

	private EntityQuery m_plantQuery;
	private EntityQuery m_gridQuery;

	protected override void OnCreate()
	{
		m_plantQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new[]
			{
				ComponentType.ReadOnly<Plant>(),
				ComponentType.ReadOnly<Translation>()
			}
		});
	}

	protected override void OnUpdate()
	{
		EntityCommandBufferSystem ecbSystem = World.GetExistingSystem<EndInitializationEntityCommandBufferSystem>();
		EntityCommandBuffer ecb = ecbSystem.CreateCommandBuffer();

		NativeArray<Translation> plantTranslations = m_plantQuery.ToComponentDataArrayAsync<Translation>(Allocator.TempJob, out JobHandle plantTranslationHandle);
		NativeArray<Entity> plantEntities = m_plantQuery.ToEntityArrayAsync(Allocator.TempJob, out JobHandle plantEntitiesHandle);

		Dependency = JobHandle.CombineDependencies(Dependency, plantTranslationHandle, plantEntitiesHandle);

		JobHandle foreachHandle = Entities
			.WithAll<Drone, WorkerIntent_None>()
			.WithDisposeOnCompletion(plantTranslations)
			.WithDisposeOnCompletion(plantEntities)
			.ForEach((
				Entity entity,
				ref RandomNumberGenerator rng) =>
			{
				bool switchedToState = false;
				int nextStateIndex = rng.rng.NextInt(0, 1);
				switch (nextStateIndex)
				{
					case 0:
						switchedToState = WorkerIntentUtils.SwitchToHarvestIntent(ecb, entity, plantTranslations, plantEntities);
						break;

				}
				if (switchedToState)
				{
					ecb.RemoveComponent<WorkerIntent_None>(entity);
				}
			}).Schedule(Dependency);

		Dependency = foreachHandle;

		ecbSystem.AddJobHandleForProducer(Dependency);
	}
}


class WorkerIntentUtils
{
	public static bool SwitchToHarvestIntent(EntityCommandBuffer ecb, Entity entity, NativeArray<Translation> plantTranslations, NativeArray<Entity> plantEntities)
	{
		ecb.AddComponent<WorkerIntent_HarvestSearch>(entity);
		return true;
		//return false;
		if (plantEntities.Length == 0 || plantEntities[0] == Entity.Null)
		{
			return false;
		}
		//reserve?

		ecb.AddComponent<WorkerIntent_Harvest>(entity);
		ecb.SetComponent(entity, new WorkerIntent_Harvest { PlantEntity = plantEntities[0] });
		ecb.RemoveComponent<PlantStateGrown>(plantEntities[0]);
		return true;
	}

	public static bool SwitchToBreakIntent(EntityCommandBuffer ecb, Entity entity)
	{
		return false;
		//find a rock to kill. Maybe just tile location instead?		
		//ecb.AddComponent<SmashTarget>(entity);
		//ecb.SetComponent<SmashTarget>(foundRockEntity);
		ecb.AddComponent<WorkerIntent_Break>(entity);
	}

	public static bool SwitchToSowIntent(
		EntityCommandBuffer ecb,
		Entity workerEntity,
		Translation workerTranslation,
		ref Grid grid,
		Entity gridEntity,
		ref BufferFromEntity<GridSectionReference> sectionRefBuffer,
		ref BufferFromEntity<GridTile> tileBuffer,
		RandomNumberGenerator rng,
		ref Path path)
	{

		int2 workerGridLoc = new int2((int)math.floor(workerTranslation.Value.x), (int)math.floor(workerTranslation.Value.z));

		bool targetFound = false;
		int2 targetLocation = new int2();
		int2 targetRowDirection = new int2();

		int attempts = 3;
		int sectionSearchRadius = 2;

		while (!targetFound && attempts > 0)
		{
			attempts--;

			int sectionId = -1;

			for (int pickSectionAttempt = 0; pickSectionAttempt < 10; pickSectionAttempt++)
			{
				int2 locOffset = grid.SectionDimensions * new int2(rng.rng.NextInt(-sectionSearchRadius, sectionSearchRadius + 1), rng.rng.NextInt(-sectionSearchRadius, sectionSearchRadius + 1));
				if (grid.IsValidGridLocation(workerGridLoc + locOffset))
				{
					sectionId = grid.GetSectionId(workerGridLoc + locOffset);
					break;
				}
			}

			if (sectionId != -1)
			{
				Entity sectionEntity = sectionRefBuffer[gridEntity][sectionId].SectionEntity;
				DynamicBuffer<GridTile> sectionTiles = tileBuffer[sectionEntity];

				int bufferSize = sectionTiles.Length;

				int searchStart = rng.rng.NextInt(0, bufferSize);
				for (int tileOffset = 0; tileOffset < bufferSize; tileOffset++)
				{
					int tileIndex = (searchStart + tileOffset) % bufferSize;
					GridTile tile = sectionTiles[tileIndex];

					if (tile.IsPlowed && tile.OccupationType == OccupationType.Unoccupied)
					{
						targetFound = true;
						targetLocation = grid.GetGridLocationFromIndices(sectionId, tileIndex);

						if (rng.rng.NextBool())
						{
							targetRowDirection = new int2(rng.rng.NextBool() ? -1 : 1, 0);
						}
						else
						{
							targetRowDirection = new int2(0, rng.rng.NextBool() ? -1 : 1);
						}

						int rowTries = 10;
						while (rowTries > 0)
						{
							rowTries--;

							int2 tryLocation = targetLocation - targetRowDirection;
							if (grid.IsValidGridLocation(tryLocation))
							{
								sectionId = grid.GetSectionId(tryLocation);
								tileIndex = grid.GetTileIndex(tryLocation);
								sectionEntity = sectionRefBuffer[gridEntity][sectionId].SectionEntity;
								sectionTiles = tileBuffer[sectionEntity];

								tile = sectionTiles[tileIndex];

								if (tile.IsPlowed && tile.OccupationType == OccupationType.Unoccupied)
								{
									targetLocation = tryLocation;
								}
								else
								{
									rowTries = 0;
								}
							}
							else
							{
								rowTries = 0;
							}
						}
						
						break;
					}
				}
			}
		}

		if (targetFound)
		{
			ecb.AddComponent(workerEntity, new WorkerIntent_Sow
			{
				TargetTilePos = targetLocation,
				ContinueDirection = targetRowDirection
			});

			path.targetPosition = new float3(targetLocation.x, 0, targetLocation.y);
			ecb.RemoveComponent<PathComplete>(workerEntity);
			
			return true;
		}
		else
		{
			return false;
		}
	}

	public static bool SwitchToPlowIntent(
		EntityCommandBuffer ecb, 
		Entity workerEntity,
		Translation workerTranslation,
		ref Grid grid, 
		Entity gridEntity, 
		ref BufferFromEntity<GridSectionReference> sectionRefBuffer, 
		ref BufferFromEntity<GridTile> tileBuffer,
		RandomNumberGenerator rng,
		ref Path path)
	{
		int fieldMinWidth = 2;
		int fieldMinHeight = 2;
		int fieldTargetWidth = rng.rng.NextInt(fieldMinWidth, 8);
		int fieldTargetHeight = rng.rng.NextInt(fieldMinHeight, 8);

		bool fieldFound = false;
		int2 baseLoc = new int2();
		int2 fieldSize = new int2();

		int2 workerGridLoc = new int2((int)math.floor(workerTranslation.Value.x), (int)math.floor(workerTranslation.Value.z));

		int attempts = 10;
		int sectionSearchRadius = 2;

		while (!fieldFound && attempts > 0)
		{
			attempts--;

			int sectionId = -1;

			for (int pickSectionAttempt = 0; pickSectionAttempt < 10; pickSectionAttempt++)
			{
				int2 locOffset = grid.SectionDimensions * new int2(rng.rng.NextInt(-sectionSearchRadius, sectionSearchRadius + 1), rng.rng.NextInt(-sectionSearchRadius, sectionSearchRadius + 1));
				if (grid.IsValidGridLocation(workerGridLoc + locOffset))
				{
					sectionId = grid.GetSectionId(workerGridLoc + locOffset);
					break;
				}
			}

			if (sectionId != -1)
			{
				Entity sectionEntity = sectionRefBuffer[gridEntity][sectionId].SectionEntity;
				DynamicBuffer<GridTile> sectionTiles = tileBuffer[sectionEntity];

				int bufferSize = sectionTiles.Length;

				int searchStart = rng.rng.NextInt(0, bufferSize);
				for (int tileOffset = 0; tileOffset < bufferSize; tileOffset++)
				{
					int tileIndex = (searchStart + tileOffset) % bufferSize;
					GridTile tile = sectionTiles[tileIndex];

					if (!tile.IsPlowed && tile.OccupationType == OccupationType.Unoccupied)
					{
						baseLoc = grid.GetGridLocationFromIndices(sectionId, tileIndex);

						int fieldWidth = fieldTargetWidth;
						int fieldHeight = fieldTargetHeight;

						bool doExpand = true;

						for (int expandX = 0; doExpand && expandX < fieldWidth; expandX++)
						{
							for (int expandZ = 0; doExpand && expandZ < fieldHeight; expandZ++)
							{
								int2 tileLoc = new int2(baseLoc.x + expandX, baseLoc.y + expandZ);

								if (grid.IsValidGridLocation(tileLoc))
								{
									tile = GetTileAtPos(tileLoc.x, tileLoc.y, ref grid, gridEntity, ref sectionRefBuffer, ref tileBuffer);

									if (tile.IsPlowed || tile.OccupationType != OccupationType.Unoccupied)
									{
										doExpand = false;

										if (expandX >= fieldMinWidth)
										{
											fieldWidth = expandX;
											fieldFound = true;
										}
									}
								}
							}
						}

						if (fieldFound)
						{
							fieldSize = new int2(fieldWidth, fieldHeight);
						}

						break;
					}
				}
			}
		}

		if (fieldFound)
		{
			//ecb.AddComponent<WorkerIntent_Plow>(workerEntity);
			ecb.AddComponent(workerEntity, new WorkerIntent_Plow
			{
				BaseLoc = baseLoc,
				FieldSize = fieldSize,
				CurrentIndex = 0
			});

			//Debug.Log($"plowing field with base loc {baseLoc} size {fieldSize}");

			path.targetPosition = new float3(baseLoc.x, 0, baseLoc.y);
			ecb.RemoveComponent<PathComplete>(workerEntity);

			return true;
		}
		else
		{
			return false;
		}
	}


	public static GridTile GetTileAtPos(int x, int y, ref Grid grid, Entity gridEntity, ref BufferFromEntity<GridSectionReference> sectionRefBuffer, ref BufferFromEntity<GridTile> tileBuffer)
	{
		int2 pos = new int2(x, y);
		int sectionRefId = grid.GetSectionId(pos);
		int tileIndex = grid.GetTileIndex(pos);
		Entity sectionEntity = sectionRefBuffer[gridEntity][sectionRefId].SectionEntity;
		GridTile tile = tileBuffer[sectionEntity][tileIndex];
		return tile;
	}

}





