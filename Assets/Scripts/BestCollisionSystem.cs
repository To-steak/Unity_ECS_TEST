using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

partial struct BestCollisionSystem : ISystem
{
    private const float CELL_SIZE = 5f;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {

    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
        var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

        var enemyGrid = new NativeParallelMultiHashMap<int2, EnemyGridData>(1000, Allocator.Temp);

        foreach (var (enemyTransform, enemy, enemyEntity) in SystemAPI.Query<RefRO<LocalTransform>, RefRO<Enemy>>().WithEntityAccess())
        {
            float3 position = enemyTransform.ValueRO.Position;
            int2 cellCoord = new int2((int)math.floor(position.x / CELL_SIZE), (int)math.floor(position.z / CELL_SIZE));

            enemyGrid.Add(cellCoord, new EnemyGridData
            {
                Entity = enemyEntity,
                Position = position
            });
        }

        foreach (var (bulletTransform, bullet, bulletEntity) in SystemAPI.Query<RefRO<LocalTransform>, RefRO<Bullet>>().WithEntityAccess())
        {
            float3 position = bulletTransform.ValueRO.Position;
            float radius = bullet.ValueRO.Radius;
            int2 cellCoord = new int2((int)math.floor(position.x / CELL_SIZE), (int)math.floor(position.z / CELL_SIZE));

            bool hit = false;

            for (int i = -1; i <= 1; i++)
            {
                for (int j = -1; j <= 1; j++)
                {
                    int2 checkCell = cellCoord + new int2(i, j);

                    if (enemyGrid.TryGetFirstValue(checkCell, out var enemyData, out var iterator))
                    {
                        do
                        {
                            float distance = math.distance(position, enemyData.Position);

                            if (distance <= radius)
                            {
                                ecb.DestroyEntity(enemyData.Entity);
                                hit = true;
                                break;
                            }
                        }
                        while (enemyGrid.TryGetNextValue(out enemyData, ref iterator));
                    }
                    if (hit)
                    {
                        break;
                    }
                }
                if (hit)
                {
                    break;
                }
            }
        }

        enemyGrid.Dispose();
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {

    }
}

struct EnemyGridData
{
    public Entity Entity;
    public float3 Position;
}