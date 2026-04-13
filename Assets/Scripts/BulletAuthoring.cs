using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;

public struct Bullet : IComponentData
{
    public float3 Position;
    public float3 Direction;
    public float Speed;
    public float Radius;
    public float Power;
}

public class BulletAuthoring : MonoBehaviour
{
    public float3 position;
    public float3 direction;
    public float speed;
    public float radius;
    public float power;
}

class BulletBaker : Baker<BulletAuthoring>
{
    public override void Bake(BulletAuthoring authoring)
    {
        var entity = GetEntity(TransformUsageFlags.Dynamic);

        AddComponent(entity, new Bullet
        {
            Position = authoring.position,
            Direction = authoring.direction,
            Speed = authoring.speed,
            Radius = authoring.radius,
            Power = authoring.power
        });
    }
}