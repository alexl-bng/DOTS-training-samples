using Unity.Entities;
using Unity.Mathematics;

[GenerateAuthoringComponent]
public struct ResourceManager : IComponentData
{
	public int FarmerPrice;
	public int DronePrice;
	public int FarmerCoins;
	public int DroneCoins;
}
