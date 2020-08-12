using Unity.Entities;
using Unity.Mathematics;

[GenerateAuthoringComponent]
public struct PlantConfig : IComponentData
{
	public float GrowthTime;
}