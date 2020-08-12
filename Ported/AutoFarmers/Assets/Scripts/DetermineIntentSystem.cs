using Unity.Entities;

public class DetermineIntentSystem_Farmer : SystemBase
{
	protected override void OnUpdate()
	{
		EntityCommandBufferSystem ecbSystem = World.GetExistingSystem<EndInitializationEntityCommandBufferSystem>();
		EntityCommandBuffer ecb = ecbSystem.CreateCommandBuffer();

		Entities
			.WithAll<Farmer, WorkerIntent_None>()			
			.ForEach((
				Entity entity,
				ref RandomNumberGenerator rng) =>
		{
			int nextStateIndex = rng.rng.NextInt(0, 4);
			switch (nextStateIndex)
			{
				case 0:
					WorkerIntentUtils.SwitchToHarvestIntent(ecb, entity);
					break;
				case 1:
					WorkerIntentUtils.SwitchToPlantIntent(ecb, entity);
					break;
				case 2:
					WorkerIntentUtils.SwitchToPlowIntent(ecb, entity, rng);
					break;
				case 3:
					WorkerIntentUtils.SwitchToBreakIntent(ecb, entity);
					break;

			}

			ecb.RemoveComponent<WorkerIntent_None>(entity);
		}).Schedule();

		ecbSystem.AddJobHandleForProducer(Dependency);
	}
}


public class DetermineIntentSystem_Drone: SystemBase
{
	protected override void OnUpdate()
	{
		EntityCommandBufferSystem ecbSystem = World.GetExistingSystem<EndInitializationEntityCommandBufferSystem>();
		EntityCommandBuffer ecb = ecbSystem.CreateCommandBuffer();

		Entities.ForEach((Entity entity, ref RandomNumberGenerator rng) =>
		{
			int nextStateIndex = rng.rng.NextInt(0, 1);
			switch (nextStateIndex)
			{
				case 0:
					WorkerIntentUtils.SwitchToHarvestIntent(ecb, entity);
					break;				

			}

			ecb.RemoveComponent<WorkerIntent_None>(entity);
		}).Schedule();

		ecbSystem.AddJobHandleForProducer(Dependency);
	}
}


class WorkerIntentUtils
{
	public static void SwitchToHarvestIntent(EntityCommandBuffer ecb, Entity entity)
	{
		//find a plant to harvest. Maybe just tile location instead?
		//reserve?
		//ecb.AddComponent<HarvestTarget>(entity);
		//ecb.SetComponent<HarvestTarget>(foundPlantEntity);
		ecb.AddComponent<WorkerIntent_Harvest>(entity);
	}

	public static void SwitchToBreakIntent(EntityCommandBuffer ecb, Entity entity)
	{
		//find a rock to kill. Maybe just tile location instead?		
		//ecb.AddComponent<SmashTarget>(entity);
		//ecb.SetComponent<SmashTarget>(foundRockEntity);
		ecb.AddComponent<WorkerIntent_Break>(entity);
	}

	public static void SwitchToPlantIntent(EntityCommandBuffer ecb, Entity entity)
	{
		//find a tile to plant on. Maybe just tile location instead?
		//reserve?
		//ecb.AddComponent<PlantTarget>(entity);
		//ecb.SetComponent<PlantTarget>(foundTileEntity);
		ecb.AddComponent<WorkerIntent_Plant>(entity);
	}

	public static void SwitchToPlowIntent(EntityCommandBuffer ecb, Entity entity, RandomNumberGenerator rng)
	{
		//tjtj: maybe someday
		//int fieldWidth = rng.rng.NextInt(0, 8);
		//int fieldHeight = rng.rng.NextInt(0, 8);
		//ecb.AddComponent<FieldDimensions>(entity);
		//ecb.SetComponent(entity, new FieldDimensions {Width = fieldWidth, Height = fieldHeight });

		//find a tile to plow on. Maybe just tile location instead?
		//reserve?
		//ecb.AddComponent<PlowTarget>(entity);
		//ecb.SetComponent<PlowTarget>(foundTileEntity);
		ecb.AddComponent<WorkerIntent_Plow>(entity);
	}
}