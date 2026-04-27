using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

partial struct NativeArrayCollisionSystem : ISystem
{
    private const float CELL_SIZE = 10f;
    private const int GRID_SIZE = 200;
    private const int GRID_OFFSET = 100;
    private const int TOTAL_CELLS = GRID_SIZE * GRID_SIZE;

    private EntityQuery _enemyQuery;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        // 적의 총 개수를 미리 계산하기 위한 쿼리
        _enemyQuery = state.GetEntityQuery(ComponentType.ReadOnly<Enemy>(), ComponentType.ReadOnly<LocalTransform>());
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        int enemyCount = _enemyQuery.CalculateEntityCount();
        if (enemyCount == 0) return;

        var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
        var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

        // 1. 메모리 할당 (NativeArray)
        var enemyDataArray = new NativeArray<EnemySortData>(enemyCount, Allocator.Temp);
        var gridOffsets = new NativeArray<int>(TOTAL_CELLS, Allocator.Temp);
        var gridCounts = new NativeArray<int>(TOTAL_CELLS, Allocator.Temp);

        // 오프셋 초기화 (-1은 빈 공간을 의미)
        for (int i = 0; i < TOTAL_CELLS; i++)
        {
            gridOffsets[i] = -1;
            gridCounts[i] = 0;
        }

        // 2. 적 데이터 수집 및 1차원 셀 인덱스 맵핑
        int index = 0;
        foreach (var (transform, enemy, entity) in SystemAPI.Query<RefRO<LocalTransform>, RefRO<Enemy>>().WithEntityAccess())
        {
            float3 pos = transform.ValueRO.Position;
            int2 cellCoord = new int2((int)math.floor(pos.x / CELL_SIZE), (int)math.floor(pos.z / CELL_SIZE));

            int x = cellCoord.x + GRID_OFFSET;
            int z = cellCoord.y + GRID_OFFSET;

            int linearIndex = -1; // 범위를 벗어나면 무효한 인덱스
            if (x >= 0 && x < GRID_SIZE && z >= 0 && z < GRID_SIZE)
            {
                linearIndex = z * GRID_SIZE + x;
            }

            enemyDataArray[index++] = new EnemySortData
            {
                CellIndex = linearIndex,
                Entity = entity,
                Position = pos
            };
        }

        // 3. 배열 정렬 (CellIndex 기준 오름차순)
        enemyDataArray.Sort();

        // 4. 오프셋 및 개수 기록
        for (int i = 0; i < enemyDataArray.Length; i++)
        {
            int cellIndex = enemyDataArray[i].CellIndex;
            if (cellIndex == -1) continue; // 맵 밖의 적은 제외

            if (gridOffsets[cellIndex] == -1)
            {
                gridOffsets[cellIndex] = i; // 해당 셀의 첫 번째 요소가 시작되는 배열 인덱스 기록
            }
            gridCounts[cellIndex]++;
        }

        // 5. O(1) 연속 메모리 탐색 및 충돌 검사
        foreach (var (transform, bullet, entity) in SystemAPI.Query<RefRO<LocalTransform>, RefRO<Bullet>>().WithEntityAccess())
        {
            float3 pos = transform.ValueRO.Position;
            float radius = bullet.ValueRO.Radius;

            int2 cellCoord = new int2((int)math.floor(pos.x / CELL_SIZE), (int)math.floor(pos.z / CELL_SIZE));
            bool hit = false;

            for (int i = -1; i <= 1; i++)
            {
                for (int j = -1; j <= 1; j++)
                {
                    int checkX = cellCoord.x + i + GRID_OFFSET;
                    int checkZ = cellCoord.y + j + GRID_OFFSET;

                    if (checkX < 0 || checkX >= GRID_SIZE || checkZ < 0 || checkZ >= GRID_SIZE)
                        continue;

                    int linearIndex = checkZ * GRID_SIZE + checkX;
                    int offset = gridOffsets[linearIndex];

                    // 빈 공간이면 즉시 스킵 (BitArray의 검문소 역할을 완벽히 대체)
                    if (offset == -1) continue;

                    int count = gridCounts[linearIndex];
                    int endIdx = offset + count;

                    // 캐시에 통째로 올라간 연속된 메모리 구간을 순차적으로 읽음
                    for (int k = offset; k < endIdx; k++)
                    {
                        var enemyData = enemyDataArray[k];
                        float distance = math.distance(pos, enemyData.Position);

                        if (distance <= radius)
                        {
                            ecb.DestroyEntity(enemyData.Entity);
                            hit = true;
                            break;
                        }
                    }
                    if (hit) break;
                }
                if (hit) break;
            }
        }

        // 배열 메모리 해제
        enemyDataArray.Dispose();
        gridOffsets.Dispose();
        gridCounts.Dispose();
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {

    }
}

// 정렬을 위해 IComparable을 구현한 구조체
struct EnemySortData : System.IComparable<EnemySortData>
{
    public int CellIndex;
    public Entity Entity;
    public float3 Position;

    public int CompareTo(EnemySortData other)
    {
        return CellIndex.CompareTo(other.CellIndex);
    }
}