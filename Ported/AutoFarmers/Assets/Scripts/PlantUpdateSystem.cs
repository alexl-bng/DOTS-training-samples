using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

public class PlantInitialize : SystemBase
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
			.WithName("Plant_Initialize")
			.WithAll<Plant>()
			.WithNone<PlantStateGrowing, PlantStateGrown, PlantStateCarried>()
			.ForEach((int entityInQueryIndex, Entity entity) =>
			{
				entityCommandBuffer.AddComponent(entityInQueryIndex, entity, new PlantStateGrowing { GrowthProgress = 0.0f });

			}).ScheduleParallel();

		m_CommandBufferSystem.AddJobHandleForProducer(Dependency);
	}
}

public class PlantGrowthSystem : SystemBase
{
	private EntityCommandBufferSystem m_CommandBufferSystem;
	private float3 kPlantScale = new float3(0.125f, 1.0f, 0.125f);

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
		float3 plantScale = kPlantScale;

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
		float heightDelta = warpSpeed * deltaTime;
		float maxWarpHeight = plantConfig.MaxWarpHeight; 

		Entities
			.WithName("Plant_Warp_Out")
			.ForEach((int entityInQueryIndex, Entity entity, ref Translation translation, ref PlantStateWarpingOut warpOut) =>
			{
				if (translation.Value.y > maxWarpHeight)
				{
					entityCommandBuffer.DestroyEntity(entityInQueryIndex, entity);
				}
				else
				{
					translation.Value += new float3(0.0f, heightDelta, 0.0f);
				}

			}).ScheduleParallel();

		m_CommandBufferSystem.AddJobHandleForProducer(Dependency);
	}
}