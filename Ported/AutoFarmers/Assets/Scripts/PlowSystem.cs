using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

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
			int sectionRefId = grid.GetSectionId(plow.BaseLoc);
			int tileIndex = grid.GetTileIndex(plow.BaseLoc);
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
				ref WorkerIntent_Plow plow,
				ref Path path,
				in Translation translation) =>
			{
				Debug.Log($"updating plow with field base loc {plow.BaseLoc} size {plow.FieldSize} index {plow.CurrentIndex}");

				int targetX = plow.CurrentIndex / plow.FieldSize.y;
				int targetZ = (targetX % 2 == 0) ? plow.CurrentIndex % plow.FieldSize.y : plow.FieldSize.y - 1 - (plow.CurrentIndex % plow.FieldSize.y);
				int2 targetTile = plow.BaseLoc + new int2(targetX, targetZ);

				float3 flattenedWorldPos = new float3(translation.Value.x, 0, translation.Value.z);
				float3 targetTileWorldPos = new float3(targetTile.x, 0, targetTile.y);
				float dist = math.distance(flattenedWorldPos, targetTileWorldPos);
				if (dist < 0.2f)
				{
					// plow tile
					int sectionRefId = grid.GetSectionId(targetTile);
					int tileIndex = grid.GetTileIndex(targetTile);
					Entity sectionEntity = sectionRefBuffer[gridEntity][sectionRefId].SectionEntity;
					DynamicBuffer<GridTile> tileBuffer = tileBufferMap[sectionEntity];
					GridTile tile = tileBuffer[tileIndex];
					if (!tile.IsPlowed)
					{
						tile.IsPlowed = true;
						tile.RenderTileDirty = true;
						tileBuffer[tileIndex] = tile;
						ecb.AddComponent<GridSectionDirty>(sectionEntity);
					}

					plow.CurrentIndex = plow.CurrentIndex + 1;

					if (plow.CurrentIndex >= plow.FieldSize.x * plow.FieldSize.y)
					{
						// done!
						ecb.RemoveComponent<WorkerIntent_Plow>(entity);
						ecb.AddComponent<WorkerIntent_None>(entity);
					}
					else
					{
						// advance to next tile in field
						targetX = plow.CurrentIndex / plow.FieldSize.y;
						targetZ = (targetX % 2 == 0) ? plow.CurrentIndex % plow.FieldSize.y : plow.FieldSize.y - 1 - (plow.CurrentIndex % plow.FieldSize.y);
						targetTile = plow.BaseLoc + new int2(targetX, targetZ);

						path.targetPosition = new float3(targetTile.x, 0, targetTile.y);
						ecb.RemoveComponent<PathComplete>(entity);
					}
				}
				
				//UnityEngine.Debug.Log(string.Format("{0} {1} {2} {3} {4}", dist, ltw.Position.x, ltw.Position.z, plow.TargetTilePos.x, plow.TargetTilePos.y));
				//UnityEngine.Debug.Log(string.Format("Progress: {0}", path.progress));
				
			}).Schedule();

		ecbSystem.AddJobHandleForProducer(Dependency);
	}
}
