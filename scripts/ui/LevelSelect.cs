using Godot;

// Level selector: scans scenes/levels/*/ for level scenes at runtime and shows
// one card per level. Card spec from assets/UI_html/Level_select..html.
// Levels past Progress.Unlocked use the glass variant and are unclickable.
public partial class LevelSelect : Control
{
    private const string MenuScene = "res://scenes/main.tscn";

    // Designer spec colors.
    private static readonly Color Accent = new("87d1ef");
    private static readonly Color CardBg = new("00131a");
    private static readonly Color GlassFg = new("727272");

    private static readonly Font Sarpanch = GD.Load<Font>("res://assets/fonts/Sarpanch-SemiBold.ttf");

    public override void _Ready()
    {
        // arriving here from a paused end card must not stay paused
        GetTree().Paused = false;
        Input.MouseMode = Input.MouseModeEnum.Visible;

        var bg = new TextureRect
        {
            Texture = GD.Load<Texture2D>("res://assets/UI_asset/background/LEVEL SELECT.png"),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCovered,
        };
        bg.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        AddChild(bg);

        var center = new CenterContainer();
        center.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        AddChild(center);

        var box = new VBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
        box.AddThemeConstantOverride("separation", 18);
        center.AddChild(box);

        var title = new Label { Text = "SELECT LEVEL", HorizontalAlignment = HorizontalAlignment.Center };
        title.LabelSettings = new LabelSettings
        {
            FontSize = 40,
            FontColor = new Color(0.88f, 0.94f, 1f),
            OutlineSize = 10,
            OutlineColor = new Color(0, 0, 0, 0.9f),
        };
        box.AddChild(title);

        var spacer = new Control { CustomMinimumSize = new Vector2(0f, 12f) };
        box.AddChild(spacer);

        var cards = new GridContainer { Columns = 3 };
        cards.AddThemeConstantOverride("h_separation", 26);
        cards.AddThemeConstantOverride("v_separation", 16);
        box.AddChild(cards);

        Button first = null;
        foreach (string path in Progress.FindLevelScenes())
        {
            int number = Progress.LevelNumber(path);
            bool unlocked = Progress.IsUnlocked(number);
            var card = LevelCard(number, Progress.GetStars(number), unlocked);
            if (unlocked)
            {
                string scenePath = $"res://scenes/levels/{path}";
                card.Pressed += () => GetTree().ChangeSceneToFile(scenePath);
                first ??= card;
            }
            cards.AddChild(card);
        }

        var backSpacer = new Control { CustomMinimumSize = new Vector2(0f, 8f) };
        box.AddChild(backSpacer);

        var back = MainMenu.MenuButton("BACK");
        back.Pressed += () => GetTree().ChangeSceneToFile(MenuScene);
        box.AddChild(back);

        (first ?? back).GrabFocus();
    }

    public override void _UnhandledInput(InputEvent e)
    {
        if (e.IsActionPressed("ui_cancel"))
            GetTree().ChangeSceneToFile(MenuScene);
    }

