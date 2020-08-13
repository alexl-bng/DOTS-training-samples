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
				ComponentType.ReadOnly<Translation>(),
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
				ComponentType.ReadOnly<WorkerIntent_HarvestSearch>(),
				ComponentType.ReadWrite<Path>(),
			}
		});
	}


	protected override void OnUpdate()
	{
		EntityCommandBufferSystem ecbSystem = World.GetExistingSystem<EndInitializationEntityCommandBufferSystem>();
		EntityCommandBuffer ecb = ecbSystem.CreateCommandBuffer();
		
		//opt: make me async and use me in a job
		NativeArray<Entity> plantEntities = m_plantQuery.ToEntityArray(Allocator.TempJob);//, out JobHandle plantEntitiesHandle);		
		NativeArray<Translation> translations = m_plantQuery.ToComponentDataArray<Translation>(Allocator.TempJob);//, out JobHandle plantEntitiesHandle);		
		NativeArray<Entity> workerEntities = m_workerQuery.ToEntityArray(Allocator.TempJob);//, out JobHandle workerEntitiesHandle);
		NativeArray<Path> paths = m_workerQuery.ToComponentDataArray<Path>(Allocator.TempJob);//, out JobHandle plantEntitiesHandle);		

		//if (plantEntities.Length == 0)
		//{
		//	UnityEngine.Debug.Log("No plants");
		//}

		int workerCount = workerEntities.Length;
		int plantCount = plantEntities.Length;
		for (int i = 0; i < workerCount; ++i)
		{
			if (i < plantEntities.Length)
			{
				//UnityEngine.Debug.Log("Assigning");
				ecb.AddComponent(workerEntities[i], new WorkerIntent_Harvest { PlantEntity = plantEntities[i] });
				ecb.RemoveComponent<PlantStateGrown>(plantEntities[i]);
				Path path = paths[i];
				Translation plantLocation = translations[i];
				path.targetPosition = new float3
				{
					x = plantLocation.Value.x,
					y = plantLocation.Value.y,
					z = plantLocation.Value.z,
				};
				ecb.SetComponent(workerEntities[i], path);
			}
			else
			{
				ecb.AddComponent<WorkerIntent_None>(workerEntities[i]);
			}
			ecb.RemoveComponent<WorkerIntent_HarvestSearch>(workerEntities[i]);
			
		}

		plantEntities.Dispose();
		workerEntities.Dispose();
		translations.Dispose();
		paths.Dispose();


		ecbSystem.AddJobHandleForProducer(Dependency);
	}
}

