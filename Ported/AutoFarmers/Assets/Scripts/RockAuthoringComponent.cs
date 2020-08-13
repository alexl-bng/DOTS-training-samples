using Unity.Entities;
using Unity.Mathematics;

[GenerateAuthoringComponent]
public struct Rock : IComponentData
{
    public AABB bounds;
    public float health;
}
