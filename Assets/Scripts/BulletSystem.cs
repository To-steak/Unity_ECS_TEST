using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

partial struct BulletSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        float deltaTime = SystemAPI.Time.DeltaTime;
        float maxDistance = 10f * 10f;

        var endSimulationEntityCommandBuffer = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
        var endSimulationEntity = endSimulationEntityCommandBuffer.CreateCommandBuffer(state.WorldUnmanaged);
        foreach (var (transform, bullet, entity) in SystemAPI.Query<RefRW<LocalTransform>, RefRO<Bullet>>().WithEntityAccess())
        {
            transform.ValueRW.Position += bullet.ValueRO.Direction * bullet.ValueRO.Speed * deltaTime;
            if (math.distancesq(transform.ValueRO.Position, float3.zero) > maxDistance)
            {
                endSimulationEntity.DestroyEntity(entity);
            }
        }
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {
        
    }
}
