using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[BurstCompile]
partial struct BulletSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        // 1. 필요한 데이터 준비
        float deltaTime = SystemAPI.Time.DeltaTime;
        float maxDistanceSq = 1000f * 1000f; // 미리 제곱하여 연산량 감소

        // 2. 병렬 기록을 위한 ECB 발급
        var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
        var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();

        // 3. 병렬 잡(Job) 생성 및 스케줄링
        var moveJob = new BulletMoveParallelJob
        {
            DeltaTime = deltaTime,
            MaxDistanceSq = maxDistanceSq,
            Ecb = ecb
        };

        // 모든 가용 스레드에 작업 분배
        state.Dependency = moveJob.ScheduleParallel(state.Dependency);
    }
}

[BurstCompile]
partial struct BulletMoveParallelJob : IJobEntity
{
    public float DeltaTime;
    public float MaxDistanceSq;
    public EntityCommandBuffer.ParallelWriter Ecb;

    // 매개변수에 'Entity entity'를 추가하는 것만으로 충분합니다.
    void Execute([EntityIndexInQuery] int sortIndex, ref LocalTransform transform, in Bullet bullet, Entity entity)
    {
        transform.Position += bullet.Direction * bullet.Speed * DeltaTime;

        if (math.lengthsq(transform.Position) > MaxDistanceSq)
        {
            // 이제 'entity' 변수를 바로 사용할 수 있습니다.
            Ecb.DestroyEntity(sortIndex, entity);
        }
    }
}