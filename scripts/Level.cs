using Godot;
using System.Collections.Generic;

public class Block
{
    public enum BlockState { Solid, Hollow, Gone }

    public float Dist;
    public StaticBody3D Body;
    public MeshInstance3D MeshInstance;
    public BlockState State = BlockState.Solid;
    public float HollowAt;
    public float DestroyAt;
}

// Runtime wrapper around the baked level.tscn: rebuilds the hollowing
// bookkeeping by scanning the baked block nodes when the scene loads.
public class Level
{
    public static readonly float[] Rings = { 8f, 14f, 20f };
    public const float FloorRadius = 22.5f;

    public Node3D Root { get; }
    public List<Block> SortedBlocks { get; } = new();

    public Level(Node3D root)
    {
        Root = root;

        foreach (Node child in root.GetChildren())
        {
            if (child is not StaticBody3D body)
                continue;

            MeshInstance3D mesh = null;
            foreach (Node sub in body.GetChildren())
            {
                if (sub is MeshInstance3D mi)
                {
                    mesh = mi;
                    break;
                }
            }

            Vector3 p = body.Position;
            SortedBlocks.Add(new Block
            {
                Dist = Mathf.Sqrt(p.X * p.X + p.Z * p.Z),
                Body = body,
                MeshInstance = mesh,
            });
        }
        SortedBlocks.Sort((a, b) => a.Dist.CompareTo(b.Dist));
    }
}
