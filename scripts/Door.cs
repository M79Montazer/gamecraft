using Godot;

// Sliding gate door: a solid slab sealing a ring-wall gate until a floor
// button with the same Channel activates it, then it sinks into the floor.
// Created by BakeLevel as a standalone body next to the GridMap, so it
// survives the nothingness (which only hollows GridMap cells).
public partial class Door : StaticBody3D
{
    [Export] public string Channel = "";

    private const float SinkDepth = 2.6f; // slab height 2.4 + a little margin

    private bool _open;

    public override void _Ready()
    {
        AddToGroup("sliding_door");
    }

    public void Open()
    {
        if (_open)
            return;
        _open = true;

        var tween = CreateTween();
        tween.TweenProperty(this, "position", Position + new Vector3(0, -SinkDepth, 0), 1.4f)
            .SetTrans(Tween.TransitionType.Cubic)
            .SetEase(Tween.EaseType.InOut);
        // collision off only once the ball can't be standing on the slab top
        tween.TweenCallback(Callable.From(() => CollisionLayer = 0));
    }
}
