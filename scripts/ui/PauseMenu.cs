using Godot;
using System;

// The pause overlay, laid out in scenes/ui/pause_menu.tscn. The buttons are
// created here via MainMenu.MenuButton so they match every other menu. The
// LevelRunner opens this and owns the actual un-pause via the Resumed event.
public partial class PauseMenu : Control
{
    private Button _resume;
    private Button _fullscreen;

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

        _fullscreen = MainMenu.MenuButton("FULLSCREEN");
        _fullscreen.Pressed += ToggleFullscreen;
        buttons.AddChild(_fullscreen);
    }

    public void Open()
    {
        RefreshFullscreenLabel();
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

    private void ToggleFullscreen()
    {
        GetWindow().Mode = IsFullscreen()
            ? Window.ModeEnum.Windowed
            : Window.ModeEnum.Fullscreen;
        RefreshFullscreenLabel();
    }

    // the button names the mode a press would switch TO
    private void RefreshFullscreenLabel()
    {
        _fullscreen.Text = IsFullscreen() ? "WINDOWED" : "FULLSCREEN";
    }

    private bool IsFullscreen() => GetWindow().Mode == Window.ModeEnum.Fullscreen;
}
