using Godot;

// Playback bridge for procedural speech. The AudioStreamPlayer is an authored
// child in the scene, so this class creates no engine nodes at runtime.
[GlobalClass]
public partial class RobotSpeechPlayer : Node
{
    [Signal]
    public delegate void LineStartedEventHandler(string text, double duration);

    [Signal]
    public delegate void LineProgressEventHandler(int visibleCharacters, double playbackSeconds);

    [Signal]
    public delegate void LineFinishedEventHandler();

    [Export] public RobotVoiceProfile VoiceProfile { get; set; }
    [Export] public StringName OutputBus { get; set; } = "SFX";

    [Export(PropertyHint.Range, "0,0.2,0.005")]
    public float RevealLeadSeconds { get; set; } = 0.035f;

    private AudioStreamPlayer _audio;
    private RobotSpeechClip _clip;
    private string _text = string.Empty;
    private int _visibleCharacters = -1;

    public override void _Ready()
    {
        _audio = GetNode<AudioStreamPlayer>("GeneratedVoice");
        _audio.Bus = OutputBus;
        _audio.Finished += OnAudioFinished;
        SetProcess(false);
    }

    public bool SpeakLine(
        string text,
        StringName emotion = default,
        RobotVoiceProfile overrideProfile = null)
    {
        if (_clip is not null)
            CancelLine();

        RobotVoiceProfile activeProfile = overrideProfile ?? VoiceProfile;
        if (activeProfile is null)
        {
            GD.PushWarning("RobotSpeechPlayer.SpeakLine requires a voice profile.");
            return false;
        }

        _text = text ?? string.Empty;
        _clip = RobotSpeechSynth.RenderLine(_text, activeProfile, emotion);
        _visibleCharacters = -1;
        _audio.Stream = _clip.Stream;
        _audio.Play();
        SetProcess(true);
        EmitSignal(SignalName.LineStarted, _text, _clip.Duration);
        UpdateReveal(0.0);
        return true;
    }

    public void StopLine(bool revealAll = true)
    {
        if (_clip is null)
            return;

        _audio.Stop();
        if (revealAll)
        {
            _visibleCharacters = _text.Length;
            EmitSignal(SignalName.LineProgress, _visibleCharacters, _clip.Duration);
        }
        FinishLine();
    }

    // Stops playback without firing completion signals during scene teardown.
    public void CancelLine()
    {
        _audio?.Stop();
        _clip = null;
        SetProcess(false);
    }

    public bool IsSpeaking() => _clip is not null && _audio.Playing;

    public override void _Process(double delta)
    {
        if (_clip is not null)
            UpdateReveal(_audio.GetPlaybackPosition() + RevealLeadSeconds);
    }

    private void UpdateReveal(double playbackSeconds)
    {
        if (_clip is null)
            return;

        int nextVisible = 0;
        for (int index = 1; index < _clip.CharacterTimes.Length; index++)
        {
            if (_clip.CharacterTimes[index] <= playbackSeconds)
                nextVisible = index;
            else
                break;
        }

        if (nextVisible == _visibleCharacters)
            return;

        _visibleCharacters = nextVisible;
        EmitSignal(SignalName.LineProgress, _visibleCharacters, playbackSeconds);
    }

    private void OnAudioFinished()
    {
        if (_clip is null)
            return;

        _visibleCharacters = _text.Length;
        EmitSignal(SignalName.LineProgress, _visibleCharacters, _clip.Duration);
        FinishLine();
    }

    private void FinishLine()
    {
        _clip = null;
        SetProcess(false);
        EmitSignal(SignalName.LineFinished);
    }
}
