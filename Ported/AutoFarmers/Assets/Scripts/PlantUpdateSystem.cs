using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

public class PlantGrowthSystem : SystemBase
{
	private EntityCommandBufferSystem m_CommandBufferSystem;

	protected override void OnCreate()
	{
		m_CommandBufferSystem = World.GetExistingSystem<EndSimulationEntityCommandBufferSystem>();
	}

	protected override void OnUpdate()
	{
		PlantConfig plantConfig = GetSingleton<PlantConfig>();

		float deltaTime = Time.DeltaTime;
		float growthSpeed = 1.0f / math.max(0.001f, plantConfig.GrowthTime);
		float growthDelta = deltaTime * growthSpeed;
		float3 plantScale = plantConfig.PlantScale;

		var entityCommandBuffer = m_CommandBufferSystem.CreateCommandBuffer().AsParallelWriter();

		Entities
			.WithName("Plant_Growth")
			.ForEach((int entityInQueryIndex, Entity entity, ref PlantStateGrowing growingState, ref Translation translation, ref NonUniformScale scale) =>
			{
				if (growingState.GrowthProgress < 1.0f)
				{
					growingState.GrowthProgress = math.min(1.0f, growingState.GrowthProgress + growthDelta);
					scale.Value = growingState.GrowthProgress * plantScale;
				}
				else
				{
					// plant is fully grown
					entityCommandBuffer.AddComponent(entityInQueryIndex, entity, new PlantStateGrown { });
					entityCommandBuffer.RemoveComponent<PlantStateGrowing>(entityInQueryIndex, entity);
				}

			}).ScheduleParallel();

		m_CommandBufferSystem.AddJobHandleForProducer(Dependency);
	}
}

public class CarriedPlantUpdateSystem : SystemBase
{
	private EntityCommandBufferSystem m_CommandBufferSystem;

	protected override void OnCreate()
	{
		m_CommandBufferSystem = World.GetExistingSystem<EndSimulationEntityCommandBufferSystem>();
	}

	protected override void OnUpdate()
	{
		var entityCommandBuffer = m_CommandBufferSystem.CreateCommandBuffer().AsParallelWriter();

		Entities
			.WithName("Carried_Plant_Update")
			.ForEach((int entityInQueryIndex, Entity entity, ref Translation translation, ref WorkerIntent_Sell sellIntent) =>
			{
				entityCommandBuffer.SetComponent(entityInQueryIndex, sellIntent.PlantEntity, new Translation { Value = translation.Value });

			}).ScheduleParallel();

		m_CommandBufferSystem.AddJobHandleForProducer(Dependency);
	}
}

public class PlantWarpOutSystem : SystemBase
{
	private EntityCommandBufferSystem m_CommandBufferSystem;

	protected override void OnCreate()
	{
		m_CommandBufferSystem = World.GetExistingSystem<EndSimulationEntityCommandBufferSystem>();
	}

	protected override void OnUpdate()
	{
		var entityCommandBuffer = m_CommandBufferSystem.CreateCommandBuffer().AsParallelWriter();
		PlantConfig plantConfig = GetSingleton<PlantConfig>();

		float deltaTime = Time.DeltaTime;
		float warpSpeed = 1.0f / math.max(0.001f, plantConfig.WarpTime);
		float heightDelta = warpSpeed * deltaTime * plantConfig.MaxWarpHeight;

		Entities
			.WithName("Plant_Warp_Out")
			.ForEach((int entityInQueryIndex, Entity entity, ref Translation translation, ref PlantStateWarpingOut warpOut) =>
			{
				if (warpOut.WarpProgress < 1.0f)
				{
					translation.Value += new float3(0.0f, heightDelta, 0.0f);
					warpOut.WarpProgress += warpSpeed * deltaTime;
				}
				else
				{
					entityCommandBuffer.DestroyEntity(entityInQueryIndex, entity);
				}

			}).ScheduleParallel();

		m_CommandBufferSystem.AddJobHandleForProducer(Dependency);
	}
}

public class PlantHarvestSystem : SystemBase
{
	private EntityCommandBufferSystem m_CommandBufferSystem;

	protected override void OnCreate()
	{
		m_CommandBufferSystem = World.GetExistingSystem<EndSimulationEntityCommandBufferSystem>();
	}

	protected override void OnUpdate()
	{
		var entityCommandBuffer = m_CommandBufferSystem.CreateCommandBuffer().AsParallelWriter();

		Entities
			.WithName("Plant_Harvest")
			.WithAll<PathComplete>()
			.ForEach((int entityInQueryIndex, Entity entity, ref WorkerIntent_Harvest harvestIntent) =>
			{
				// update plant state
				entityCommandBuffer.RemoveComponent<PlantStateGrown>(entityInQueryIndex, harvestIntent.PlantEntity);
				entityCommandBuffer.AddComponent<PlantStateCarried>(entityInQueryIndex, harvestIntent.PlantEntity);

				// update worker state
				entityCommandBuffer.RemoveComponent<PathComplete>(entityInQueryIndex, entity);
				entityCommandBuffer.RemoveComponent<WorkerIntent_Harvest>(entityInQueryIndex, entity);
				entityCommandBuffer.AddComponent<WorkerIntent_Sell>(entityInQueryIndex, entity);

			}).ScheduleParallel();

		m_CommandBufferSystem.AddJobHandleForProducer(Dependency);
	}
}

public class PlantSellSystem : SystemBase
{
	private EntityCommandBufferSystem m_CommandBufferSystem;

	protected override void OnCreate()
	{
		m_CommandBufferSystem = World.GetExistingSystem<EndSimulationEntityCommandBufferSystem>();
	}

	protected override void OnUpdate()
	{
		var entityCommandBuffer = m_CommandBufferSystem.CreateCommandBuffer().AsParallelWriter();
		PlantConfig plantConfig = GetSingleton<PlantConfig>();
		GameState gameState = GetSingleton<GameState>();
		ResourceManager resourceManager = GetSingleton<ResourceManager>();

		Entities
			.WithName("Plant_Sell")
			.WithAll<PathComplete>()
			.ForEach((int entityInQueryIndex, Entity entity, ref WorkerIntent_Sell sellIntent, ref Path path) =>
			{
				// update game state
				gameState.LastStorePosition = path.targetPosition;
				gameState.PlantSold = true;
				resourceManager.FarmerCoins++;
				resourceManager.DroneCoins++;

				// update plant state
				entityCommandBuffer.RemoveComponent<PlantStateCarried>(entityInQueryIndex, sellIntent.PlantEntity);
				entityCommandBuffer.AddComponent<PlantStateWarpingOut>(entityInQueryIndex, sellIntent.PlantEntity);

				// update worker state
				entityCommandBuffer.RemoveComponent<PathComplete>(entityInQueryIndex, entity);
				entityCommandBuffer.RemoveComponent<WorkerIntent_Sell>(entityInQueryIndex, entity);
				entityCommandBuffer.AddComponent<WorkerIntent_None>(entityInQueryIndex, entity);

			}).Run();

		m_CommandBufferSystem.AddJobHandleForProducer(Dependency);
	}
}
