using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

public class DetermineIntentSystem_Farmer : SystemBase
{
	protected override void OnUpdate()
	{
		Entities
			.WithAll<Farmer, WorkerIntent_None>()			
			.ForEach((
				Farmer entity,
				RandomNumberGenerator rng
				) =>
		{
			int nextStateIndex = rng.rng.NextInt(0, 3);
		}).Schedule();
	}
}


public class DetermineIntentSystem_Drone: SystemBase
{
	protected override void OnUpdate()
	{
		Entities.ForEach((Drone entity) =>
		{

		}).Schedule();
	}
}