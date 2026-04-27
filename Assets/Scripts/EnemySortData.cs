
using Unity.Entities;
using Unity.Mathematics;

struct EnemySortData : System.IComparable<EnemySortData>
{
    public int CellIndex;
    public Entity Entity;
    public float3 Position;

    public int CompareTo(EnemySortData other)
    {
        return CellIndex.CompareTo(other.CellIndex);
    }
}