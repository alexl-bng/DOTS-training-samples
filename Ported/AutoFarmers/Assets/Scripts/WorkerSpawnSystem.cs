using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

public class WorkerSpawnSystem : SystemBase
{
	private EntityCommandBufferSystem m_CommandBufferSystem;
	private EntityQuery m_FarmerQuery;
	private EntityQuery m_DroneQuery;

	protected override void OnCreate()
	{
		m_CommandBufferSystem = World.GetExistingSystem<EndSimulationEntityCommandBufferSystem>();

		m_FarmerQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new[]
			{
				ComponentType.ReadOnly<Farmer>(),
			}
		});

		m_DroneQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new[]
			{
				ComponentType.ReadOnly<Drone>(),
			}
		});
	}

	protected override void OnUpdate()
	{
		var entityCommandBuffer = m_CommandBufferSystem.CreateCommandBuffer();

		WorkerConfig workerConfig = GetSingleton<WorkerConfig>();
		GameState gameState = GetSingleton<GameState>();
		ResourceManager resourceManager = GetSingleton<ResourceManager>();

		if (gameState.PlantSold)
		{
			// get farmer and drone counts
			int currentFarmerCount = m_FarmerQuery.CalculateEntityCount();
			int currentDroneCount = m_DroneQuery.CalculateEntityCount();

			if (resourceManager.FarmerCoins >= resourceManager.FarmerPrice &&
				currentFarmerCount < workerConfig.MaxFarmerCount)
			{
				Entity newFarmerEntity = entityCommandBuffer.Instantiate(workerConfig.FarmerPrefab);
				entityCommandBuffer.SetComponent(newFarmerEntity, new Translation
				{
					Value = gameState.LastStorePosition
				});

				resourceManager.FarmerCoins -= resourceManager.FarmerPrice;
			}

			if (resourceManager.DroneCoins >= resourceManager.DronePrice &&
				currentDroneCount < workerConfig.MaxDroneCount)
			{
				Entity newDroneEntity = entityCommandBuffer.Instantiate(workerConfig.DronePrefab);
				entityCommandBuffer.SetComponent(newDroneEntity, new Translation
				{
					Value = gameState.LastStorePosition
				});

				resourceManager.DroneCoins -= resourceManager.DronePrice;
			}
		}

		gameState.PlantSold = false;

		m_CommandBufferSystem.AddJobHandleForProducer(Dependency);
	}
}
