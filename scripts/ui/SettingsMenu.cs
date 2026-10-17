using Godot;

// Settings page opened from the main menu, laid out in scenes/ui/settings.tscn.
// Content is created here via the shared MainMenu helpers so it matches every
// other menu. Values live in the AppSettings autoload and persist across runs.
public partial class SettingsMenu : Control
{
    private const string MenuScene = "res://scenes/main.tscn";

    private Button _fullscreen;

    public override void _Ready()
    {
        var content = GetNode<VBoxContainer>("Center/Panel/Box/Content");

        var music = MainMenu.MenuSlider(AppSettings.MusicVolume);
        music.ValueChanged += v => AppSettings.SetMusicVolume((float)v / 100f);
        content.AddChild(MainMenu.SliderRow("MUSIC", music));

        var sfx = MainMenu.MenuSlider(AppSettings.SfxVolume);
        sfx.ValueChanged += v => AppSettings.SetSfxVolume((float)v / 100f);
        content.AddChild(MainMenu.SliderRow("SFX", sfx));

        // the button names the mode a press would switch to
        _fullscreen = MainMenu.MenuButton("FULLSCREEN");
        _fullscreen.Pressed += () =>
        {
            AppSettings.ToggleFullscreen();
            RefreshFullscreenLabel();
        };
        content.AddChild(_fullscreen);
        RefreshFullscreenLabel();

        var back = MainMenu.MenuButton("BACK");
        back.Pressed += Back;
        content.AddChild(back);

        music.GrabFocus();
    }

    // Esc goes back to the main menu, mirroring the level select page.
    public override void _UnhandledInput(InputEvent e)
    {
        if (e.IsEcho() || !e.IsActionPressed("ui_cancel"))
            return;

        GetViewport().SetInputAsHandled();
        Back();
    }

    private void RefreshFullscreenLabel()
    {
        _fullscreen.Text = AppSettings.IsFullscreen() ? "WINDOWED" : "FULLSCREEN";
    }

    private void Back() => GetTree().ChangeSceneToFile(MenuScene);
}
