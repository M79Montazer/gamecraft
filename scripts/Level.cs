using Godot;
using System.Collections.Generic;

// MeshLibrary item ids of assets/blocks.tres, shared by the editor palette
// and the runtime. Other libraries don't need constants: their hollow
// variants are resolved by item-name convention (see Level).
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

    public GridMap Map;
    public float Dist;
    public Vector3I Cell;
    public BlockState State = BlockState.Solid;
    public float HollowAt;
    public float DestroyAt;
    // Item id to swap to when hollowed; -1 for blocks that are already
    // hollow variants and only need to crumble away.
    public int HollowId = -1;
    public int Orientation;
}

// Runtime wrapper around a level's GridMaps: builds the hollowing
// bookkeeping by scanning the occupied cells when the scene loads.
//
// A level may contain any number of GridMap nodes; each solid item is
// hollowed into its library's matching item, resolved by name:
// an item named "hollow_X" is the hollow variant of "X", and a library
// with a plain "hollow" item uses it for everything else. So new level
// geometry works without code changes as long as the library follows
// that naming.
public class Level
{
    public float[] Rings { get; }
    public List<Block> SortedBlocks { get; } = new();

    public Level(IReadOnlyList<GridMap> maps, float[] rings)
    {
        Rings = rings;

        foreach (GridMap map in maps)
        {
            Dictionary<int, int> hollowOf = BuildHollowMapping(map.MeshLibrary);
            foreach (Vector3I cell in map.GetUsedCells())
            {
                Vector3 p = map.ToGlobal(map.MapToLocal(cell));
                SortedBlocks.Add(new Block
                {
                    Map = map,
                    Dist = Mathf.Sqrt(p.X * p.X + p.Z * p.Z),
                    Cell = cell,
                    HollowId = hollowOf.GetValueOrDefault(map.GetCellItem(cell), -1),
                    Orientation = map.GetCellItemOrientation(cell),
                });
            }
        }
        SortedBlocks.Sort((a, b) => a.Dist.CompareTo(b.Dist));
    }

    private static Dictionary<int, int> BuildHollowMapping(MeshLibrary library)
    {
        var hollowOf = new Dictionary<int, int>();
        int universal = -1;
        var variantOf = new Dictionary<string, int>();
        foreach (int id in library.GetItemList())
        {
            string name = library.GetItemName(id);
            if (string.IsNullOrEmpty(name))
                continue;
            if (name == "hollow")
                universal = id;
            else if (name.StartsWith("hollow_"))
                variantOf[name.Substring("hollow_".Length)] = id;
        }
        foreach (int id in library.GetItemList())
        {
            string name = library.GetItemName(id);
            if (variantOf.TryGetValue(name, out int variant))
                hollowOf[id] = variant;
            else if (universal >= 0 && !(name == "hollow" || name.StartsWith("hollow_")))
                hollowOf[id] = universal;
        }
        return hollowOf;
    }

    public void MakeHollow(Block b)
    {
        if (b.HollowId >= 0)
            b.Map.SetCellItem(b.Cell, b.HollowId, b.Orientation);
        b.State = Block.BlockState.Hollow;
    }

    public void Destroy(Block b)
    {
        b.Map.SetCellItem(b.Cell, -1);
        b.State = Block.BlockState.Gone;
    }
}
