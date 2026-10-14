using Godot;
using System;
using System.Collections.Generic;
using Environment = Godot.Environment;

// One-shot level baker. Run from the project root after `dotnet build`:
//   godot --headless --path . --script res://scripts/BakeLevel.cs
// Saves the shared block MeshLibrary to res://assets/blocks.tres, then bakes a
// complete playable level scene (world, GridMap, player, camera, runner) under
// res://scenes/levels/<name>/. Cell (i, k, j) sits at world (i, 0.5 + k, j):
// the map node is offset half a cell so the play space matches the original
// integer block grid.
public partial class BakeLevel : SceneTree
{
    private const float WallBand = 0.6f;      // |dist - R| window for wall cells
    private const float GateHalfWidth = 1.5f; // gate gap half-arc, in blocks
    private const string MeshLibraryPath = "res://assets/blocks.tres";
    private const string PlayerScenePath = "res://scenes/player.tscn";
    private const string LevelName = "level_1";
    private static string LevelPath => $"res://scenes/levels/{LevelName}/{LevelName}.tscn";

    private static PackedScene PlayerScene => ResourceLoader.Load<PackedScene>(PlayerScenePath);

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
            GameAssets.EnsureLoaded();

            Error libErr = ResourceSaver.Save(BuildMeshLibrary(), MeshLibraryPath);
            if (libErr != Error.Ok)
                throw new InvalidOperationException($"mesh library save failed: {libErr}");
            MeshLibrary library = ResourceLoader.Load<MeshLibrary>(MeshLibraryPath);
            if (library == null)
                throw new InvalidOperationException($"failed to load {MeshLibraryPath}");

            Node3D root = BuildLevelScene(library);

            Error dirErr = DirAccess.MakeDirRecursiveAbsolute(LevelPath.GetBaseDir());
            if (dirErr != Error.Ok && dirErr != Error.AlreadyExists)
                throw new InvalidOperationException($"dir create failed: {dirErr}");

            var packed = new PackedScene();
            Error packErr = packed.Pack(root);
            if (packErr != Error.Ok)
                throw new InvalidOperationException($"level pack failed: {packErr}");
            Error saveErr = ResourceSaver.Save(packed, LevelPath);
            if (saveErr != Error.Ok)
                throw new InvalidOperationException($"level save failed: {saveErr}");

