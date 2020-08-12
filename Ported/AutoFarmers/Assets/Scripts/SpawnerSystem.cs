using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[UpdateInGroup(typeof(InitializationSystemGroup))]
public class SpawnerSystem : SystemBase
{
	protected override void OnUpdate()
	{
		EntityCommandBufferSystem ecbSystem = World.GetExistingSystem<EndInitializationEntityCommandBufferSystem>();
		EntityCommandBuffer ecb = ecbSystem.CreateCommandBuffer();

		Entities.ForEach((Entity entity, in TestSpawner spawner, in LocalToWorld ltw) =>
		{
			float gridCenterX = (spawner.Count.x - 1) / 2;
			float gridCenterY = (spawner.Count.y - 1) / 2;
			float gridCenterZ = (spawner.Count.z - 1) / 2;

			float3 gridCenter = new float3(gridCenterX, gridCenterY, gridCenterZ) * spawner.Spacing;

			for (int x = 0; x < spawner.Count.x; x++)
			{
				for (int y = 0; y < spawner.Count.y; y++)
				{
					for (int z = 0; z < spawner.Count.z; z++)
					{
						float3 spawnPos = new float3(x, y, z) * spawner.Spacing - gridCenter;

						Entity spawnEntity = ecb.Instantiate(spawner.Prefab);

						ecb.SetComponent(spawnEntity, new Translation
						{
							Value = ltw.Position + spawnPos
						});

						ecb.AddComponent<RandomNumberGenerator>(spawnEntity);
						ecb.SetComponent(spawnEntity, new RandomNumberGenerator
						{
							rng = new Random((uint)spawnEntity.Index)
						});
					}
				}
			}

			ecb.DestroyEntity(entity);
		}).Schedule();

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
							Entity tileEntity = ecb.Instantiate(spawner.TilePrefab);

							ecb.SetComponent(tileEntity, new Translation
							{
								Value = ltw.Position + new float3(sectionX * spawner.GridSectionDimensions.x + tileX, 0, sectionZ * spawner.GridSectionDimensions.y + tileZ) * spawner.GridWorldScale
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
}
