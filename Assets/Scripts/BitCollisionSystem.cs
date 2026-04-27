using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

partial struct BitCollisionSystem : ISystem
{
    private const float CELL_SIZE = 10f;
    private const int GRID_SIZE = 200;
    private const int GRID_OFFSET = 100;
    private const int TOTAL_CELLS = GRID_SIZE * GRID_SIZE;

    private EntityQuery _enemyQuery;

    public void OnCreate(ref SystemState state)
    {
        _enemyQuery = state.GetEntityQuery(ComponentType.ReadOnly<Enemy>(), ComponentType.ReadOnly<LocalTransform>());
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        int enemyCount = _enemyQuery.CalculateEntityCount();
        if (enemyCount == 0) return;

        var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
        var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

        // 메모리 할당 (BitArray + 정렬용 배열 + 오프셋)
        var occupancyBits = new NativeBitArray(TOTAL_CELLS, Allocator.Temp, NativeArrayOptions.ClearMemory);
        var enemyDataArray = new NativeArray<EnemyGridData>(enemyCount, Allocator.Temp);
        var gridOffsets = new NativeArray<int>(TOTAL_CELLS, Allocator.Temp);
        var gridCounts = new NativeArray<int>(TOTAL_CELLS, Allocator.Temp);

        for (int i = 0; i < TOTAL_CELLS; i++)
        {
            gridOffsets[i] = -1;
            gridCounts[i] = 0;
        }

        // 1. 데이터 수집 및 비트 세팅
        int index = 0;
        foreach (var (enemyTransform, enemy, enemyEntity) in SystemAPI.Query<RefRO<LocalTransform>, RefRO<Enemy>>().WithEntityAccess())
        {
            float3 position = enemyTransform.ValueRO.Position;
            int2 cellCoord = new int2((int)math.floor(position.x / CELL_SIZE), (int)math.floor(position.z / CELL_SIZE));

            int x = cellCoord.x + GRID_OFFSET;
            int z = cellCoord.y + GRID_OFFSET;

            int linearIndex = -1;
            if (x >= 0 && x < GRID_SIZE && z >= 0 && z < GRID_SIZE)
            {
                linearIndex = z * GRID_SIZE + x;
                occupancyBits.Set(linearIndex, true); // 해당 셀에 적이 있음을 비트로 표시
            }

            enemyDataArray[index++] = new EnemyGridData
            {
                CellIndex = linearIndex,
                Entity = enemyEntity,
                Position = position
            };
        }

        // 2. 기수 정렬 수행
        enemyDataArray.Sort();

        // 3. 오프셋 기록
        for (int i = 0; i < enemyDataArray.Length; i++)
        {
            int cellIndex = enemyDataArray[i].CellIndex;
            if (cellIndex == -1) continue;

            if (gridOffsets[cellIndex] == -1)
            {
                gridOffsets[cellIndex] = i;
            }
            gridCounts[cellIndex]++;
        }

        // 4. 총알 순회 및 충돌 검사
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
                    int checkX = cellCoord.x + i + GRID_OFFSET;
                    int checkZ = cellCoord.y + j + GRID_OFFSET;

                    if (checkX < 0 || checkX >= GRID_SIZE || checkZ < 0 || checkZ >= GRID_SIZE)
                    {
                        continue;
                    }

                    int linearIndex = checkZ * GRID_SIZE + checkX;

                    // 1차 검문: 비트가 0이면 빈 공간이므로 즉시 스킵 (해시맵 때와 동일한 Fast-fail)
                    if (!occupancyBits.IsSet(linearIndex))
                    {
                        continue;
                    }

                    // 2차 탐색: 비트가 1일 때만 배열 오프셋 접근
                    int offset = gridOffsets[linearIndex];
                    if (offset != -1) // 안전 장치
                    {
                        int count = gridCounts[linearIndex];
                        int endIdx = offset + count;

                        for (int k = offset; k < endIdx; k++)
                        {
                            var enemyData = enemyDataArray[k];
                            float distance = math.distance(position, enemyData.Position);

                            if (distance <= radius)
                            {
                                ecb.DestroyEntity(enemyData.Entity);
                                hit = true;
                                break;
                            }
                        }
                    }
                    if (hit) break;
                }
                if (hit) break;
            }
        }

        occupancyBits.Dispose();
        enemyDataArray.Dispose();
        gridOffsets.Dispose();
        gridCounts.Dispose();
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {

    }
}