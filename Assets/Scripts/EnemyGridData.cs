using Unity.Entities;
using Unity.Mathematics;

struct EnemyGridData : System.IComparable<EnemyGridData>
{
    public int CellIndex;
    public Entity Entity;
    public float3 Position;

    public int CompareTo(EnemyGridData other)
    {
        return CellIndex.CompareTo(other.CellIndex);
    }
}