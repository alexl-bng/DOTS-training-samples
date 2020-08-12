﻿using Unity.Entities;
using Unity.Mathematics;

public struct PlantStateGrowing : IComponentData
{
	public float GrowthProgress;
}

public struct PlantStateGrown : IComponentData
{
}

public struct PlantStateCarried : IComponentData
{
	public Entity Owner;
}

public struct PlantStateWarpingOut : IComponentData
{
	public float WarpProgress;
}
