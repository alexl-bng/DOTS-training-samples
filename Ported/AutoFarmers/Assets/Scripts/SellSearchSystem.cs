using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

public class SellSearchSystem : SystemBase
{
	private EntityQuery m_storeQuery;
	private EntityQuery m_workerQuery;
	private EntityCommandBufferSystem m_ecbSystem;

	protected override void OnCreate()
	{
		m_storeQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new[]
			{
				ComponentType.ReadOnly<Store>()	,
				ComponentType.ReadOnly<Translation>()
			}
		});

		m_workerQuery = GetEntityQuery(new EntityQueryDesc
		{
			Any = new[]
			{
				ComponentType.ReadOnly<Farmer>(),
				ComponentType.ReadOnly<Drone>(),
			},
			All = new[]
			{
				ComponentType.ReadWrite<Path>(),
				ComponentType.ReadWrite<WorkerIntent_SellSearch>(),
			},
			None = new[]
			{
				ComponentType.ReadOnly<WorkerIntent_Sell>(),
			}
		});

		m_ecbSystem = World.GetExistingSystem<EndSimulationEntityCommandBufferSystem>();
	}

	protected override void OnUpdate()
	{
		EntityCommandBuffer ecb = m_ecbSystem.CreateCommandBuffer();

		NativeArray<Entity> storeEntities = m_storeQuery.ToEntityArray(Allocator.TempJob);
		NativeArray<Translation> storeTranslations = m_storeQuery.ToComponentDataArray<Translation>(Allocator.TempJob);
		NativeArray<Path> workerPaths = m_workerQuery.ToComponentDataArray<Path>(Allocator.TempJob);
		NativeArray<Entity> workerEntities = m_workerQuery.ToEntityArray(Allocator.TempJob);

		int workerCount = workerEntities.Length;
		int storeCount = storeEntities.Length;

		for (int i = 0; i < workerCount; ++i)
		{
			if (storeCount > 0)
			{
				ecb.AddComponent(workerEntities[i], new WorkerIntent_Sell { });
				Path newPath = workerPaths[i];
				newPath.targetPosition = storeTranslations[0].Value;
				ecb.SetComponent(workerEntities[i], newPath);
			}
			else
			{
				ecb.RemoveComponent<WorkerIntent_SellSearch>(workerEntities[i]);
				ecb.AddComponent(workerEntities[i], new WorkerIntent_None { });
			}
		}

		storeEntities.Dispose();
		workerEntities.Dispose();

		m_ecbSystem.AddJobHandleForProducer(Dependency);
	}
}

