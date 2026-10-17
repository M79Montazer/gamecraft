using Godot;

// Autoload singleton (scenes/music_player.tscn): background music that must
// outlive scene changes, so it lives on the root instead of inside any scene.
// Track 0 is the menu theme; 1-8 are the level themes, matching the OST
// naming. The streams are referenced by the scene itself, which is what
// carries them into exported release builds (C# GD.Load paths are not scanned
// by the exporter).
public partial class MusicPlayer : Node
{
    public static MusicPlayer Instance { get; private set; }

    // Index 0 = menu, 1-8 = level number; order is defined in the scene.
    [Export] public AudioStream[] Tracks { get; set; }

    private AudioStreamPlayer _player;
    private int _current = -1;

    public override void _EnterTree()
    {
        Instance = this;
    }

    public override void _Ready()
    {
        // the pause menu, intro dialogue and score cards pause the whole tree;
        // music must keep running through all of that
        ProcessMode = ProcessModeEnum.Always;

        _player = GetNode<AudioStreamPlayer>("Player");
    }

    // Starts the given track on loop; a request for the track already playing
    // is a no-op, so menu-to-level-select and R restarts don't restart it.
    public void Play(int trackIndex)
    {
        if (trackIndex < 0 || trackIndex >= Tracks.Length)
        {
            GD.PushWarning($"MusicPlayer: no track {trackIndex} (0..{Tracks.Length - 1})");
            return;
        }
        if (trackIndex == _current)
            return;

        // imported OGG streams default to no loop; force it per stream
        if (Tracks[trackIndex] is AudioStreamOggVorbis ogg)
            ogg.Loop = true;

        _current = trackIndex;
        _player.Stream = Tracks[trackIndex];
        _player.Play();
    }

    // Dialogue ducking is applied on the player rather than the Music bus, so
    // it stacks cleanly with the user's saved music-volume setting.
    public void SetDialogueDuck(bool active, float duckVolumeDb = -12.0f)
    {
        if (_player is null)
            return;

        _player.VolumeDb = active ? Mathf.Min(duckVolumeDb, 0.0f) : 0.0f;
    }
}
