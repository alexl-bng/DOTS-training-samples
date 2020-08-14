using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

//[UpdateAfter(typeof(F))]
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
            ForEach((int entityInQueryIndex, Entity entity, ref Translation translation, ref Rock rockData) =>
            {
                if (rockData.health > 0)
                {
                    translation.Value.y -= ((float)(breakRate) * deltaTime);
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
            .ForEach((int entityInQueryIndex, Entity entity, in Translation translation) =>
        {
            ecb.SetComponent(entityInQueryIndex, entity, new Translation()
            {
                Value = new float3(translation.Value.x, translation.Value.y + ((float)(math.sin(time) * deltaTime) / 5.0f), translation.Value.z)
            });
        }).ScheduleParallel();
        
        m_ecb.AddJobHandleForProducer(Dependency);
    }
}
