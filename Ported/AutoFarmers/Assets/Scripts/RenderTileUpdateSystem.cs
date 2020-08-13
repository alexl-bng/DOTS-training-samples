using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;

public class RenderTileUpdateSystem : SystemBase
{
	private EntityQuery _worldGeneratorQuery;

	protected override void OnCreate()
	{
		_worldGeneratorQuery = EntityManager.CreateEntityQuery(typeof(WorldGenerator));
	}

	protected override void OnUpdate()
    {
	    EntityCommandBufferSystem ecbSystem = World.GetExistingSystem<EndInitializationEntityCommandBufferSystem>();
	    EntityCommandBuffer ecb = ecbSystem.CreateCommandBuffer();

	    WorldGenerator worldGen = _worldGeneratorQuery.GetSingleton<WorldGenerator>();

	    BufferFromEntity<GridTile> tileBuffers = GetBufferFromEntity<GridTile>(false);

		Entities
			.WithAll<GridSectionDirty>()
			.ForEach((Entity entity) =>
		{
			DynamicBuffer<GridTile> tileBuffer = tileBuffers[entity];

            for (int tileIndex = 0; tileIndex < tileBuffer.Length; tileIndex++)
            {
				GridTile tile = tileBuffer[tileIndex];
				if (tile.RenderTileDirty)
				{
					float4 color = tile.IsPlowed ? new float4(0.25f, 0.75f, 0.25f, 1) : new float4(1, 1, 1, 1);
					ecb.SetComponent(tile.RenderTileEntity, new MeshColor{ Value = color });

					tile.RenderTileDirty = false;
					tileBuffer[tileIndex] = tile;
				}
            }

			ecb.RemoveComponent<GridSectionDirty>(entity);
        }).Schedule();

		ecbSystem.AddJobHandleForProducer(Dependency);
    }

	protected override void OnDestroy()
	{
		_worldGeneratorQuery.Dispose();
	}
}
