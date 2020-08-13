using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;



public class HarvestSearchSystem : SystemBase
{
	private EntityQuery m_plantQuery;
	private EntityQuery m_workerQuery;

	protected override void OnCreate()
	{
		m_plantQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new[]
			{
				ComponentType.ReadOnly<Plant>(),
				ComponentType.ReadOnly<PlantStateGrown>(),				
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
				ComponentType.ReadWrite<WorkerIntent_HarvestSearch>(),
			}
		});
	}


	protected override void OnUpdate()
	{
		EntityCommandBufferSystem ecbSystem = World.GetExistingSystem<EndInitializationEntityCommandBufferSystem>();
		EntityCommandBuffer ecb = ecbSystem.CreateCommandBuffer();


		NativeArray<Entity> plantEntities = m_plantQuery.ToEntityArray(Allocator.TempJob);//, out JobHandle plantEntitiesHandle);		
		NativeArray<Entity> workerEntities = m_workerQuery.ToEntityArray(Allocator.TempJob);//, out JobHandle workerEntitiesHandle);

		if (plantEntities.Length == 0)
		{
			UnityEngine.Debug.Log("No plants");
		}

		int workerCount = workerEntities.Length;
		int plantCount = plantEntities.Length;
		for (int i = 0; i < workerCount; ++i)
		{
			if (i < plantEntities.Length)
			{
				UnityEngine.Debug.Log("Assigning");
				ecb.AddComponent(workerEntities[i], new WorkerIntent_Harvest { PlantEntity = plantEntities[i] });
				ecb.RemoveComponent<PlantStateGrown>(plantEntities[i]);
			}
			else
			{
				ecb.AddComponent<WorkerIntent_None>(workerEntities[i]);
			}
			ecb.RemoveComponent<WorkerIntent_HarvestSearch>(workerEntities[i]);
			
		}

		plantEntities.Dispose();
		workerEntities.Dispose();


		ecbSystem.AddJobHandleForProducer(Dependency);
	}
}

