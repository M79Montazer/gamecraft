using Godot;

// Per-level game controller: the root script of a level scene, expected to sit
// alongside its GridMap, Player and Camera nodes.
public partial class LevelRunner : Node3D
{
    private enum GameState { Playing, Won, Lost }

    private const string LevelSelectScene = "res://scenes/ui/level_select.tscn";
    private const string NothingnessScene = "res://scenes/nothingness.tscn";
    private const string HudScene = "res://scenes/ui/hud.tscn";

    // Distance from the origin past which the player counts as escaped.
    [Export] public float FloorRadius { get; set; } = 22.5f;
    // Distances marking the HUD zone boundaries, inner to outer.
    [Export] public float[] Rings { get; set; } = { 8f, 14f, 20f };

    private Player _player;
    private Level _level;
    private Nothingness _nothingness;
    private Hud _hud;
    private GameState _state = GameState.Playing;
    private float _time;

    public override void _Ready()
    {
        EnsureAction("move_left", Key.A);
        EnsureAction("move_right", Key.D);
        EnsureAction("move_forward", Key.W);
        EnsureAction("move_back", Key.S);
        EnsureAction("jump", Key.Space);

        _player = GetNode<Player>("Player");
        _level = new Level(GetNode<GridMap>("GridMap"), FloorRadius, Rings);

        _nothingness = GD.Load<PackedScene>(NothingnessScene).Instantiate<Nothingness>();
        _nothingness.Level = _level;
        AddChild(_nothingness);

        _hud = GD.Load<PackedScene>(HudScene).Instantiate<Hud>();
        AddChild(_hud);
    }

    public override void _Process(double delta)
    {
        if (_state != GameState.Playing)
            return;
        _time += (float)delta;

        Vector3 p = _player.GlobalPosition;
        float dist = Mathf.Sqrt(p.X * p.X + p.Z * p.Z);

        if (p.Y < -4f)
        {
            SetEnd("HOLLOWED", "the void consumed the world");
            return;
        }
        if (dist > _level.FloorRadius)
        {
            SetEnd("YOU ESCAPED", "the robot outran the hollow");
            return;
        }

        string zone = dist > _level.Rings[2] ? "OUTER RING"
            : dist > _level.Rings[1] ? "MIDDLE RING"
            : dist > _level.Rings[0] ? "INNER RING"
            : "THE CENTER";
        float toGo = Mathf.Max(0f, _level.FloorRadius - dist);
        _hud.SetStatus($"{zone}    escape in {toGo:0.#} m    {_time:0.0} s");
    }

    public override void _UnhandledInput(InputEvent e)
    {
        if (e is InputEventKey { Pressed: true } key && (key.PhysicalKeycode == Key.R || key.Keycode == Key.R))
            GetTree().ReloadCurrentScene();
        else if (e.IsActionPressed("ui_cancel"))
            GetTree().ChangeSceneToFile(LevelSelectScene);
    }

    private static void EnsureAction(string action, Key key)
    {
        if (InputMap.HasAction(action))
            return;
        InputMap.AddAction(action);
        InputMap.ActionAddEvent(action, new InputEventKey { PhysicalKeycode = key });
    }

    private void SetEnd(string title, string sub)
    {
        _state = title == "YOU ESCAPED" ? GameState.Won : GameState.Lost;
        _nothingness.Active = false;
        _hud.ShowEnd($"{title}\n{sub}\n\npress R to retry · Esc for levels");
    }
}
