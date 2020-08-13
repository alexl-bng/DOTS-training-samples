using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;



public class PlowSystem : SystemBase
{
	protected override void OnUpdate()
	{
		EntityCommandBufferSystem ecbSystem = World.GetExistingSystem<EndInitializationEntityCommandBufferSystem>();
		EntityCommandBuffer ecb = ecbSystem.CreateCommandBuffer();

		Grid grid = GetSingleton<Grid>();
		Entity gridEntity = GetSingletonEntity<Grid>();
		BufferFromEntity<GridSectionReference> sectionRefBuffer = GetBufferFromEntity<GridSectionReference>();
		BufferFromEntity<GridTile> tileBufferMap = GetBufferFromEntity<GridTile>();

		Entities.ForEach((Entity entity, ref RandomNumberGenerator rng, in WorkerIntent_Plow plow, in Path path, in LocalToWorld ltw) =>
		{
			float3 diff = ltw.Position - new float3(plow.TargetTilePos.x, 0, plow.TargetTilePos.y);
			//UnityEngine.Debug.Log(string.Format("{0} {1} {2} {3} {4}", math.length(diff), ltw.Position.x, ltw.Position.z, plow.TargetTilePos.x, plow.TargetTilePos.y));
			//UnityEngine.Debug.Log(string.Format("Progress: {0}", path.progress));
			if (math.length(diff) < 1.1f) //because Y lol
			//if (path.progress <1f)
			{
				UnityEngine.Debug.Log("got there");
				
				int sectionRefId = grid.GetSectionId(plow.TargetTilePos);
				int tileIndex = grid.GetTileIndex(plow.TargetTilePos);
				Entity sectionEntity = sectionRefBuffer[gridEntity][sectionRefId].SectionEntity;
				DynamicBuffer<GridTile> tileBuffer = tileBufferMap[sectionEntity];
				GridTile tile = tileBuffer[tileIndex];
				tile.IsPlowed = true;
				tileBuffer[tileIndex] = tile;
				ecb.RemoveComponent<WorkerIntent_Plow>(entity);
				ecb.AddComponent<WorkerIntent_None>(entity);
				
			}
			
		}).Schedule();

		ecbSystem.AddJobHandleForProducer(Dependency);
	}
}
