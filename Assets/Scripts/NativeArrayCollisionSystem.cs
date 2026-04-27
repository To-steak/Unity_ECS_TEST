using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Jobs;

[BurstCompile]
partial struct NativeArrayCollisionSystem : ISystem
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

        // 병렬 처리를 위한 ECB 발급
        var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
        var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();

        // 잡(Job)에 전달하기 위해 Allocator.TempJob 사용
        var enemyDataArray = new NativeArray<EnemySortData>(enemyCount, Allocator.TempJob);
        var gridOffsets = new NativeArray<int>(TOTAL_CELLS, Allocator.TempJob);
        var gridCounts = new NativeArray<int>(TOTAL_CELLS, Allocator.TempJob);

        // 초기화
        for (int i = 0; i < TOTAL_CELLS; i++)
        {
            gridOffsets[i] = -1;
            gridCounts[i] = 0;
        }

        // 1. 적 데이터 수집 (메인 스레드 순차 처리)
        int index = 0;
        foreach (var (transform, enemy, entity) in SystemAPI.Query<RefRO<LocalTransform>, RefRO<Enemy>>().WithEntityAccess())
        {
            float3 pos = transform.ValueRO.Position;
            int2 cellCoord = new int2((int)math.floor(pos.x / CELL_SIZE), (int)math.floor(pos.z / CELL_SIZE));
            int x = cellCoord.x + GRID_OFFSET;
            int z = cellCoord.y + GRID_OFFSET;

            int linearIndex = -1;
            if (x >= 0 && x < GRID_SIZE && z >= 0 && z < GRID_SIZE)
                linearIndex = z * GRID_SIZE + x;

            enemyDataArray[index++] = new EnemySortData
            {
                CellIndex = linearIndex,
                Entity = entity,
                Position = pos
            };
        }

        // 2. 정렬 및 오프셋 계산 (메인 스레드)
        enemyDataArray.Sort();

        for (int i = 0; i < enemyDataArray.Length; i++)
        {
            int cellIndex = enemyDataArray[i].CellIndex;
            if (cellIndex == -1) continue;
            if (gridOffsets[cellIndex] == -1) gridOffsets[cellIndex] = i;
            gridCounts[cellIndex]++;
        }

        // 3. 병렬 충돌 검사 잡 스케줄링
        var collisionJob = new CollisionParallelJob
        {
            Ecb = ecb,
            EnemyDataArray = enemyDataArray,
            GridOffsets = gridOffsets,
            GridCounts = gridCounts,
            GridSize = GRID_SIZE,
            GridOffset = GRID_OFFSET,
            CellSize = CELL_SIZE
        };

        // ScheduleParallel()을 통해 모든 가용 코어(Worker Threads)로 분산
        state.Dependency = collisionJob.ScheduleParallel(state.Dependency);

        // 사용한 메모리 해제 예약 (잡이 끝난 뒤 실행됨)
        enemyDataArray.Dispose(state.Dependency);
        gridOffsets.Dispose(state.Dependency);
        gridCounts.Dispose(state.Dependency);
    }
}

[BurstCompile]
partial struct CollisionParallelJob : IJobEntity
{
    public EntityCommandBuffer.ParallelWriter Ecb;
    [ReadOnly] public NativeArray<EnemySortData> EnemyDataArray;
    [ReadOnly] public NativeArray<int> GridOffsets;
    [ReadOnly] public NativeArray<int> GridCounts;

    public int GridSize;
    public int GridOffset;
    public float CellSize;

    // IJobEntity를 통해 모든 탄환(Bullet)을 병렬로 처리
    // [EntityIndexInQuery]는 병렬 ECB 기록 시 순서를 보장하기 위한 인덱스
    void Execute([EntityIndexInQuery] int sortIndex, in LocalTransform transform, in Bullet bullet)
    {
        float3 pos = transform.Position;
        float radius = bullet.Radius;
        int2 cellCoord = new int2((int)math.floor(pos.x / CellSize), (int)math.floor(pos.z / CellSize));

        for (int i = -1; i <= 1; i++)
        {
            for (int j = -1; j <= 1; j++)
            {
                int checkX = cellCoord.x + i + GridOffset;
                int checkZ = cellCoord.y + j + GridOffset;

                if (checkX < 0 || checkX >= GridSize || checkZ < 0 || checkZ >= GridSize)
                    continue;

                int linearIndex = checkZ * GridSize + checkX;
                int offset = GridOffsets[linearIndex];

                if (offset == -1) continue;

                int count = GridCounts[linearIndex];
                int endIdx = offset + count;

                for (int k = offset; k < endIdx; k++)
                {
                    var enemyData = EnemyDataArray[k];
                    if (math.distance(pos, enemyData.Position) <= radius)
                    {
                        // 병렬 쓰기를 위해 sortIndex 전달
                        Ecb.DestroyEntity(sortIndex, enemyData.Entity);
                        return; // 한 번 충돌하면 해당 탄환 검사 종료
                    }
                }
            }
        }
    }
}