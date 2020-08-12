using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[UpdateInGroup(typeof(InitializationSystemGroup))]
public class TestSpawnerSystem : SystemBase
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
							rng = new Random((uint)spawnEntity.Index * 100)
						});
					}
				}
			}

			ecb.DestroyEntity(entity);
		}).Schedule();
		
		ecbSystem.AddJobHandleForProducer(Dependency);
	}
}
