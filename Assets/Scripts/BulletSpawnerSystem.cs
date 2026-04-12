using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

partial struct BulletSpawnerSystem : ISystem
{
    private Random _random;
    private EntityQuery _bulletQuery;

    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<BulletSpawner>();
        
        _random = new Random((uint)System.DateTime.Now.Ticks);
        _bulletQuery = state.GetEntityQuery(ComponentType.ReadOnly<Bullet>());
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        int currentBulletCount = _bulletQuery.CalculateEntityCount();
        var spawner = SystemAPI.GetSingleton<BulletSpawner>();
        int targetCount = spawner.SpawnCount;
        
        if (currentBulletCount < targetCount)
        {
            int size = targetCount - currentBulletCount;
            NativeArray<Entity> bullets = new NativeArray<Entity>(size, Allocator.Temp);
            state.EntityManager.Instantiate(spawner.BulletPrefab, bullets);
            foreach (var bullet in bullets)
            {
                float2 random2D = _random.NextFloat2Direction();
                float3 randomDirection = new float3(random2D.x, 0, random2D.y);

                float randomSpeed = _random.NextFloat(3f, 8f);

                float2 randomPos2D = _random.NextFloat2Direction() * _random.NextFloat(0f, 1f);
                float3 spawnPosition = new float3(randomPos2D.x, 0, randomPos2D.y);
                
                state.EntityManager.SetComponentData(bullet, LocalTransform.FromPosition(spawnPosition));
                state.EntityManager.SetComponentData(bullet, new Bullet
                {
                    Direction = randomDirection,
                    Speed = randomSpeed,
                    Radius = 0.5f,
                    Power = 15
                });
            }

            bullets.Dispose();
        }
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {

    }
}
