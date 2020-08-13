using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Random = Unity.Mathematics.Random;

//using UnityEngine;

[UpdateInGroup(typeof(InitializationSystemGroup))]
public class GridSpawnerSystem : SystemBase
{
	private EntityQuery _worldGeneratorQuery;

	protected override void OnCreate()
	{
		_worldGeneratorQuery = EntityManager.CreateEntityQuery(typeof(WorldGenerator));

		//Debug.Log($"size of tile buffer element is {System.Runtime.InteropServices.Marshal.SizeOf<GridTile>()}");
	}

	protected override void OnUpdate()
	{
		EntityCommandBufferSystem ecbSystem = World.GetExistingSystem<EndInitializationEntityCommandBufferSystem>();
		EntityCommandBuffer ecb = ecbSystem.CreateCommandBuffer();

		WorldGenerator worldGen = _worldGeneratorQuery.GetSingleton<WorldGenerator>();

		Entities.ForEach((Entity entity, in GridSpawner spawner, in LocalToWorld ltw) =>
		{
			Entity gridEntity = ecb.CreateEntity();

			Grid grid = new Grid
			{
				SectionDimensions = spawner.GridSectionDimensions,
				SectionCount = spawner.GridSectionCount,
				WorldScale = spawner.GridWorldScale
			};
			ecb.AddComponent(gridEntity, grid);

			ecb.AddComponent(gridEntity, new RandomNumberGenerator
			{
				rng = new Random((uint)entity.Index * 100 + 1000)
			});

			ecb.AddComponent<NeedsWorldGeneration>(gridEntity);

			DynamicBuffer<GridSectionReference> sectionReferenceBuffer = ecb.AddBuffer<GridSectionReference>(gridEntity);

			for (int sectionX = 0; sectionX < spawner.GridSectionCount.x; sectionX++)
			{
				for (int sectionZ = 0; sectionZ < spawner.GridSectionCount.y; sectionZ++)
				{
					Entity sectionEntity = ecb.CreateEntity();

					GridSection gridSection = new GridSection
					{
						TileOffset = new int2(sectionX * spawner.GridSectionDimensions.x, sectionZ * spawner.GridSectionDimensions.y),
						TileDimensions = spawner.GridSectionDimensions,
						WorldScale = spawner.GridWorldScale
					};
					ecb.AddComponent(sectionEntity, gridSection);

					DynamicBuffer<GridTile> sectionTileBuffer = ecb.AddBuffer<GridTile>(sectionEntity);

					sectionReferenceBuffer.Add(new GridSectionReference
					{
						SectionEntity = sectionEntity
					});

					for (int tileX = 0; tileX < spawner.GridSectionDimensions.x; tileX++)
					{
						for (int tileZ = 0; tileZ < spawner.GridSectionDimensions.y; tileZ++)
						{
							Entity tileEntity = ecb.Instantiate(worldGen.UnplowedTilePrefab);

							ecb.SetComponent(tileEntity, new Translation
							{
								Value = ltw.Position + new float3(sectionX * spawner.GridSectionDimensions.x + tileX, -0.5f, sectionZ * spawner.GridSectionDimensions.y + tileZ) * spawner.GridWorldScale
							});
							
							sectionTileBuffer.Add(new GridTile
							{
								IsPlowed = false,
								IsReserved = false,
								OccupationType = OccupationType.Unoccupied,
								OccupyingEntity = Entity.Null,
								RenderTileEntity = tileEntity
							});
						}
					}
				}
			}

			ecb.DestroyEntity(entity);
		}).Schedule();

		ecbSystem.AddJobHandleForProducer(Dependency);
	}

	protected override void OnDestroy()
	{
		_worldGeneratorQuery.Dispose();
	}
}
