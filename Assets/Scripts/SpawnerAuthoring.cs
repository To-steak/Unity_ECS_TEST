using Unity.Entities;
using UnityEngine;

public struct Spawner : IComponentData
{
    public Entity BulletPrefab;
    public int SpawnCount;
}

class SpawnerAuthoring : MonoBehaviour
{
    public GameObject bulletPrefab;
    public int spawnCount;
}

class SpawnerAuthoringBaker : Baker<SpawnerAuthoring>
{
    public override void Bake(SpawnerAuthoring authoring)
    {
        var entity = GetEntity(TransformUsageFlags.None);

        AddComponent(entity, new Spawner
        {
           BulletPrefab = GetEntity(authoring.bulletPrefab, TransformUsageFlags.Dynamic),
           SpawnCount = authoring.spawnCount
        });
    }
}
