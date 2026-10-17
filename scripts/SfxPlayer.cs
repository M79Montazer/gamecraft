using Godot;
using System.Collections.Generic;

// One-shot sound effect ids; each member maps 1:1 to an AudioStreamPlayer
// child of the same name in scenes/sfx_player.tscn.
public enum Sfx
{
    UiHover,
    UiClick,
    Jump,
    Door,
    FloorButton,
    SwitchButton,
    Win,
    Lose,
    StarPickup,
}

// Autoload singleton (scenes/sfx_player.tscn), the SFX counterpart to
// MusicPlayer: one-shot sounds that must outlive scene changes, so they live
// on the root instead of inside any scene. The streams are referenced by the
// scene itself, which is what carries them into exported release builds
// (C# GD.Load paths are not scanned by the exporter). All players sit on the
// "SFX" bus, so the settings/pause volume slider controls them.
public partial class SfxPlayer : Node
{
    public static SfxPlayer Instance { get; private set; }

    private readonly Dictionary<Sfx, AudioStreamPlayer> _players = new();

    public override void _EnterTree()
    {
        Instance = this;
    }

    public override void _Ready()
    {
        // the pause menu and the end cards pause the whole tree; UI sounds
        // must keep working through all of that
        ProcessMode = ProcessModeEnum.Always;

        foreach (Sfx sfx in System.Enum.GetValues<Sfx>())
            _players[sfx] = GetNode<AudioStreamPlayer>(sfx.ToString());
    }

    // Fire-and-forget one-shot; safe to call from anywhere, even before the
    // autoload is up (silently does nothing). Re-triggering a sound that is
    // still playing just restarts it, which suits these short clips.
    public static void Play(Sfx sfx)
    {
        Instance?._players[sfx].Play();
    }
}
