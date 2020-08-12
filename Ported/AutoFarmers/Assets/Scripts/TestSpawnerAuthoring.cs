using Unity.Entities;
using Unity.Mathematics;

[GenerateAuthoringComponent]
public struct TestSpawner : IComponentData
{
	public Entity Prefab;
	public int3 Count;
	public float3 Spacing;
}