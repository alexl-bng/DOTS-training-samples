using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

[UpdateInGroup(typeof(InitializationSystemGroup))]
public class GridOccupantSystem : SystemBase
{
	private EntityQuery _gridQuery;

	protected override void OnCreate()
	{
		_gridQuery = EntityManager.CreateEntityQuery(typeof(Grid));
	}

    protected override void OnUpdate()
    {
	    EntityCommandBufferSystem ecbSystem = World.GetExistingSystem<EndInitializationEntityCommandBufferSystem>();
	    EntityCommandBuffer ecb = ecbSystem.CreateCommandBuffer();

		Entity gridEntity = _gridQuery.GetSingletonEntity();
		Grid grid = _gridQuery.GetSingleton<Grid>();
		DynamicBuffer<GridSectionReference> sectionRefs = GetBufferFromEntity<GridSectionReference>(true)[gridEntity];
		BufferFromEntity<GridTile> tileBuffers = GetBufferFromEntity<GridTile>(false);

		Entities
			.WithAll<GridOccupant, GridLocation>()
			.WithNone<RegisteredGridOccupant>()
			.ForEach((Entity entity, in GridOccupant gridOccupant, in GridLocation gridLocation) =>
		{
			int2 gridMin = gridLocation.Value;
			int2 gridMax = gridLocation.Value + gridOccupant.GridSize;

			for (int gridX = gridMin.x; gridX < gridMax.x; gridX++)
			{
				for (int gridZ = gridMin.y; gridZ < gridMax.y; gridZ++)
				{
					int sectionIndex = grid.GetSectionId(new int2(gridX, gridZ));
					int tileIndex = grid.GetTileIndex(new int2(gridX, gridZ));
					Entity sectionEntity = sectionRefs[sectionIndex].SectionEntity;

					DynamicBuffer<GridTile> tileBuffer = tileBuffers[sectionEntity];
					GridTile tile = tileBuffer[tileIndex];

					tile.OccupationType = gridOccupant.OccupationType;
					tile.OccupyingEntity = entity;

					tileBuffer[tileIndex] = tile;
				}
			}

			ecb.AddComponent<RegisteredGridOccupant>(entity, new RegisteredGridOccupant
			{
				OccupiedMin = gridMin,
				OccupiedMax = gridMax
			});
        }).Schedule();

	    Entities
		    .WithAll<RegisteredGridOccupant>()
		    .WithNone<GridLocation>()
		    .ForEach((Entity entity, in RegisteredGridOccupant registeredGridOccupant) =>
	    {
			int2 gridMin = registeredGridOccupant.OccupiedMin;
		    int2 gridMax = registeredGridOccupant.OccupiedMax;

		    for (int gridX = gridMin.x; gridX < gridMax.x; gridX++)
		    {
			    for (int gridZ = gridMin.y; gridZ < gridMax.y; gridZ++)
			    {
				    int sectionIndex = grid.GetSectionId(new int2(gridX, gridZ));
				    int tileIndex = grid.GetTileIndex(new int2(gridX, gridZ));
				    Entity sectionEntity = sectionRefs[sectionIndex].SectionEntity;

				    DynamicBuffer<GridTile> tileBuffer = tileBuffers[sectionEntity];
				    GridTile tile = tileBuffer[tileIndex];

				    tile.OccupationType = OccupationType.Unoccupied;
				    tile.OccupyingEntity = Entity.Null;

				    tileBuffer[tileIndex] = tile;
			    }
		    }

			ecb.RemoveComponent<RegisteredGridOccupant>(entity);
	    }).Schedule();

		ecbSystem.AddJobHandleForProducer(Dependency);
    }

	protected override void OnDestroy()
	{
		_gridQuery.Dispose();
	}
}
