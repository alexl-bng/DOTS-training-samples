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
				in WorkerIntent_Sow plow) =>
		{
			int sectionRefId = grid.GetSectionId(plow.TargetTilePos);
			int tileIndex = grid.GetTileIndex(plow.TargetTilePos);
			Entity sectionEntity = sectionRefBuffer[gridEntity][sectionRefId].SectionEntity;
			DynamicBuffer<GridTile> tileBuffer = tileBufferMap[sectionEntity];
			GridTile tile = tileBuffer[tileIndex];
			//UnityEngine.Debug.Log("Sown!");
			Entity newPlantEntity = ecb.Instantiate(config.PlantPrefab);
			ecb.AddComponent(newPlantEntity, new Plant());
			ecb.SetComponent(newPlantEntity, new Translation { Value = new float3(plow.TargetTilePos.x, 0.0f, plow.TargetTilePos.y) });
			ecb.AddComponent<PlantStateGrowing>(newPlantEntity);
			ecb.SetComponent(newPlantEntity, new PlantStateGrowing { GrowthProgress = 0.1f });
			//ecb.AddComponent(newPlantEntity, 
			//tile.IsPlowed = true;
			//tile.RenderTileDirty = true;
			tileBuffer[tileIndex] = tile;
			ecb.RemoveComponent<WorkerIntent_Sow>(entity);
			ecb.AddComponent<WorkerIntent_None>(entity);
			//? ecb.AddComponent<GridSectionDirty>(sectionEntity);
			ecb.RemoveComponent<PathComplete>(entity);
		}).Schedule();

		ecbSystem.AddJobHandleForProducer(Dependency);
	}
}

