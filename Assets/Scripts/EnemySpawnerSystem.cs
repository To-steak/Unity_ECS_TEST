using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

partial struct EnemySpawnerSystem : ISystem
{
    private Random _random;
    private EntityQuery _enemyQuery;

    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<EnemySpawner>();

        _random = new Random((uint)System.DateTime.Now.Ticks);
        _enemyQuery = state.GetEntityQuery(ComponentType.ReadOnly<Enemy>());
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        int currentEnemyCount = _enemyQuery.CalculateEntityCount();
        var spawner = SystemAPI.GetSingleton<EnemySpawner>();
        int targetCount = spawner.SpawnCount;

        if (currentEnemyCount < targetCount)
        {
            int size = targetCount - currentEnemyCount;
            NativeArray<Entity> enemies = new NativeArray<Entity>(size, Allocator.Temp);
            state.EntityManager.Instantiate(spawner.EnemyPrefab, enemies);
            foreach (var enemy in enemies)
            {
                float2 randomPos2D = _random.NextFloat2Direction() * _random.NextFloat(0f, 1000f);
                float3 spawnPosition = new float3(randomPos2D.x, 0, randomPos2D.y);

                state.EntityManager.SetComponentData(enemy, LocalTransform.FromPositionRotationScale(spawnPosition, quaternion.identity, 15.0f));
            }

            enemies.Dispose();
        }
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {

    }
}
