using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

partial struct EnemySystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        // 병렬 잡에서는 스레드별 독립적인 난수 상태가 필요하므로 시스템 레벨의 _random은 제거
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        // Random의 Seed는 0이 될 수 없으므로, ElapsedTime을 기반으로 안전한 기준 시드 생성
        uint baseSeed = (uint)(SystemAPI.Time.ElapsedTime * 1000000.0) + 1;

        var moveJob = new EnemyMoveParallelJob
        {
            DeltaTime = SystemAPI.Time.DeltaTime,
            RangeSq = 1000f * 1000f,
            BaseSeed = baseSeed
        };

        // 워커 스레드 전체로 작업 분산
        state.Dependency = moveJob.ScheduleParallel(state.Dependency);
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {
    }
}

[BurstCompile]
partial struct EnemyMoveParallelJob : IJobEntity
{
    public float DeltaTime;
    public float RangeSq;
    public uint BaseSeed;

    // [EntityIndexInQuery]를 활용해 각 엔티티마다 고유한 난수 상태를 할당
    void Execute([EntityIndexInQuery] int entityIndex, ref LocalTransform transform, in Enemy enemy)
    {
        // 기준 시드에 엔티티 인덱스를 더해 스레드 경합이 없는 독립적인 Random 인스턴스 생성
        var random = Unity.Mathematics.Random.CreateFromIndex(BaseSeed + (uint)entityIndex);

        float2 random2D = random.NextFloat2Direction();
        float3 moveDirection = new float3(random2D.x, 0, random2D.y);

        transform.Position += moveDirection * enemy.MoveSpeed * DeltaTime;

        if (math.lengthsq(transform.Position) > RangeSq)
        {
            transform.Position = float3.zero;
        }
    }
}