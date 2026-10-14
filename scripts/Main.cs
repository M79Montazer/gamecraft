using Godot;

public partial class Main : Node3D
{
    private enum GameState { Playing, Won, Lost }

    private Player _player;
    private Nothingness _nothingness;
    private Label _hud;
    private Label _center;
    private GameState _state = GameState.Playing;
    private float _time;

    public override void _Ready()
    {
        EnsureAction("move_left", Key.A);
        EnsureAction("move_right", Key.D);
        EnsureAction("move_forward", Key.W);
        EnsureAction("move_back", Key.S);
        EnsureAction("jump", Key.Space);

        GameAssets.Load();
        SetupWorld();

        var level = new Level(GetNode<Node3D>("Level"));

        _nothingness = new Nothingness { Level = level };
        AddChild(_nothingness);

        _player = new Player { Position = new Vector3(5f, 1.6f, 0f) };
        AddChild(_player);

        AddChild(new CameraRig { Target = _player });

        SetupHud();
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
        if (dist > Level.FloorRadius)
        {
            SetEnd("YOU ESCAPED", "the ball outran the hollow");
            return;
        }

        string zone = dist > Level.Rings[2] ? "OUTER RING"
            : dist > Level.Rings[1] ? "MIDDLE RING"
            : dist > Level.Rings[0] ? "INNER RING"
            : "THE CENTER";
        float toGo = Mathf.Max(0f, Level.FloorRadius - dist);
        _hud.Text = $"{zone}    escape in {toGo:0.#} m    {_time:0.0} s";
    }

    public override void _UnhandledInput(InputEvent e)
    {
        if (e is InputEventKey { Pressed: true } key && (key.PhysicalKeycode == Key.R || key.Keycode == Key.R))
            GetTree().ReloadCurrentScene();
    }

    private static void EnsureAction(string action, Key key)
    {
        if (InputMap.HasAction(action))
            return;
        InputMap.AddAction(action);
        InputMap.ActionAddEvent(action, new InputEventKey { PhysicalKeycode = key });
    }

    private void SetupWorld()
    {
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
        AddChild(new WorldEnvironment { Environment = env });

        var sun = new DirectionalLight3D
        {
            RotationDegrees = new Vector3(-52f, -35f, 0f),
            LightEnergy = 1.2f,
            ShadowEnabled = true,
        };
        AddChild(sun);
    }

    private void SetupHud()
    {
        var layer = new CanvasLayer();
        AddChild(layer);

        _hud = new Label { Position = new Vector2(16f, 12f) };
        _hud.LabelSettings = new LabelSettings
        {
            FontSize = 24,
            FontColor = Colors.White,
            OutlineSize = 8,
            OutlineColor = new Color(0, 0, 0, 0.85f),
        };
        layer.AddChild(_hud);

        _center = new Label
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Visible = false,
        };
        _center.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        _center.LabelSettings = new LabelSettings
        {
            FontSize = 56,
            FontColor = Colors.White,
            OutlineSize = 12,
            OutlineColor = new Color(0, 0, 0, 0.9f),
        };
        layer.AddChild(_center);

        var hint = new Label
        {
            Text = "WASD move   ·   Space jump   ·   R restart",
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        hint.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.BottomWide);
        hint.OffsetTop = -44f;
        hint.LabelSettings = new LabelSettings
        {
            FontSize = 16,
            FontColor = new Color(1, 1, 1, 0.65f),
            OutlineSize = 6,
            OutlineColor = new Color(0, 0, 0, 0.8f),
        };
        layer.AddChild(hint);
    }

    private void SetEnd(string title, string sub)
    {
        _state = title == "YOU ESCAPED" ? GameState.Won : GameState.Lost;
        _nothingness.Active = false;
        _center.Text = $"{title}\n{sub}\n\npress R to restart";
        _center.Visible = true;
    }
}
