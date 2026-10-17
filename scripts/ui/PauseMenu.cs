using Godot;
using System;

// The pause overlay, laid out in scenes/ui/pause_menu.tscn. The buttons and
// volume sliders are created here via MainMenu helpers so they match every
// other menu. The LevelRunner opens this and owns the actual un-pause via the
// Resumed event.
public partial class PauseMenu : Control
{
    private const string MenuScene = "res://scenes/main.tscn";

    private Button _resume;

    // Raised when the player resumes (RESUME button or Esc); LevelRunner
    // unpauses the tree and restores the mouse mode for the active camera.
    public event Action Resumed;

    public override void _Ready()
    {
        var buttons = GetNode<VBoxContainer>("Center/Panel/Box/Buttons");

        _resume = MainMenu.MenuButton("RESUME");
        _resume.Pressed += Resume;
        buttons.AddChild(_resume);

        var restart = MainMenu.MenuButton("RESTART");
        restart.Pressed += () => GetTree().ReloadCurrentScene();
        buttons.AddChild(restart);

        buttons.AddChild(VolumeRow("MUSIC", AppSettings.SetMusicVolume, AppSettings.MusicVolume));
        buttons.AddChild(VolumeRow("SFX", AppSettings.SetSfxVolume, AppSettings.SfxVolume));

        var mainMenu = MainMenu.MenuButton("MAIN MENU");
        mainMenu.Pressed += GoToMainMenu;
        buttons.AddChild(mainMenu);
    }

    private static HBoxContainer VolumeRow(string label, Action<float> apply, float value)
    {
        var slider = MainMenu.MenuSlider(value);
        slider.ValueChanged += v => apply((float)v / 100f);
        return MainMenu.SliderRow(label, slider);
    }

    public void Open()
    {
        Visible = true;
        _resume.GrabFocus();
    }

    public void Resume()
    {
        Visible = false;
        Resumed?.Invoke();
    }

    // Esc while open resumes. The echo guard keeps a held key from
    // auto-repeating, and the event is consumed so LevelRunner's own
    // ui_cancel branch does not also fire on the same press.
    public override void _UnhandledInput(InputEvent e)
    {
        if (!Visible || e.IsEcho() || !e.IsActionPressed("ui_cancel"))
            return;

        GetViewport().SetInputAsHandled();
        Resume();
    }

    // Unlike the level select page, the main menu does not un-pause itself on
    // entry, so leaving a paused level has to clear the pause (and free the
    // mouse) here before the scene switch.
    private void GoToMainMenu()
    {
        GetTree().Paused = false;
        Input.MouseMode = Input.MouseModeEnum.Visible;
        GetTree().ChangeSceneToFile(MenuScene);
    }
}
