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

    // best star count per completed level number
    private static readonly Dictionary<int, int> Stars = new();

    static Progress()
    {
        var cfg = new ConfigFile();
        if (cfg.Load(SavePath) != Error.Ok)
            return;
        if (cfg.HasSectionKey("levels", "unlocked"))
            Unlocked = System.Math.Max(1, cfg.GetValue("levels", "unlocked", 1).AsInt32());
        foreach (string key in cfg.GetSectionKeys("levels") ?? System.Array.Empty<string>())
            if (key.StartsWith("stars/"))
                Stars[int.Parse(key["stars/".Length..])] = cfg.GetValue("levels", key, 0).AsInt32();
    }

    public static int GetStars(int levelNumber) => Stars.GetValueOrDefault(levelNumber);

    public static void MarkCompleted(int levelNumber, int stars)
    {
        // replays only ever raise the best result, never lower it
        Stars[levelNumber] = System.Math.Max(GetStars(levelNumber), System.Math.Clamp(stars, 0, 3));
        Unlocked = System.Math.Max(Unlocked, levelNumber + 1);
        var cfg = new ConfigFile();
        cfg.SetValue("levels", "unlocked", Unlocked);
        foreach (var (number, best) in Stars)
            cfg.SetValue("levels", $"stars/{number}", best);
        cfg.Save(SavePath);
    }

    public static bool IsUnlocked(int levelNumber) => levelNumber <= Unlocked;

    // Player-facing level scenes under scenes/levels/*, sorted level_2 before
    // level_10. Level 0 is retained in the project but hidden from the final
    // level-select and progression UI.
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
                if ((file == $"{dir}.tscn" || file == $"{dir}.scn") && LevelNumber(dir) != 0)
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
