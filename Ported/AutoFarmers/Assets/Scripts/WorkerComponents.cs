using Unity.Entities;
using Unity.Mathematics;

public struct WorkerIntent_Harvest : IComponentData
{
	public Entity PlantEntity;
}

public struct WorkerIntent_Sell : IComponentData
{
	public Entity PlantEntity;
}

public struct WorkerIntent_Sow : IComponentData
{
	public int2 TargetTilePos;
}
public struct WorkerIntent_Plow : IComponentData
{
	public int2 TargetTilePos;
}
public struct WorkerIntent_Break : IComponentData { }

public struct WorkerIntent_HarvestSearch : IComponentData { }



