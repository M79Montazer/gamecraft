using Godot;
using System.Collections.Generic;

// MeshLibrary item ids shared by the baker and the runtime.
public static class BlockItem
{
    public const int Floor = 0;
    public const int Wall = 1;
    public const int Beacon = 2;
    public const int Hollow = 3;
}

public class Block
{
    public enum BlockState { Solid, Hollow, Gone }

    public float Dist;
    public Vector3I Cell;
    public BlockState State = BlockState.Solid;
    public float HollowAt;
    public float DestroyAt;
}

// Runtime wrapper around a level's GridMap: builds the hollowing bookkeeping
// by scanning the occupied cells when the scene loads.
public class Level
{
    public static readonly float[] Rings = { 8f, 14f, 20f };
    public const float FloorRadius = 22.5f;

    public GridMap Map { get; }
    public List<Block> SortedBlocks { get; } = new();

    public Level(GridMap map)
    {
        Map = map;

        foreach (Vector3I cell in map.GetUsedCells())
        {
            Vector3 p = map.MapToLocal(cell);
            SortedBlocks.Add(new Block
            {
                Dist = Mathf.Sqrt(p.X * p.X + p.Z * p.Z),
                Cell = cell,
            });
        }
        SortedBlocks.Sort((a, b) => a.Dist.CompareTo(b.Dist));
    }

    public void MakeHollow(Block b)
    {
        Map.SetCellItem(b.Cell, BlockItem.Hollow);
        b.State = Block.BlockState.Hollow;
    }

    public void Destroy(Block b)
    {
        Map.SetCellItem(b.Cell, -1);
        b.State = Block.BlockState.Gone;
    }
}
