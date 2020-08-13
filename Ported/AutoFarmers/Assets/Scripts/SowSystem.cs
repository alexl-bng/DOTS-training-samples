using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;



public class SowSystem_PathComplete : SystemBase
{
	protected override void OnUpdate()
	{
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
				in WorkerIntent_Sow plow) =>
		{
			int sectionRefId = grid.GetSectionId(plow.TargetTilePos);
			int tileIndex = grid.GetTileIndex(plow.TargetTilePos);
			Entity sectionEntity = sectionRefBuffer[gridEntity][sectionRefId].SectionEntity;
			DynamicBuffer<GridTile> tileBuffer = tileBufferMap[sectionEntity];
			GridTile tile = tileBuffer[tileIndex];
			UnityEngine.Debug.Log("Sown!");
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

