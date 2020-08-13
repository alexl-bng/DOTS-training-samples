﻿using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

[GenerateAuthoringComponent]
public struct WorldGenerator : IComponentData
{
	public int StoreTargetCount;
	public int RockAttempts;
	public int2 RockSizeMin;
	public int2 RockSizeMax;

	public int PlowedTileTargetCount;

	public Entity UnplowedTilePrefab;
	public Entity PlowedTilePrefab;
	public Entity StorePrefab;
	public Entity RockPrefab;
}

public struct NeedsWorldGeneration : IComponentData
{
	
}
