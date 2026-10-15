using Godot;

// Collectible star: a spinning golden star the player grabs by rolling into it.
// The model is assets/star.glb (authored in art_src/star.blend), instanced in
// scenes/star.tscn under "Pivot"; this script only spins/bobs the pivot and
// raises the Collected signal. Levels count pickups via the "star" group.
public partial class Star : Area3D
{
    [Signal] public delegate void CollectedEventHandler(Star star);

    private Node3D _pivot;
    private float _time;
    private bool _taken;

    public override void _Ready()
    {
        AddToGroup("star");
        _pivot = GetNode<Node3D>("Pivot");
        BodyEntered += OnBodyEntered;
    }

    public override void _Process(double delta)
    {
        if (_taken)
            return;
        _time += (float)delta;
        _pivot.RotateY((float)delta * 2.2f);
        _pivot.Position = Vector3.Up * (0.15f * Mathf.Sin(_time * 2.5f));
    }

    private void OnBodyEntered(Node3D body)
    {
        if (_taken || body is not Player)
            return;
        _taken = true;
        EmitSignal(SignalName.Collected, this);
        // tiny scale-out pop so the pickup reads as taken, then free
        var tween = CreateTween();
        tween.TweenProperty(_pivot, "scale", Vector3.One * 0.01f, 0.2f);
        tween.TweenCallback(Callable.From(QueueFree));
    }
}
