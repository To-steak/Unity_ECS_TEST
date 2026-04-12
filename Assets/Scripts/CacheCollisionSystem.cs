
using Unity.Burst;
using Unity.Entities;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Transforms;

partial struct CacheCollisionSystem : ISystem
{
    private const float CELL_SIZE = 10f;
    private const int GRID_SIZE = 200;
    private const int GRID_OFFSET = 100;
    private const int TOTAL_CELLS = GRID_SIZE * GRID_SIZE;

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
        var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);
        var occupancyBits = new NativeBitArray(TOTAL_CELLS, Allocator.Temp, NativeArrayOptions.ClearMemory);
        var enemyGrid = new NativeParallelMultiHashMap<int2, EnemyGridData>(1000, Allocator.Temp);

        foreach (var (enemyTransform, enemy, enemyEntity) in SystemAPI.Query<RefRO<LocalTransform>, RefRO<Enemy>>().WithEntityAccess())
        {
            float3 position = enemyTransform.ValueRO.Position;
            int2 cellCoord = new int2((int)math.floor(position.x / CELL_SIZE), (int)math.floor(position.z / CELL_SIZE));

            int x = cellCoord.x + GRID_OFFSET;
            int z = cellCoord.y + GRID_OFFSET;
            if (x >= 0 && x < GRID_SIZE && z >= 0 && z < GRID_SIZE)
            {
                enemyGrid.Add(cellCoord, new EnemyGridData
                {
                    Entity = enemyEntity,
                    Position = position
                });

                // GetLinearIndex 인라인
                int linearIndex = z * GRID_SIZE + x;
                occupancyBits.Set(linearIndex, true);
            }
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
                    int checkX = checkCell.x + GRID_OFFSET;
                    int checkZ = checkCell.y + GRID_OFFSET;
                    
                    if (checkX < 0 || checkX >= GRID_SIZE || checkZ < 0 || checkZ >= GRID_SIZE)
                    {
                        continue;
                    }

                    int linearIndex = checkZ * GRID_SIZE + checkX;

                    if (!occupancyBits.IsSet(linearIndex))
                    {
                        continue;
                    }

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
                    if (hit) break;
                }
                if (hit) break;
            }
        }

        occupancyBits.Dispose();
        enemyGrid.Dispose();
    }
}