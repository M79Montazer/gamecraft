using Godot;

public partial class CameraRig : Node3D
{
    public Node3D Target;
    [Export] public Vector3 Offset = new(0f, 13f, 9f);
    [Export] public float FollowSharpness = 8f;

    public override void _Ready()
    {
        var camera = new Camera3D { Position = Offset, Fov = 55f };
        AddChild(camera);
        camera.LookAt(Target.GlobalPosition, Vector3.Up);
        camera.Current = true;
    }

    public override void _Process(double delta)
    {
        if (Target == null)
            return;
        float t = 1f - Mathf.Exp(-FollowSharpness * (float)delta);
        GlobalPosition = GlobalPosition.Lerp(Target.GlobalPosition, t);
    }
}
