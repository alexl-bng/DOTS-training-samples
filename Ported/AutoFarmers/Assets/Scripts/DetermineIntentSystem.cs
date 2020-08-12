using Unity.Entities;
using Unity.Mathematics;

public class DetermineIntentSystem_Farmer : SystemBase
{
	protected override void OnUpdate()
	{
		EntityCommandBufferSystem ecbSystem = World.GetExistingSystem<EndInitializationEntityCommandBufferSystem>();
		EntityCommandBuffer ecb = ecbSystem.CreateCommandBuffer();

		//once
		Grid grid = GetSingleton<Grid>();
		Entity gridEntity = GetSingletonEntity<Grid>();
		BufferFromEntity<GridSectionReference> sectionRefBuffer = GetBufferFromEntity<GridSectionReference>();
		BufferFromEntity<GridTile> tileBuffer = GetBufferFromEntity<GridTile>();

		Entities
			.WithAll<Farmer, WorkerIntent_None>()			
			.ForEach((
				Entity entity,
				ref Path path,
				ref RandomNumberGenerator rng) =>
		{
			int nextStateIndex = rng.rng.NextInt(2, 3);
			switch (nextStateIndex)
			{
				case 0:
					WorkerIntentUtils.SwitchToHarvestIntent(ecb, entity);
					break;
				case 1:
					WorkerIntentUtils.SwitchToPlantIntent(ecb, entity);
					break;
				case 2:
					WorkerIntentUtils.SwitchToPlowIntent(ecb, entity, ref grid, gridEntity, ref sectionRefBuffer, ref tileBuffer, rng, ref path);
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

		Entities.WithAll<Drone>().ForEach((Entity entity, ref RandomNumberGenerator rng) =>
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

	public static void SwitchToPlowIntent(
		EntityCommandBuffer ecb, 
		Entity workerEntity,
		ref Grid grid, 
		Entity gridEntity, 
		ref BufferFromEntity<GridSectionReference> sectionRefBuffer, 
		ref BufferFromEntity<GridTile> tileBuffer,
		RandomNumberGenerator rng,
		ref Path path)
	{
		//tjtj: maybe someday
		//int fieldWidth = rng.rng.NextInt(0, 8);
		//int fieldHeight = rng.rng.NextInt(0, 8);
		//ecb.AddComponent<FieldDimensions>(entity);
		//ecb.SetComponent(entity, new FieldDimensions {Width = fieldWidth, Height = fieldHeight });

		int2 worldDim = grid.GetWorldDimensions();

		int tries = 10;

		bool foundTile = false;
		int x;
		int y;
		do
		{
			tries--;
			x = rng.rng.NextInt(0, worldDim.x);
			y = rng.rng.NextInt(0, worldDim.y);
			GridTile tile = GetTileAtPos(x, y, ref grid, gridEntity, ref sectionRefBuffer, ref tileBuffer);
			foundTile = !tile.IsPlowed;
		} while (!foundTile && tries>0);

		if (foundTile)
		{
			//find a tile to plow on. Maybe just tile location instead?
			//reserve?
			ecb.AddComponent<WorkerIntent_Plow>(workerEntity);
			ecb.SetComponent(workerEntity, new WorkerIntent_Plow
			{
				TargetTilePos = new int2(x, y)
			});

			path.targetPosition = new float3(x, 0, y);
		}
	}


	public static GridTile GetTileAtPos(int x, int y, ref Grid grid, Entity gridEntity, ref BufferFromEntity<GridSectionReference> sectionRefBuffer, ref BufferFromEntity<GridTile> tileBuffer)
	{
		int2 pos = new int2(x, y);
		int sectionRefId = grid.GetSectionId(pos);
		int tileIndex = grid.GetTileIndex(pos);
		Entity sectionEntity = sectionRefBuffer[gridEntity][sectionRefId].SectionEntity;
		GridTile tile = tileBuffer[sectionEntity][tileIndex];
		return tile;
	}

	/*
	public static GetTileAt(int x, int y, Grid grid, )
	{
		//each
		int2 pos = new int2(x, y);
		int sectionRefId = grid.GetSectionId(pos);
		Entity sectionRef = sectionRefBuffer[sectionRefId].SectionEntity;
		GridSection section = GetComponent<GridSection>(sectionRef);
		int tileIndex = section.GetTileIndex(pos);
		DynamicBuffer<GridTile> tileBuffer = GetBuffer<GridTile>(sectionRef);
		GridTile tile = tileBuffer[tileIndex];
	}
	*/
}