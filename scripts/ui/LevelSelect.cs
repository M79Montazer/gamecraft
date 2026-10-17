using Godot;
using System.Collections.Generic;

// Level selector: scans scenes/levels/*/ for level scenes at runtime and shows
// one button per level. Levels past Progress.Unlocked show locked (disabled).
public partial class LevelSelect : Control
{
    private const string MenuScene = "res://scenes/main.tscn";

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
        box.AddThemeConstantOverride("separation", 12);
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

        var spacer = new Control { CustomMinimumSize = new Vector2(0f, 18f) };
        box.AddChild(spacer);

        List<string> scenes = Progress.FindLevelScenes();
        Button first = null;
        foreach (string path in scenes)
        {
            int number = Progress.LevelNumber(path);
            bool unlocked = Progress.IsUnlocked(number);
            var b = MainMenu.MenuButton(unlocked ? Prettify(path) : $"{Prettify(path)} 🔒");
            b.Disabled = !unlocked;
            if (unlocked)
            {
                string scenePath = $"res://scenes/levels/{path}";
                b.Pressed += () => GetTree().ChangeSceneToFile(scenePath);
            }
            box.AddChild(b);
            first ??= b;
        }

        if (first == null)
        {
            var none = new Label
            {
                Text = "no levels found",
                HorizontalAlignment = HorizontalAlignment.Center,
            };
            none.LabelSettings = new LabelSettings { FontSize = 20, FontColor = new Color(1, 1, 1, 0.5f) };
            box.AddChild(none);
        }

        var backSpacer = new Control { CustomMinimumSize = new Vector2(0f, 14f) };
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

    private static string Prettify(string path)
    {
        string name = path.Contains('/') ? path[..path.IndexOf('/')] : path;
        return name.Replace('_', ' ').ToUpperInvariant();
    }
}
