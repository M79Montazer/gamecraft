using Godot;

// Sliding gate door: a solid slab sealing a ring-wall gate until a floor
// button with the same Channel activates it, then it sinks into the floor.
// Created by BakeLevel as a standalone body next to the GridMap, so it
// survives the nothingness (which only hollows GridMap cells).
public partial class Door : StaticBody3D
{
    [Export] public string Channel = "";

    // Distance the Panel sinks when opening — tune in the inspector to match
    // the door.fbx scale once fixed in the editor.
    [Export] public float SinkDepth = 2.6f;

    // If true, the door starts the game already open (panel sunk, collision
    // off); switch/button toggling still works from that state.
    [Export] public bool StartOpen = false;

    private Node3D _panel;
    private bool _open;
    private uint _restCollisionLayer;
    private Vector3 _restPanelPosition;
    private Tween _tween;

    public override void _Ready()
    {
        AddToGroup("sliding_door");
        _panel = GetNode<Node3D>("Panel");
        _restCollisionLayer = CollisionLayer;
        _restPanelPosition = _panel.Position;

        if (StartOpen)
        {
            _open = true;
            _panel.Position = _restPanelPosition + new Vector3(0, -SinkDepth, 0);
            CollisionLayer = 0;
        }
    }

    public void Open()
    {
        if (_open)
            return;
        _open = true;

        _tween?.Kill();
        _tween = CreateTween();
        _tween.TweenProperty(_panel, "position", _restPanelPosition + new Vector3(0, -SinkDepth, 0), 1.4f)
            .SetTrans(Tween.TransitionType.Cubic)
            .SetEase(Tween.EaseType.InOut);
        // collision off only once the ball can't be standing on the slab top
        _tween.TweenCallback(Callable.From(() => CollisionLayer = 0));
    }

    // For switch levers: an open door closes back up, a closed one opens.
    public void Toggle()
    {
        if (_open)
        {
            _open = false;

            // collision back on immediately — the slab rises through anything on it
            CollisionLayer = _restCollisionLayer;
            _tween?.Kill();
            _tween = CreateTween();
            _tween.TweenProperty(_panel, "position", _restPanelPosition, 1.4f)
                .SetTrans(Tween.TransitionType.Cubic)
                .SetEase(Tween.EaseType.InOut);
        }
        else
        {
            Open();
        }
    }
}
