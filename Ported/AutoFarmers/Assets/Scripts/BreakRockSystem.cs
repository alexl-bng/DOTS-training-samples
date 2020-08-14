using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

public class BreakRockSystem : SystemBase
{
    private EntityCommandBufferSystem m_ecb;
    private Random m_random;
    
    protected override void OnCreate()
    {
        m_ecb = World.GetExistingSystem<EndSimulationEntityCommandBufferSystem>();
        m_random = new Random(0x1343);
    }

    protected override void OnUpdate()
    {
        var ecb = m_ecb.CreateCommandBuffer().AsParallelWriter();
        float breakRate = 0.05f;
        float deltaTime = Time.DeltaTime;
        double time = Time.ElapsedTime;
        
        Entities.
            WithAll<Rock, WorkerIntent_Break>().
            ForEach((int entityInQueryIndex, Entity entity, ref Translation translation, ref Rock rockData, in GridOccupant gridOccupant) =>
            {
                if (rockData.health > 0)
                {
					translation.Value.y = 0.5f * (rockData.health / (gridOccupant.GridSize.x * gridOccupant.GridSize.y)) - 0.25f;
                    rockData.health -= (breakRate);
                }
                else
                {
                    translation.Value.y = 0.0f;
                    ecb.DestroyEntity(entityInQueryIndex, entity);
                }
            }).ScheduleParallel();

        var rng = m_random;
        
        Entities
            .WithAll<Farmer, WorkerIntent_Break>()
            .ForEach((int entityInQueryIndex, Entity entity, ref Translation translation) =>
        {
            translation.Value.y = translation.Value.y + ((float) (math.sin(time) * deltaTime) / 5.0f);
        }).ScheduleParallel();
        
        m_ecb.AddJobHandleForProducer(Dependency);
    }
}
