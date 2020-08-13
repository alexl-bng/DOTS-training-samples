using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;



public class PlowSystem_PathComplete : SystemBase
{
	private EntityQuery m_gridQuery;

	protected override void OnCreate()
	{
		Enabled = false;
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

		Entities
			.WithAll<PathComplete>()
			.ForEach((
				Entity entity, 
				in WorkerIntent_Plow plow) =>
		{
			int sectionRefId = grid.GetSectionId(plow.TargetTilePos);
			int tileIndex = grid.GetTileIndex(plow.TargetTilePos);
			Entity sectionEntity = sectionRefBuffer[gridEntity][sectionRefId].SectionEntity;
			DynamicBuffer<GridTile> tileBuffer = tileBufferMap[sectionEntity];
			GridTile tile = tileBuffer[tileIndex];
			tile.IsPlowed = true;
			tile.RenderTileDirty = true;
			tileBuffer[tileIndex] = tile;
			ecb.RemoveComponent<WorkerIntent_Plow>(entity);
			ecb.AddComponent<WorkerIntent_None>(entity);
			ecb.AddComponent<GridSectionDirty>(sectionEntity);
			ecb.RemoveComponent<PathComplete>(entity);
		}).Schedule();

		ecbSystem.AddJobHandleForProducer(Dependency);
	}
}



public class PlowSystem_DistanceCheck : SystemBase
{
	private EntityQuery m_gridQuery;

	protected override void OnCreate()
	{
		Enabled = true;
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

		Entities
			.ForEach((
				Entity entity,
				ref RandomNumberGenerator rng,
				in WorkerIntent_Plow plow,
				in Path path,
				in LocalToWorld ltw) =>
			{
				float3 flattenedWorldPos = new float3(ltw.Position.x, 0.0f, ltw.Position.z);
				float3 targetTileWorldPos = new float3(plow.TargetTilePos.x, 0, plow.TargetTilePos.y);
				float3 diff = flattenedWorldPos - targetTileWorldPos;
				float dist = math.length(diff);
				//UnityEngine.Debug.Log(string.Format("{0} {1} {2} {3} {4}", dist, ltw.Position.x, ltw.Position.z, plow.TargetTilePos.x, plow.TargetTilePos.y));
				//UnityEngine.Debug.Log(string.Format("Progress: {0}", path.progress));
				if (dist < 0.1f)
				{
					int sectionRefId = grid.GetSectionId(plow.TargetTilePos);
					int tileIndex = grid.GetTileIndex(plow.TargetTilePos);
					Entity sectionEntity = sectionRefBuffer[gridEntity][sectionRefId].SectionEntity;
					DynamicBuffer<GridTile> tileBuffer = tileBufferMap[sectionEntity];
					GridTile tile = tileBuffer[tileIndex];
					tile.IsPlowed = true;
					tile.RenderTileDirty = true;
					tileBuffer[tileIndex] = tile;
					ecb.RemoveComponent<WorkerIntent_Plow>(entity);
					ecb.AddComponent<WorkerIntent_None>(entity);
					ecb.AddComponent<GridSectionDirty>(sectionEntity);

				}

			}).Schedule();

		ecbSystem.AddJobHandleForProducer(Dependency);
	}
}
