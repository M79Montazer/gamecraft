using Godot;

public static class GameAssets
{
    private static bool _loaded;

    public static Mesh Ball { get; private set; }
    public static Mesh Block { get; private set; }
    public static Mesh Hollow { get; private set; }

    private static BoxShape3D _cubeShape;
    public static BoxShape3D CubeShape => _cubeShape ??= new BoxShape3D { Size = Vector3.One };

    // Nodes ready in arbitrary order (Player before its level root), so every
    // accessor guarantees the glbs are loaded rather than relying on an
    // explicit Load() call from the scene root.
    public static void EnsureLoaded()
    {
        if (_loaded)
            return;
        Ball = ExtractMesh("res://assets/ball.glb");
        Block = ExtractMesh("res://assets/block.glb");
        Hollow = ExtractMesh("res://assets/block_hollow.glb");
        _loaded = true;
    }

    private static Mesh ExtractMesh(string path)
    {
        Node root = GD.Load<PackedScene>(path).Instantiate();
        Mesh mesh = FindMesh(root);
        root.Free();
        return mesh;
    }

    private static Mesh FindMesh(Node node)
    {
        if (node is MeshInstance3D mi && mi.Mesh != null)
            return mi.Mesh;
        foreach (Node child in node.GetChildren())
        {
            Mesh m = FindMesh(child);
            if (m != null)
                return m;
        }
        return null;
    }
}
