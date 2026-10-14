using Godot;
using System.Collections.Generic;

// Level selector: scans scenes/levels/*/ for level scenes at runtime and shows
// one button per level. All levels are open for now; the unlock/progress
// filter goes in FindLevelScenes once a save system exists.
public partial class LevelSelect : Control
{
    private const string LevelsDir = "res://scenes/levels";
    private const string MenuScene = "res://scenes/main.tscn";

    public override void _Ready()
    {
        var bg = new ColorRect { Color = new Color(0.015f, 0.015f, 0.03f) };
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

        List<string> scenes = FindLevelScenes();
        Button first = null;
        foreach (string path in scenes)
        {
            var b = MainMenu.MenuButton(Prettify(path));
            string scenePath = $"{LevelsDir}/{path}";
            b.Pressed += () => GetTree().ChangeSceneToFile(scenePath);
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

    private static List<string> FindLevelScenes()
    {
        var scenes = new List<string>();
        using var root = DirAccess.Open(LevelsDir);
        if (root == null)
            return scenes;

        root.ListDirBegin();
        string dir;
        while ((dir = root.GetNext()) != "")
        {
            if (!root.CurrentIsDir() || dir.StartsWith("."))
                continue;
            using var sub = DirAccess.Open($"{LevelsDir}/{dir}");
            if (sub == null)
                continue;
            sub.ListDirBegin();
            string file;
            while ((file = sub.GetNext()) != "")
            {
                if (!sub.CurrentIsDir() && file.EndsWith(".tscn"))
                    scenes.Add($"{dir}/{file}");
            }
            sub.ListDirEnd();
        }
        root.ListDirEnd();

        // level_2 before level_10
        scenes.Sort((a, b) => LevelNumber(a).CompareTo(LevelNumber(b)));
        return scenes;
    }

    private static int LevelNumber(string path)
    {
        var match = System.Text.RegularExpressions.Regex.Match(path, @"(\d+)");
        return match.Success ? int.Parse(match.Value) : int.MaxValue;
    }

    private static string Prettify(string path)
    {
        string name = path.Contains('/') ? path[..path.IndexOf('/')] : path;
        return name.Replace('_', ' ').ToUpperInvariant();
    }
}
