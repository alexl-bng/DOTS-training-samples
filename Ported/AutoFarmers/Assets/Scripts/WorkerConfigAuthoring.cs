using Unity.Entities;
using Unity.Mathematics;

[GenerateAuthoringComponent]
public struct WorkerConfig : IComponentData
{
	public Entity FarmerPrefab;
	public Entity DronePrefab;
}