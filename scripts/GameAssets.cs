using Godot;

public static class GameAssets
{
    public static Mesh Ball { get; private set; }
    public static Mesh Block { get; private set; }
    public static Mesh Hollow { get; private set; }

    private static BoxShape3D _cubeShape;
    public static BoxShape3D CubeShape => _cubeShape ??= new BoxShape3D { Size = Vector3.One };

    public static void Load()
    {
        Ball = ExtractMesh("res://assets/ball.glb");
        Block = ExtractMesh("res://assets/block.glb");
        Hollow = ExtractMesh("res://assets/block_hollow.glb");
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
