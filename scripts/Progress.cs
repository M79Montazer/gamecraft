using Godot;
using System.Collections.Generic;

// Persistent level progress, stored in user://progress.cfg. Levels are numbered
// by the first number in their folder name (level_1 -> 1); a level stays locked
// until every earlier level is completed.
public static class Progress
{
    private const string SavePath = "user://progress.cfg";
    private const string LevelsDir = "res://scenes/levels";

    public static int Unlocked { get; private set; } = 1;

    static Progress()
    {
        var cfg = new ConfigFile();
        if (cfg.Load(SavePath) == Error.Ok && cfg.HasSectionKey("levels", "unlocked"))
            Unlocked = System.Math.Max(1, cfg.GetValue("levels", "unlocked", 1).AsInt32());
    }

    public static void MarkCompleted(int levelNumber)
    {
        if (levelNumber < Unlocked)
            return;
        Unlocked = levelNumber + 1;
        var cfg = new ConfigFile();
        cfg.SetValue("levels", "unlocked", Unlocked);
        cfg.Save(SavePath);
    }

    public static bool IsUnlocked(int levelNumber) => levelNumber <= Unlocked;

    // All level scenes under scenes/levels/*, sorted level_2 before level_10.
    public static List<string> FindLevelScenes()
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
                if (sub.CurrentIsDir())
                    continue;
                // Exported builds remap resources inside the PCK, so DirAccess
                // reports e.g. "level_1.tscn.remap" — strip the suffix before
                // comparing. Binary exports may report ".scn".
                if (file.EndsWith(".remap"))
                    file = file[..^".remap".Length];
                // Only the folder's own <folder>.tscn is a level; other scenes
                // (e.g. intro_dialogue.tscn) are support files.
                if (file == $"{dir}.tscn" || file == $"{dir}.scn")
                    scenes.Add($"{dir}/{file}");
            }
            sub.ListDirEnd();
        }
        root.ListDirEnd();

        scenes.Sort((a, b) => LevelNumber(a).CompareTo(LevelNumber(b)));
        return scenes;
    }

    public static int LevelNumber(string path)
    {
        var match = System.Text.RegularExpressions.Regex.Match(path, @"(\d+)");
        return match.Success ? int.Parse(match.Value) : int.MaxValue;
    }
}
