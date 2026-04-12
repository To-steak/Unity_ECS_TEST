using Unity.Entities;
using UnityEngine;

public struct Enemy : IComponentData
{
    public float MoveSpeed;
}

class EnemyAuthoring : MonoBehaviour
{
    public float moveSpeed;
}

class EnemyAuthoringBaker : Baker<EnemyAuthoring>
{
    public override void Bake(EnemyAuthoring authoring)
    {
        var entity = GetEntity(TransformUsageFlags.Dynamic);

        AddComponent(entity, new Enemy
        {
           MoveSpeed = authoring.moveSpeed 
        });
    }
}