    // One 261x214 level card button (the 402x330 HTML mock at ~65% scale):
    // star cluster on top, two-digit number, chevrons, level title. Locked
    // cards get the glass variant (grey, translucent) and cannot be clicked.
    private static Button LevelCard(int number, int stars, bool unlocked)
    {
        var card = new Button
        {
            CustomMinimumSize = new Vector2(261f, 214f),
            Disabled = !unlocked,
            FocusMode = unlocked ? FocusModeEnum.All : FocusModeEnum.None,
        };

        Color fg = unlocked ? Accent : GlassFg;
        var normal = new StyleBoxFlat
        {
            BgColor = CardBg,
            BorderColor = Accent,
        };
        normal.SetBorderWidthAll(1);
        normal.SetCornerRadiusAll(20);

        // "clicked" variant glow approximating box-shadow 0 4px 70px accent;
        // shown on mouse hover only
        var hover = (StyleBoxFlat)normal.Duplicate();
        hover.ShadowColor = new Color(Accent, 0.7f);
        hover.ShadowSize = 22;

        var pressed = (StyleBoxFlat)normal.Duplicate();
        pressed.ShadowColor = new Color(Accent, 0.4f);
        pressed.ShadowSize = 10;

        // glass variant for locked cards: translucent grey, faint accent edge
        var glass = new StyleBoxFlat
        {
            BgColor = new Color(0.682f, 0.682f, 0.698f, 0.3f),
            BorderColor = new Color(Accent, 0.3f),
        };
        glass.SetBorderWidthAll(1);
        glass.SetCornerRadiusAll(20);

        card.AddThemeStyleboxOverride("normal", normal);
        card.AddThemeStyleboxOverride("hover", (StyleBoxFlat)hover.Duplicate());
        card.AddThemeStyleboxOverride("pressed", pressed);
        card.AddThemeStyleboxOverride("focus", (StyleBoxFlat)normal.Duplicate());
        card.AddThemeStyleboxOverride("disabled", glass);

        AddLabel(card, number.ToString("00"), 23, fg, 52.5f, 36.5f, 188.5f);
        AddLabel(card, $"Level {number}", 31, fg, 143.5f, 13.5f, 234f);

        // mock coordinates at ~65% scale; Scale shrinks the drawn shapes
        card.AddChild(new StarCluster
        {
            Filled = unlocked ? stars : 0,
            Color = fg,
            Position = new Vector2(70f, 8.75f),
            Scale = new Vector2(0.65f, 0.65f),
        });
        card.AddChild(new ChevronCluster
        {
            Color = fg,
            Position = new Vector2(110f, 102f),
            Scale = new Vector2(0.65f, 0.65f),
        });
        return card;
    }

    private static void AddLabel(Control parent, string text, int size, Color color, float top, float left, float width)
    {
        var label = new Label
        {
            Text = text,
            Position = new Vector2(left, top),
            Size = new Vector2(width, size * 1.3f),
            HorizontalAlignment = HorizontalAlignment.Center,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        label.LabelSettings = new LabelSettings { Font = Sarpanch, FontSize = size, FontColor = color };
        parent.AddChild(label);
    }
}

// The three-star cluster from the HTML mock: star at x 0/57/114, the
// middle one raised. Earned stars are filled, the rest are outlines.
public sealed partial class StarCluster : Control
{
    // SVG path points from #star (64x58 units).
    private static readonly Vector2[] Shape =
    {
        new(32f, 0f), new(39.185f, 22.111f), new(62.434f, 22.111f),
        new(43.625f, 35.777f), new(50.809f, 57.889f), new(32f, 44.223f),
        new(13.191f, 57.889f), new(20.375f, 35.777f), new(1.566f, 22.111f),
        new(24.815f, 22.111f),
    };

    public int Filled { get; set; }
    public Color Color { get; set; } = Colors.White;

    public StarCluster()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        CustomMinimumSize = new Vector2(186f, 75f);
    }

    public override void _Draw()
    {
        for (int i = 0; i < 3; i++)
        {
            var offset = new Vector2(i * 57f, i == 1 ? 0f : 11f);
            var shifted = new Vector2[Shape.Length];
            for (int v = 0; v < Shape.Length; v++)
                shifted[v] = Shape[v] + offset;
            if (i < Filled)
                DrawColoredPolygon(shifted, Color);
            else
                DrawPolyline(Closed(shifted), Color, 1.5f);
        }
    }

    private static Vector2[] Closed(Vector2[] points)
    {
        var loop = new Vector2[points.Length + 1];
        points.CopyTo(loop, 0);
        loop[^1] = points[0];
        return loop;
    }
}

// Two upward chevrons from the HTML mock (viewBox 64x64, stroke 2).
public sealed partial class ChevronCluster : Control
{
    private static readonly Vector2[][] Chevrons =
    {
        new[] { new Vector2(19f, 29f), new Vector2(32.5f, 16f), new Vector2(46f, 29f) },
        new[] { new Vector2(19f, 48f), new Vector2(32.5f, 35f), new Vector2(46f, 48f) },
    };

    public Color Color { get; set; } = Colors.White;

    public ChevronCluster()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        CustomMinimumSize = new Vector2(64f, 64f);
    }

    public override void _Draw()
    {
        foreach (Vector2[] chevron in Chevrons)
            DrawPolyline(chevron, Color, 2f);
    }
}
