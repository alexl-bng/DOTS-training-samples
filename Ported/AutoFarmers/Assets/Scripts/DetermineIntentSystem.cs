using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Jobs;

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
				ref RandomNumberGenerator rng) =>
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
					switchedToState = WorkerIntentUtils.SwitchToSowIntent(ecb, entity, ref grid, gridEntity, ref sectionRefBuffer, ref tileBuffer, rng, ref path);
					break;
				case 2:
					//UnityEngine.Debug.Log("plowing...");
					switchedToState = WorkerIntentUtils.SwitchToPlowIntent(ecb, entity, ref grid, gridEntity, ref sectionRefBuffer, ref tileBuffer, rng, ref path);
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
		ref Grid grid,
		Entity gridEntity,
		ref BufferFromEntity<GridSectionReference> sectionRefBuffer,
		ref BufferFromEntity<GridTile> tileBuffer,
		RandomNumberGenerator rng,
		ref Path path)
	{
		int2 worldDim = grid.GetWorldDimensions();

		int tries = 10;

		bool foundTile = false;
		int x;
		int y;
		do
		{
			tries--;
			x = rng.rng.NextInt(0, worldDim.x);
			y = rng.rng.NextInt(0, worldDim.y);
			GridTile tile = GetTileAtPos(x, y, ref grid, gridEntity, ref sectionRefBuffer, ref tileBuffer);
			foundTile = tile.IsPlowed && tile.OccupationType == OccupationType.Unoccupied;
			} while (!foundTile && tries > 0);

		if (foundTile)
		{			
			//reserve?
			ecb.AddComponent<WorkerIntent_Sow>(workerEntity);
			ecb.SetComponent(workerEntity, new WorkerIntent_Sow
			{
				TargetTilePos = new int2(x, y)
			});

			path.targetPosition = new float3(x, 0, y);
		}
		else
		{
			return false;
		}

		return true;
	}

	public static bool SwitchToPlowIntent(
		EntityCommandBuffer ecb, 
		Entity workerEntity,
		ref Grid grid, 
		Entity gridEntity, 
		ref BufferFromEntity<GridSectionReference> sectionRefBuffer, 
		ref BufferFromEntity<GridTile> tileBuffer,
		RandomNumberGenerator rng,
		ref Path path)
	{
		//tjtj: maybe someday
		//int fieldWidth = rng.rng.NextInt(0, 8);
		//int fieldHeight = rng.rng.NextInt(0, 8);
		//ecb.AddComponent<FieldDimensions>(entity);
		//ecb.SetComponent(entity, new FieldDimensions {Width = fieldWidth, Height = fieldHeight });

		int2 worldDim = grid.GetWorldDimensions();

		int tries = 10;

		bool foundTile = false;
		int x;
		int y;
		do
		{
			tries--;
			x = rng.rng.NextInt(0, worldDim.x);
			y = rng.rng.NextInt(0, worldDim.y);
			GridTile tile = GetTileAtPos(x, y, ref grid, gridEntity, ref sectionRefBuffer, ref tileBuffer);
			foundTile = !tile.IsPlowed && tile.OccupationType == OccupationType.Unoccupied;
		} while (!foundTile && tries>0);

		if (foundTile)
		{
			//reserve?
			ecb.AddComponent<WorkerIntent_Plow>(workerEntity);
			ecb.SetComponent(workerEntity, new WorkerIntent_Plow
			{
				TargetTilePos = new int2(x, y)
			});

			path.targetPosition = new float3(x, 0, y);
		}
		else
		{
			return false;
		}

		return true;
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





