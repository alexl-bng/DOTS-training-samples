using Unity.Entities;
using Unity.Mathematics;

public struct Farmer : IComponentData { }
public struct Drone : IComponentData { }

public struct AI_Sell : IComponentData { }
public struct AI_Plant : IComponentData { }
public struct AI_Plow : IComponentData { }
public struct AI_Break : IComponentData { }
public struct AI_None : IComponentData { }

public struct RandomNumberGenerator : IComponentData
{
	public Random rng;
}