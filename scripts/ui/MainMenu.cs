using Godot;

// Main menu: title screen with Play (to the level selector) and Quit.
public partial class MainMenu : Control
{
    private const string LevelSelectScene = "res://scenes/ui/level_select.tscn";

    public override void _Ready()
    {
        var bg = new ColorRect { Color = new Color(0.015f, 0.015f, 0.03f) };
        bg.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        AddChild(bg);

        var center = new CenterContainer();
        center.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        AddChild(center);

        var box = new VBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
        box.AddThemeConstantOverride("separation", 14);
        center.AddChild(box);

        var title = new Label { Text = "H O L L O W", HorizontalAlignment = HorizontalAlignment.Center };
        title.LabelSettings = new LabelSettings
        {
            FontSize = 84,
            FontColor = new Color(0.88f, 0.94f, 1f),
            OutlineSize = 16,
            OutlineColor = new Color(0, 0, 0, 0.9f),
        };
        box.AddChild(title);

        var sub = new Label
        {
            Text = "outrun the circle that unmakes the world",
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        sub.LabelSettings = new LabelSettings
        {
            FontSize = 17,
            FontColor = new Color(0.45f, 0.7f, 0.8f, 0.8f),
        };
        box.AddChild(sub);

        var spacer = new Control { CustomMinimumSize = new Vector2(0f, 30f) };
        box.AddChild(spacer);

        var play = MenuButton("PLAY");
        play.Pressed += () => GetTree().ChangeSceneToFile(LevelSelectScene);
        box.AddChild(play);

        var quit = MenuButton("QUIT");
        quit.Pressed += () => GetTree().Quit();
        box.AddChild(quit);

        play.GrabFocus();
    }

    internal static Button MenuButton(string text)
    {
        var b = new Button
        {
            Text = text,
            CustomMinimumSize = new Vector2(260f, 52f),
            FocusMode = FocusModeEnum.All,
        };
        b.AddThemeFontSizeOverride("font_size", 22);

        var normal = new StyleBoxFlat
        {
            BgColor = new Color(0.06f, 0.08f, 0.14f, 0.9f),
            BorderColor = new Color(0.25f, 0.5f, 0.6f, 0.5f),
            ContentMarginLeft = 24f,
            ContentMarginRight = 24f,
            ContentMarginTop = 10f,
            ContentMarginBottom = 10f,
        };
        normal.SetBorderWidthAll(1);
        normal.SetCornerRadiusAll(6);

        var hover = (StyleBoxFlat)normal.Duplicate();
        hover.BgColor = new Color(0.1f, 0.16f, 0.24f, 0.95f);
        hover.BorderColor = new Color(0.35f, 0.8f, 0.95f, 0.9f);

        var pressed = (StyleBoxFlat)normal.Duplicate();
        pressed.BgColor = new Color(0.04f, 0.06f, 0.1f);

        var focus = (StyleBoxFlat)normal.Duplicate();
        focus.BorderColor = new Color(0.35f, 0.8f, 0.95f);
        focus.SetBorderWidthAll(2);
        focus.DrawCenter = false;

        b.AddThemeStyleboxOverride("normal", normal);
        b.AddThemeStyleboxOverride("hover", hover);
        b.AddThemeStyleboxOverride("pressed", pressed);
        b.AddThemeStyleboxOverride("focus", focus);
        return b;
    }
}
