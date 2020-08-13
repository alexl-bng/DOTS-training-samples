﻿using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using UnityEngine;

[MaterialProperty("_BaseColor", MaterialPropertyFormat.Float4)]
public struct MeshColor : IComponentData
{
	public float4 Value;
}

public class MeshColorAuthoring : MonoBehaviour, IConvertGameObjectToEntity
{
	public UnityEngine.Color Color;
	public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
	{
		dstManager.AddComponentData(entity, new MeshColor
		{
			Value = new float4(Color.r, Color.g, Color.b, 1)
		});
	}
}
