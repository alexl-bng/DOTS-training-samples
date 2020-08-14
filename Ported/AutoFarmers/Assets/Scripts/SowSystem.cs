using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

public class SowSystem_PathComplete : SystemBase
{
	private EntityQuery m_gridQuery;

	protected override void OnCreate()
	{		
		m_gridQuery = EntityManager.CreateEntityQuery(typeof(Grid));
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
		BufferFromEntity<GridTile> tileBufferMap = GetBufferFromEntity<GridTile>();
		PlantConfig config = GetSingleton<PlantConfig>();

		Entities
			.WithAll<PathComplete>()
			.ForEach((
				Entity entity, 
				ref Path path,
				ref WorkerIntent_Sow sow) =>
		{
			int sectionRefId = grid.GetSectionId(sow.TargetTilePos);
			int tileIndex = grid.GetTileIndex(sow.TargetTilePos);
			Entity sectionEntity = sectionRefBuffer[gridEntity][sectionRefId].SectionEntity;
			DynamicBuffer<GridTile> tileBuffer = tileBufferMap[sectionEntity];
			GridTile tile = tileBuffer[tileIndex];

			if (tile.IsPlowed && tile.OccupationType == OccupationType.Unoccupied)
			{
				Entity newPlantEntity = ecb.Instantiate(config.PlantPrefab);
				ecb.AddComponent(newPlantEntity, new Plant());
				ecb.SetComponent(newPlantEntity, new Translation { Value = new float3(sow.TargetTilePos.x, 0.0f, sow.TargetTilePos.y) });
				ecb.AddComponent<PlantStateGrowing>(newPlantEntity);
				ecb.SetComponent(newPlantEntity, new PlantStateGrowing { GrowthProgress = 0.1f });
				ecb.AddComponent(newPlantEntity, new GridLocation { Value = sow.TargetTilePos });

				bool foundNeighbor = false;					
				int2 neighborLocation = sow.TargetTilePos + sow.ContinueDirection;

				if (grid.IsValidGridLocation(neighborLocation))
				{
					GridTile neighborTile = WorkerIntentUtils.GetTileAtPos(
						neighborLocation.x,
						neighborLocation.y,
						ref grid,
						gridEntity,
						ref sectionRefBuffer,
						ref tileBufferMap);

					if (neighborTile.IsPlowed && neighborTile.OccupationType == OccupationType.Unoccupied)
					{
						foundNeighbor = true;
					}
				}

				if (foundNeighbor)
				{
					sow.TargetTilePos = neighborLocation;
					path.targetPosition = new float3(neighborLocation.x, 0, neighborLocation.y);
					ecb.RemoveComponent<PathComplete>(entity);
				}
				else
				{
					ecb.RemoveComponent<WorkerIntent_Sow>(entity);
					ecb.AddComponent<WorkerIntent_None>(entity);
				}
			}
			else
			{
				ecb.RemoveComponent<WorkerIntent_Sow>(entity);
				ecb.AddComponent<WorkerIntent_None>(entity);
			}
		}).Schedule();

		ecbSystem.AddJobHandleForProducer(Dependency);
	}
}

