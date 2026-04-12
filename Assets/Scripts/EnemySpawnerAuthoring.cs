using Unity.Entities;
using UnityEngine;

public struct EnemySpawner : IComponentData
{
    public Entity EnemyPrefab;
    public int SpawnCount;
}

class EnemySpawnerAuthoring : MonoBehaviour
{
    public GameObject enemyPrefab;
    public int spawnCount;
}

class EnemySpawnerAuthoringBaker : Baker<EnemySpawnerAuthoring>
{
    public override void Bake(EnemySpawnerAuthoring authoring)
    {
        var entity = GetEntity(TransformUsageFlags.None);

        AddComponent(entity, new EnemySpawner
        {
            EnemyPrefab = GetEntity(authoring.enemyPrefab, TransformUsageFlags.Dynamic),
            SpawnCount = authoring.spawnCount
        });
    }
}
