using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

// TODO: define buffer capacity to match grid size
[InternalBufferCapacity(1000)]
public struct GridSectionReference : IBufferElementData
{
	public Entity SectionEntity;
}

public struct Grid : IComponentData
{
	public int2 SectionDimensions;
	public int2 SectionCount;
	public float3 WorldScale;

	public int2 GetWorldDimensions()
	{
		return new int2(SectionCount.x * SectionDimensions.x, SectionCount.y * SectionDimensions.y);
	}

	
	
	public int GetSectionId(int2 gridLocation)
	{
		int sectionX = gridLocation.x / SectionDimensions.x;
		int sectionZ = gridLocation.y / SectionDimensions.y;
		return sectionX * SectionCount.y + sectionZ;
	}

	public int GetTileIndex(int2 pos)
	{
		int relativeX = pos.x % SectionDimensions.x;
		int relativeY = pos.y % SectionDimensions.y;
		int tileIndex =  relativeX * SectionDimensions.y + relativeY;
		return tileIndex;
	}

	public int2 GetTileDimensions()
	{
		return SectionDimensions * SectionCount;
	}
}

public struct GridSection : IComponentData
{
	public int2 TileOffset;
	public int2 TileDimensions;
	public float3 WorldScale;

	public int GetTileIndex(int2 tileLocation)
	{
		int2 relativeLocation = tileLocation - TileOffset;

		return GetTileIndexRelative(relativeLocation);
	}

	public int GetTileIndexRelative(int2 tileLocation)
	{
		return tileLocation.x * TileDimensions.y + tileLocation.y;
	}
}

public enum OccupationType : uint
{
	Unoccupied,
	Rock,
	Plant,
	Store
}

// TODO: define buffer to match sectionsize
[InternalBufferCapacity(256)]
public struct GridTile : IBufferElementData
{
	public bool IsPlowed;
	public bool IsReserved;
	public OccupationType OccupationType;
	public Entity RenderTileEntity;
	public Entity OccupyingEntity;
}
