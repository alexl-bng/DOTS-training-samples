using Unity.Entities;
using Unity.Mathematics;

[GenerateAuthoringComponent]
public struct RandomNumberGenerator : IComponentData
{
	public Random rng;
}