            GD.Print($"baked {MeshLibraryPath} and {LevelPath}");
            root.Free();
        }
        catch (Exception e)
        {
            GD.PushError($"bake failed: {e}");
            exitCode = 1;
        }
        Quit(exitCode);
    }

    private static MeshLibrary BuildMeshLibrary()
    {
        var lib = new MeshLibrary();
        AddItem(lib, BlockItem.Floor, "floor", GameAssets.Block);
        AddItem(lib, BlockItem.Wall, "wall", Tinted(GameAssets.Block, new Color(0.22f, 0.24f, 0.30f)));
        AddItem(lib, BlockItem.Beacon, "beacon", Tinted(GameAssets.Block, new Color(0.10f, 0.45f, 0.55f), new Color(0.2f, 0.9f, 1f)));
        AddItem(lib, BlockItem.Hollow, "hollow", GameAssets.Hollow);
        return lib;
    }

    // MeshLibrary items carry no material override, so tinted variants get the
    // material set directly on a copy of the mesh's surface.
    private static Mesh Tinted(Mesh source, Color albedo, Color? emission = null)
    {
        if (source is not ArrayMesh array)
            throw new InvalidOperationException($"expected ArrayMesh, got {source.GetType().Name}");
        var mat = new StandardMaterial3D { AlbedoColor = albedo };
        if (emission.HasValue)
        {
            mat.EmissionEnabled = true;
            mat.Emission = emission.Value;
            mat.EmissionEnergyMultiplier = 2.5f;
        }
        var mesh = (ArrayMesh)array.Duplicate();
        mesh.SurfaceSetMaterial(0, mat);
        return mesh;
    }

    private static void AddItem(MeshLibrary lib, int id, string name, Mesh mesh)
    {
        lib.CreateItem(id);
        lib.SetItemName(id, name);
        lib.SetItemMesh(id, mesh);
        lib.SetItemShapes(id, new Godot.Collections.Array { GameAssets.CubeShape, Transform3D.Identity });
    }

    private static Node3D BuildLevelScene(MeshLibrary library)
    {
        var root = new LevelRunner { Name = "Level_1" };

        var env = new Environment
        {
            BackgroundMode = Environment.BGMode.Color,
            BackgroundColor = new Color(0.015f, 0.015f, 0.03f),
            AmbientLightSource = Environment.AmbientSource.Color,
            AmbientLightColor = new Color(0.5f, 0.55f, 0.65f),
            AmbientLightEnergy = 0.4f,
            TonemapMode = Environment.ToneMapper.Filmic,
            FogEnabled = true,
            FogLightColor = new Color(0.05f, 0.05f, 0.1f),
            FogDensity = 0.012f,
            GlowEnabled = true,
            GlowIntensity = 0.9f,
            GlowBloom = 0.1f,
        };
        root.AddChild(new WorldEnvironment { Name = "WorldEnvironment", Environment = env });

        root.AddChild(new DirectionalLight3D
        {
            Name = "Sun",
            RotationDegrees = new Vector3(-52f, -35f, 0f),
            LightEnergy = 1.2f,
            ShadowEnabled = true,
        });

        var map = new GridMap
        {
            Name = "GridMap",
            MeshLibrary = library,
            CellSize = Vector3.One,
            Position = new Vector3(-0.5f, 0f, -0.5f),
        };
        Paint(map);
        root.AddChild(map);

        AddDoorsAndButtons(root);
        AddJumpPads(root);

        var player = (Node3D)PlayerScene.Instantiate();
        player.Name = "Player";
        player.Position = new Vector3(5f, 1.6f, 0f);
        root.AddChild(player);

        // fixed angled top-down camera aimed at the arena center, not the ball;
        // Basis.LookingAt works detached from the tree, unlike Node3D.LookAt.
        // Height/distance chosen so the full 45-wide disc fits the 55° vfov.
        var camera = new Camera3D
        {
            Name = "Camera",
            Fov = 55f,
            Position = new Vector3(0f, 45f, 30f),
            Current = true,
        };
        Vector3 aim = -camera.Position;
        camera.Basis = Basis.LookingAt(aim.Normalized(), Vector3.Up);
        root.AddChild(camera);

        // PackedScene.Pack only serializes nodes owned by the packed root
        foreach (Node child in root.GetChildren())
            SetOwnerRecursive(child, root);
        return root;
    }

    // Sliding doors seal every gate of the outermost ring wall; floor buttons
    // placed deeper inside share one channel so any pressed button opens them.
    // Both come from their own scenes; only the transforms are set here.
    private static void AddDoorsAndButtons(Node3D root)
    {
        PackedScene doorScene = ResourceLoader.Load<PackedScene>("res://scenes/door.tscn");
        PackedScene buttonScene = ResourceLoader.Load<PackedScene>("res://scenes/floor_button.tscn");

        float ringRadius = Level.Rings[^1];
        foreach (float gateDeg in RingGates[2])
        {
            float a = Mathf.DegToRad(gateDeg);
            // local X must span the gate tangentially: rotY = a + 90°
            var door = doorScene.Instantiate<Door>();
            door.Name = $"Door_{gateDeg:0}";
            door.Channel = $"gate_{gateDeg:0}";
            door.Position = new Vector3(Mathf.Cos(a) * ringRadius, 2.2f, Mathf.Sin(a) * ringRadius);
            door.RotationDegrees = new Vector3(0f, gateDeg + 90f, 0f);
            root.AddChild(door);
        }

        // one button per gate, placed inside at the gate's own angle
        foreach (float gateDeg in RingGates[2])
        {
            float a = Mathf.DegToRad(gateDeg);
            var button = buttonScene.Instantiate<FloorButton>();
            button.Name = $"Button_{gateDeg:0}";
            button.Channel = $"gate_{gateDeg:0}";
            button.Position = new Vector3(Mathf.RoundToInt(Mathf.Cos(a) * 10f), 1f, Mathf.RoundToInt(Mathf.Sin(a) * 10f));
            root.AddChild(button);
        }
    }

    // Jump pads sit flush on the floor disc: root at the block top (y = 1),
    // the pad's cube base sunk half a cell into the floor block so only the
    // glowing disc shows.
    private static void AddJumpPads(Node3D root)
    {
        PackedScene padScene = ResourceLoader.Load<PackedScene>("res://scenes/jump_pad.tscn");

        foreach ((int x, int z) in new[] { (-6, 5), (7, -4), (-3, -9) })
        {
            var pad = padScene.Instantiate<JumpPad>();
            pad.Name = $"JumpPad_{x}_{z}";
            pad.Position = new Vector3(x, 1f, z);
            root.AddChild(pad);
        }
    }

    private static void SetOwnerRecursive(Node node, Node owner)
    {
        node.Owner = owner;
        foreach (Node child in node.GetChildren())
            SetOwnerRecursive(child, owner);
    }

    private static void Paint(GridMap map)
    {
        int range = (int)Mathf.Ceil(Level.FloorRadius);

        // floor: solid disc of blocks
        for (int i = -range; i <= range; i++)
            for (int j = -range; j <= range; j++)
            {
                if (Mathf.Sqrt(i * i + j * j) > Level.FloorRadius)
                    continue;
                map.SetCellItem(new Vector3I(i, 0, j), BlockItem.Floor);
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
                    map.SetCellItem(new Vector3I(bi, 1, bj), BlockItem.Beacon);
                    map.SetCellItem(new Vector3I(bi, 2, bj), BlockItem.Beacon);
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
                        map.SetCellItem(new Vector3I(i, 1 + y, j), BlockItem.Wall);
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
}
