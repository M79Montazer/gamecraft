using Godot;
using System;
using System.Collections.Generic;

// One-shot level baker. Run from the project root after `dotnet build`:
//   godot --headless --path . --script res://scripts/BakeLevel.cs
// Overwrites res://scenes/level.tscn — hand edits made in the editor are lost on re-bake.
public partial class BakeLevel : SceneTree
{
    private const float WallBand = 0.6f;      // |dist - R| window for wall cells
    private const float GateHalfWidth = 1.5f; // gate gap half-arc, in blocks
    private const string BlockMeshPath = "res://assets/block_mesh.res";
    private const string LevelPath = "res://scenes/level.tscn";

    // fixed gate angles (degrees) per ring, deterministic so runs are comparable
    private static readonly float[][] RingGates =
    {
        new[] { 90f, 240f },
        new[] { 0f, 150f },
        new[] { 60f, 200f, 300f },
    };

    public override void _Initialize()
    {
        int exitCode = 0;
        try
        {
            GameAssets.Load();

            // save the block mesh as a standalone resource so level.tscn references
            // it externally instead of embedding a copy
            ResourceSaver.Save(GameAssets.Block, BlockMeshPath);
            Mesh blockMesh = ResourceLoader.Load<Mesh>(BlockMeshPath);
            if (blockMesh == null)
                throw new InvalidOperationException($"failed to load {BlockMeshPath}");

            Material wallMat = new StandardMaterial3D { AlbedoColor = new Color(0.22f, 0.24f, 0.30f) };
            Material beaconMat = new StandardMaterial3D
            {
                AlbedoColor = new Color(0.10f, 0.45f, 0.55f),
                EmissionEnabled = true,
                Emission = new Color(0.2f, 0.9f, 1f),
                EmissionEnergyMultiplier = 2.5f,
            };

            var root = new Node3D { Name = "Level" };
            BuildLevel(root, blockMesh, wallMat, beaconMat);

            var packed = new PackedScene();
            Error packErr = packed.Pack(root);
            if (packErr != Error.Ok)
                throw new InvalidOperationException($"level pack failed: {packErr}");
            Error saveErr = ResourceSaver.Save(packed, LevelPath);
            if (saveErr != Error.Ok)
                throw new InvalidOperationException($"level save failed: {saveErr}");

            GD.Print($"baked {LevelPath}: {root.GetChildCount()} bodies");
            root.Free();
        }
        catch (Exception e)
        {
            GD.PushError($"bake failed: {e}");
            exitCode = 1;
        }
        Quit(exitCode);
    }

    private static void BuildLevel(Node3D root, Mesh blockMesh, Material wallMat, Material beaconMat)
    {
        int range = (int)Mathf.Ceil(Level.FloorRadius);

        // floor: solid disc of blocks
        for (int i = -range; i <= range; i++)
            for (int j = -range; j <= range; j++)
            {
                if (Mathf.Sqrt(i * i + j * j) > Level.FloorRadius)
                    continue;
                AddBlock(root, blockMesh, i, 0.5f, j, null);
            }

        // ring walls with gates and high/low segments
        for (int r = 0; r < Level.Rings.Length; r++)
        {
            float ringRadius = Level.Rings[r];
            float baseRot = r * 47f;
            var occupied = new HashSet<(int, int)>();

            // glowing beacon pillars marking each gate, placed first so walls skip their cells
            foreach (float gateDeg in RingGates[r])
                foreach (float side in new[] { -1f, 1f })
                {
                    float a = Mathf.DegToRad(gateDeg) + side * (GateHalfWidth + 0.8f) / ringRadius;
                    int bi = Mathf.RoundToInt(Mathf.Cos(a) * ringRadius);
                    int bj = Mathf.RoundToInt(Mathf.Sin(a) * ringRadius);
                    if (!occupied.Add((bi, bj)))
                        continue;
                    AddBlock(root, blockMesh, bi, 1.5f, bj, beaconMat);
                    AddBlock(root, blockMesh, bi, 2.5f, bj, beaconMat);
                }

            for (int i = -range; i <= range; i++)
                for (int j = -range; j <= range; j++)
                {
                    float d = Mathf.Sqrt(i * i + j * j);
                    if (Mathf.Abs(d - ringRadius) > WallBand || occupied.Contains((i, j)))
                        continue;

                    float ang = Mathf.RadToDeg(Mathf.Atan2(j, i));
                    if (IsGate(r, ang))
                        continue;

                    int height = IsHighSpan(baseRot, ang) ? 2 : 1;
                    for (int y = 0; y < height; y++)
                        AddBlock(root, blockMesh, i, 1.5f + y, j, wallMat);
                }
        }
    }

    private static bool IsGate(int ring, float angDeg)
    {
        float halfDeg = Mathf.RadToDeg(GateHalfWidth / Level.Rings[ring]);
        foreach (float g in RingGates[ring])
        {
            float diff = Mathf.Wrap(angDeg - g, -180f, 180f);
            if (Mathf.Abs(diff) < halfDeg)
                return true;
        }
        return false;
    }

    private static bool IsHighSpan(float baseRot, float angDeg)
    {
        // two ~70-degree segments of 2-high wall per ring; rest is 1-high (jumpable)
        float a = Mathf.Wrap(angDeg - baseRot, -180f, 180f);
        return Mathf.Abs(a - 35f) < 35f || Mathf.Abs(a + 145f) < 35f;
    }

    private static void AddBlock(Node3D root, Mesh blockMesh, float x, float y, float z, Material mat)
    {
        var body = new StaticBody3D { Position = new Vector3(x, y, z) };
        var shape = new CollisionShape3D { Shape = GameAssets.CubeShape };
        body.AddChild(shape);

        var mi = new MeshInstance3D { Mesh = blockMesh };
        if (mat != null)
            mi.MaterialOverride = mat;
        body.AddChild(mi);
        root.AddChild(body);

        // PackedScene.Pack only serializes nodes owned by the packed root
        body.Owner = root;
        shape.Owner = root;
        mi.Owner = root;
    }
}
