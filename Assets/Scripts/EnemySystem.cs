using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

partial struct EnemySystem : ISystem
{
    private Random _random;

    public void OnCreate(ref SystemState state)
    {
        _random = new Random((uint)System.DateTime.Now.Ticks);
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        float deltaTime = SystemAPI.Time.DeltaTime;
        float range = 100f * 100f;

        foreach (var (transform, enemy) in SystemAPI.Query<RefRW<LocalTransform>, RefRO<Enemy>>())
        {
            float2 random2D = _random.NextFloat2Direction();
            float3 moveDirection = new float3(random2D.x, 0, random2D.y);

            transform.ValueRW.Position += moveDirection * enemy.ValueRO.MoveSpeed * deltaTime;
            if(math.lengthsq(transform.ValueRO.Position) > range)
            {
                transform.ValueRW.Position = float3.zero;
            }
        }
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {
        
    }
}
