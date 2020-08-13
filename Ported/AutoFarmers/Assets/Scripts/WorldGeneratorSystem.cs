using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

[UpdateInGroup(typeof(InitializationSystemGroup))]
public class WorldGeneratorSystem : SystemBase
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

		NativeHashSet<int2> usedTiles = new NativeHashSet<int2>(0, Allocator.TempJob);

	    BufferFromEntity<GridSectionReference> sectionRefs = GetBufferFromEntity<GridSectionReference>(true);
	    BufferFromEntity<GridTile> tileBuffers = GetBufferFromEntity<GridTile>(false);

		Entities
			.WithDisposeOnCompletion(usedTiles)
			.WithAll<NeedsWorldGeneration>()
			.ForEach((Entity entity, ref RandomNumberGenerator rng, in Grid grid) =>
		{
			int2 gridDims = grid.GetTileDimensions();

			for (int storeIndex = 0; storeIndex < worldGen.StoreTargetCount; storeIndex++)
			{
				int attempts = 10;
				while (attempts > 0)
				{
					int2 tryLocation = new int2(rng.rng.NextInt(0, gridDims.x), rng.rng.NextInt(0, gridDims.y));

					if (!usedTiles.Contains(tryLocation))
					{
						Entity storeEntity = ecb.Instantiate(worldGen.StorePrefab);

						ecb.SetComponent(storeEntity, new Translation
						{
							Value = new float3(tryLocation.x, 1, tryLocation.y) * grid.WorldScale
						});

						ecb.AddComponent(storeEntity, new GridLocation
						{
							Value = tryLocation
						});

						usedTiles.Add(tryLocation);
						usedTiles.Add(tryLocation + new int2(0, 1));
						usedTiles.Add(tryLocation + new int2(0, -1));
						usedTiles.Add(tryLocation + new int2(1, 0));
						usedTiles.Add(tryLocation + new int2(-1, 0));

						attempts = 0;
					}
					else
					{
						attempts--;
					}
				}
			}

			// TODO: rocks


			for (int plowedTileIndex = 0; plowedTileIndex < worldGen.PlowedTileTargetCount; plowedTileIndex++)
			{
				int attempts = 10;
				while (attempts > 0)
				{
					int2 tryLocation = new int2(rng.rng.NextInt(0, gridDims.x), rng.rng.NextInt(0, gridDims.y));

					if (!usedTiles.Contains(tryLocation))
					{
						int sectionIndex = grid.GetSectionId(tryLocation);
						int tileIndex = grid.GetSectionId(tryLocation);
						Entity sectionEntity = sectionRefs[entity][sectionIndex].SectionEntity;

						DynamicBuffer<GridTile> tileBuffer = tileBuffers[sectionEntity];

						GridTile tile = tileBuffer[tileIndex];

						tile.IsPlowed = true;
						tile.RenderTileDirty = true;

						tileBuffer[tileIndex] = tile;

						ecb.AddComponent<GridSectionDirty>(sectionEntity);

						usedTiles.Add(tryLocation);

						attempts = 0;
					}
					else
					{
						attempts--;
					}
				}
			}

			ecb.RemoveComponent<NeedsWorldGeneration>(entity);
        }).Schedule();

	    ecbSystem.AddJobHandleForProducer(Dependency);
	}

	protected override void OnDestroy()
	{
		_worldGeneratorQuery.Dispose();
	}
}
