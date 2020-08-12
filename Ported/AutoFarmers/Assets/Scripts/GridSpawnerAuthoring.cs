using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

[GenerateAuthoringComponent]
public struct GridSpawner : IComponentData
{
	public int2 GridSectionDimensions;
	public int2 GridSectionCount;
	public float3 GridWorldScale;
	public Entity TilePrefab;
}
