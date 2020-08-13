using Unity.Entities;
using Unity.Mathematics;

[GenerateAuthoringComponent]
public struct PlantConfig : IComponentData
{
	public Entity PlantPrefab;
	public float3 PlantScale;
	public float GrowthTime;
	public float WarpTime;
	public float MaxWarpHeight;
}