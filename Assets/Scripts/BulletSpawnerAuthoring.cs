using Unity.Entities;
using UnityEngine;

public struct BulletSpawner : IComponentData
{
    public Entity BulletPrefab;
    public int SpawnCount;
}

class BulletSpawnerAuthoring : MonoBehaviour
{
    public GameObject bulletPrefab;
    public int spawnCount;
}

class BulletSpawnerAuthoringBaker : Baker<BulletSpawnerAuthoring>
{
    public override void Bake(BulletSpawnerAuthoring authoring)
    {
        var entity = GetEntity(TransformUsageFlags.None);

        AddComponent(entity, new BulletSpawner
        {
           BulletPrefab = GetEntity(authoring.bulletPrefab, TransformUsageFlags.Dynamic),
           SpawnCount = authoring.spawnCount
        });
    }
}
