using Godot;

// Autoload singleton (project.godot): user settings that must survive scene
// changes and app restarts, stored in user://settings.cfg with the same
// ConfigFile pattern as Progress. The saved state is applied here on startup,
// so it holds in exported release builds too — nothing depends on running
// from the editor.
public partial class AppSettings : Node
{
    private const string SavePath = "user://settings.cfg";
    private const string MusicBus = "Music";
    private const string SfxBus = "SFX";

    // linear 0..1 loudness per bus; 1 leaves the bus unchanged
    public static float MusicVolume { get; private set; } = 1f;
    public static float SfxVolume { get; private set; } = 1f;

    private static bool _fullscreen;

    public override void _Ready()
    {
        // the pause menu pauses the whole tree; settings must still respond
        ProcessMode = ProcessModeEnum.Always;

        var cfg = new ConfigFile();
        if (cfg.Load(SavePath) == Error.Ok)
        {
            MusicVolume = Mathf.Clamp(cfg.GetValue("audio", "music", 1f).AsSingle(), 0f, 1f);
            SfxVolume = Mathf.Clamp(cfg.GetValue("audio", "sfx", 1f).AsSingle(), 0f, 1f);
            _fullscreen = cfg.GetValue("display", "fullscreen", false).AsBool();
        }

        ApplyVolume(MusicBus, MusicVolume);
        ApplyVolume(SfxBus, SfxVolume);

        // one frame of delay: setting the window mode before the first frame
        // is drawn can be ignored on some platforms
        if (_fullscreen)
            CallDeferred(MethodName.ApplySavedWindowMode);
    }

    public static void SetMusicVolume(float linear)
    {
        MusicVolume = Mathf.Clamp(linear, 0f, 1f);
        ApplyVolume(MusicBus, MusicVolume);
        Save();
    }

    public static void SetSfxVolume(float linear)
    {
        SfxVolume = Mathf.Clamp(linear, 0f, 1f);
        ApplyVolume(SfxBus, SfxVolume);
        Save();
    }

    public static bool IsFullscreen() =>
        DisplayServer.WindowGetMode() is DisplayServer.WindowMode.Fullscreen
            or DisplayServer.WindowMode.ExclusiveFullscreen;

    public static void ToggleFullscreen() => SetFullscreen(!IsFullscreen());

    public static void SetFullscreen(bool fullscreen)
    {
        _fullscreen = fullscreen;
        DisplayServer.WindowSetMode(fullscreen
            ? DisplayServer.WindowMode.ExclusiveFullscreen
            : DisplayServer.WindowMode.Windowed);
        Save();
    }

    private void ApplySavedWindowMode()
    {
        if (_fullscreen && !IsFullscreen())
            DisplayServer.WindowSetMode(DisplayServer.WindowMode.ExclusiveFullscreen);
    }

    private static void ApplyVolume(string bus, float linear)
    {
        int index = AudioServer.GetBusIndex(bus);
        if (index < 0)
            return;
        // square for a perceptual curve — linear dB makes low slider values
        // barely quieter; LinearToDb(0) is -inf, so mute instead
        AudioServer.SetBusVolumeDb(index, linear > 0f ? Mathf.LinearToDb(linear * linear) : -80f);
    }

    private static void Save()
    {
        var cfg = new ConfigFile();
        cfg.SetValue("audio", "music", MusicVolume);
        cfg.SetValue("audio", "sfx", SfxVolume);
        cfg.SetValue("display", "fullscreen", _fullscreen);
        cfg.Save(SavePath);
    }
}
