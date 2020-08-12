using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

public class WorkerSpawnSystem : SystemBase
{
	private EntityCommandBufferSystem m_CommandBufferSystem;

	protected override void OnCreate()
	{
		m_CommandBufferSystem = World.GetExistingSystem<EndSimulationEntityCommandBufferSystem>();
	}

	protected override void OnUpdate()
	{
		var entityCommandBuffer = m_CommandBufferSystem.CreateCommandBuffer();

		WorkerConfig workerConfig = GetSingleton<WorkerConfig>();
		GameState gameState = GetSingleton<GameState>();
		ResourceManager resourceManager = GetSingleton<ResourceManager>();

		if (gameState.PlantSold)
		{
			if (resourceManager.FarmerCoins >= resourceManager.FarmerPrice)
			{
				Entity newFarmerEntity = entityCommandBuffer.Instantiate(workerConfig.FarmerPrefab);
				entityCommandBuffer.SetComponent(newFarmerEntity, new Translation
				{
					Value = gameState.LastStorePosition
				});

				resourceManager.FarmerCoins -= resourceManager.FarmerPrice;
			}

			if (resourceManager.DroneCoins >= resourceManager.DronePrice)
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
