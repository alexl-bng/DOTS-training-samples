using System.Security.Cryptography.X509Certificates;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public struct ClosestStoreFloodNode
{
	public int2 StoreLocation;
	public int2 Offset;
}

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
		NativeQueue<ClosestStoreFloodNode> storeFloodQueue = new NativeQueue<ClosestStoreFloodNode>(Allocator.TempJob);

	    BufferFromEntity<GridSectionReference> sectionRefs = GetBufferFromEntity<GridSectionReference>(true);
	    BufferFromEntity<GridTile> tileBuffers = GetBufferFromEntity<GridTile>(false);

		Entities
			.WithDisposeOnCompletion(usedTiles)
			.WithDisposeOnCompletion(storeFloodQueue)
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

						storeFloodQueue.Enqueue(new ClosestStoreFloodNode
						{
							StoreLocation = tryLocation,
							Offset = new int2(0, 0)
						});

						attempts = 0;
					}
					else
					{
						attempts--;
					}
				}
			}

			int rockAttempts = worldGen.RockAttempts;
			while (rockAttempts > 0)
			{
				int2 tryLocation = new int2(rng.rng.NextInt(0, gridDims.x), rng.rng.NextInt(0, gridDims.y));

				bool isValidPlacement = true;

				int sizeX = rng.rng.NextInt(worldGen.RockSizeMin.x, worldGen.RockSizeMax.x);
				int sizeZ = rng.rng.NextInt(worldGen.RockSizeMin.y, worldGen.RockSizeMax.y);

				sizeX = math.min(sizeX, gridDims.x - tryLocation.x);
				sizeZ = math.min(sizeZ, gridDims.y - tryLocation.y);

				for (int testX = tryLocation.x; testX < tryLocation.x + sizeX; testX++)
				{
					for (int testZ = tryLocation.y; testZ < tryLocation.y + sizeZ; testZ++)
					{
						if (usedTiles.Contains(new int2(testX, testZ)))
						{
							isValidPlacement = false;
						}
					}
				}

				if (isValidPlacement)
				{
					Entity rockEntity = ecb.Instantiate(worldGen.RockPrefab);

					float3 worldScale = new float3(sizeX - 0.25f, 0.5f, sizeZ - 0.25f) * grid.WorldScale;
					ecb.AddComponent(rockEntity, new NonUniformScale
					{
						Value = worldScale
					});

					float3 worldPosition = new float3(tryLocation.x + sizeX * 0.5f - 0.5f, 0.25f, tryLocation.y + sizeZ * 0.5f - 0.5f);
					ecb.SetComponent(rockEntity, new Translation
					{
						Value = worldPosition
					});

					ecb.SetComponent(rockEntity, new GridOccupant
					{
						OccupationType = OccupationType.Rock,
						GridSize = new int2(sizeX, sizeZ)
					});

					ecb.AddComponent(rockEntity, new GridLocation
					{
						Value = tryLocation
					});

					ecb.SetComponent(rockEntity, new Rock
					{
						health = sizeX * sizeZ
					});

					for (int testX = tryLocation.x; testX < tryLocation.x + sizeX; testX++)
					{
						for (int testZ = tryLocation.y; testZ < tryLocation.y + sizeZ; testZ++)
						{
							usedTiles.Add(new int2(testX, testZ));
						}
					}
				}

				rockAttempts--;
			}

			for (int plowedTileIndex = 0; plowedTileIndex < worldGen.PlowedTileTargetCount; plowedTileIndex++)
			{
				int attempts = 10;
				while (attempts > 0)
				{
					int2 tryLocation = new int2(rng.rng.NextInt(0, gridDims.x), rng.rng.NextInt(0, gridDims.y));

					if (!usedTiles.Contains(tryLocation))
					{
						int sectionIndex = grid.GetSectionId(tryLocation);
						int tileIndex = grid.GetTileIndex(tryLocation);
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

			while (!storeFloodQueue.IsEmpty())
			{
				ClosestStoreFloodNode thisNode = storeFloodQueue.Dequeue();
				int2 thisLocation = thisNode.StoreLocation - thisNode.Offset;

				if (grid.IsValidGridLocation(thisLocation))
				{
					int sectionIndex = grid.GetSectionId(thisLocation);
					int tileIndex = grid.GetTileIndex(thisLocation);
					Entity sectionEntity = sectionRefs[entity][sectionIndex].SectionEntity;
					DynamicBuffer<GridTile> tileBuffer = tileBuffers[sectionEntity];
					GridTile tile = tileBuffer[tileIndex];

					if (math.abs(thisNode.Offset.x) + math.abs(thisNode.Offset.y) < (long)math.abs(tile.ClosestStoreOffset.x) + math.abs(tile.ClosestStoreOffset.y))
					{
						tile.ClosestStoreOffset = thisNode.Offset;
						tileBuffer[tileIndex] = tile;

						storeFloodQueue.Enqueue(new ClosestStoreFloodNode
						{
							StoreLocation = thisNode.StoreLocation,
							Offset = thisNode.Offset + new int2(1, 0)
						});

						storeFloodQueue.Enqueue(new ClosestStoreFloodNode
						{
							StoreLocation = thisNode.StoreLocation,
							Offset = thisNode.Offset + new int2(-1, 0)
						});

						storeFloodQueue.Enqueue(new ClosestStoreFloodNode
						{
							StoreLocation = thisNode.StoreLocation,
							Offset = thisNode.Offset + new int2(0, 1)
						});

						storeFloodQueue.Enqueue(new ClosestStoreFloodNode
						{
							StoreLocation = thisNode.StoreLocation,
							Offset = thisNode.Offset + new int2(0, -1)
						});
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
