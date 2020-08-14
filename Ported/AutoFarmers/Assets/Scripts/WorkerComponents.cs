using Unity.Entities;
using Unity.Mathematics;

public struct WorkerIntent_Harvest : IComponentData
{
	public Entity PlantEntity;
}

public struct WorkerIntent_Sell : IComponentData {}

public struct WorkerIntent_Sow : IComponentData
{
	public int2 TargetTilePos;
	public int2 ContinueDirection;
}
public struct WorkerIntent_Plow : IComponentData
{
	public int2 BaseLoc;
	public int2 FieldSize;
	public int CurrentIndex;
}
public struct WorkerIntent_Break : IComponentData { }

public struct WorkerIntent_HarvestSearch : IComponentData { }

public struct WorkerIntent_SellSearch : IComponentData
{
	public Entity PlantEntity;
}



