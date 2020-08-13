using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using UnityEngine;

[MaterialProperty("_UVOffset", MaterialPropertyFormat.Float)]
public struct MeshUVTransform : IComponentData
{
	public float Value;
}

public class MeshUVTransformAuthoring : MonoBehaviour, IConvertGameObjectToEntity
{
	public float UVOffset;
	public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
	{
		dstManager.AddComponentData(entity, new MeshUVTransform
		{
			Value = UVOffset
		});
	}
}
