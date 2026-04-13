using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

partial struct WorstCollisionSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
        var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

        foreach (var (bulletTransform, bullet, bulletEntity) in SystemAPI.Query<RefRO<LocalTransform>, RefRO<Bullet>>().WithEntityAccess())
        {
            foreach (var (enemyTransform, enemy, enemyEntity) in SystemAPI.Query<RefRO<LocalTransform>, RefRO<Enemy>>().WithEntityAccess())
            {
                float3 bulletPos = bulletTransform.ValueRO.Position;
                float3 enemyPos = enemyTransform.ValueRO.Position;

                float distance = math.distance(bulletPos, enemyPos);
                float collisionRadius = bullet.ValueRO.Radius;
                float bulletDamage = bullet.ValueRO.Power;

                if (distance <= collisionRadius)
                {
                    ecb.DestroyEntity(enemyEntity);
                    // todo: enemyEntity에게 bulletDamage 입히기
                    break;
                }
            }
        }
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {
        
    }
}
