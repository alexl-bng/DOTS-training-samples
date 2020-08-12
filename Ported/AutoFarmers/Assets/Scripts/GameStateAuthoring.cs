using Unity.Entities;
using Unity.Mathematics;

public enum WorkerSpawnType : uint
{
	None,
	Farmer,
	Drone
}

[GenerateAuthoringComponent]
public struct GameState : IComponentData
{
	public float3 LastStorePosition;
	public bool PlantSold;
}
