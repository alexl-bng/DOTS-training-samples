using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

[GenerateAuthoringComponent]
public struct Path : IComponentData
{
    public float3 sourcePosition;
    public float3 targetPosition;
    public float speed;
    public float progress;
    public float smoothingFactor;
}
