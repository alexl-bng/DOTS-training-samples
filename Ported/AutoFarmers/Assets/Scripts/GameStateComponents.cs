using Unity.Entities;
using Unity.Mathematics;

public struct StoreTracking : IComponentData
{
	public float3 LastStorePosition;
}