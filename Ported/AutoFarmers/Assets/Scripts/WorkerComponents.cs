using Unity.Entities;

public struct WorkerIntent_Harvest : IComponentData { }
public struct WorkerIntent_Sell : IComponentData
{
	public Entity PlantEntity;
}

public struct WorkerIntent_Plant : IComponentData { }
public struct WorkerIntent_Plow : IComponentData { }
public struct WorkerIntent_Break : IComponentData { }


