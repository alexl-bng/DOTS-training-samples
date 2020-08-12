using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

[GenerateAuthoringComponent]
public struct GridOccupant : IComponentData
{
	public OccupationType OccupationType;
	public int2 GridSize;
}

public struct GridLocation : IComponentData
{
	public int2 Value;
}

public struct RegisteredGridOccupant : ISystemStateComponentData
{
	public int2 OccupiedMin;
	public int2 OccupiedMax;
}
