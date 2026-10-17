using Godot;

// Plays level-introduction dialogue through the editor-authored controls in
// stage_dialogue.tscn. Visual nodes and audio players live in the scene; this
// controller only supplies content and playback state.
public partial class StageDialogue : Control
{
    private enum DialogueState { Typing, WaitingForInput, Finished }

    [ExportGroup("Dialogue")]
    [Export] public Godot.Collections.Array<DialogueLine> Lines { get; set; } = new();
    [Export] public PackedScene NextStage { get; set; }

    [ExportGroup("Fallback Typewriter")]
    [Export] public float CharactersPerSecond { get; set; } = 35.0f;

    [ExportGroup("Audio")]
    [Export(PropertyHint.Range, "-30,0,0.5")]
    public float MusicDuckDb { get; set; } = -12.0f;

    [ExportGroup("Characters")]
    // Leave empty to use the Name label authored in stage_dialogue.tscn.
    // Set these on a level's intro_dialogue scene for level-specific names.
    [Export] public string UpperCharacterName { get; set; } = string.Empty;
    [Export] public string LowerCharacterName { get; set; } = string.Empty;
    [Export] public Texture2D UpperAvatar { get; set; }
    [Export] public Texture2D LowerAvatar { get; set; }
    [Export] public RobotVoiceProfile UpperVoiceProfile { get; set; }
    [Export] public RobotVoiceProfile LowerVoiceProfile { get; set; }

    [ExportGroup("Background")]
    [Export] public float BlurStrength { get; set; } = 2.0f;
    [Export] public float BackdropDarkness { get; set; } = 0.35f;

    private ColorRect _backdrop;
    private Control _upperDialog;
    private Control _lowerDialog;
    private TextureRect _upperAvatar;
    private TextureRect _lowerAvatar;
    private Label _upperName;
    private Label _lowerName;
    private RichTextLabel _upperText;
    private RichTextLabel _lowerText;
    private Label _continueHint;
    private RobotSpeechPlayer _robotSpeech;

    private DialogueState _state = DialogueState.Finished;
    private int _currentLineIndex;
    private float _typeProgress;
    private RichTextLabel _activeText;
    private bool _usingSpeechTiming;
    private bool _transitioned;
    private bool _musicDucked;

    public bool HasLines => Lines is { Count: > 0 };

    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;
        MouseFilter = MouseFilterEnum.Ignore;

        BindSceneNodes();
        ConfigureScene();
        EnsureAdvanceAction();

        if (HasLines)
        {
            SetMusicDucked(true);
            _currentLineIndex = 0;
            StartLine();
        }
        else
        {
            Hide();
            _state = DialogueState.Finished;
        }
    }

    private void BindSceneNodes()
    {
        const string rows = "DialogueCenter/DialogueRows";
        _backdrop = GetNode<ColorRect>("Backdrop");
        _upperDialog = GetNode<Control>($"{rows}/UpperDialog");
        _lowerDialog = GetNode<Control>($"{rows}/LowerDialog");
        _upperAvatar = GetNode<TextureRect>($"{rows}/UpperDialog/Avatar");
        _lowerAvatar = GetNode<TextureRect>($"{rows}/LowerDialog/Avatar");
        _upperName = GetNode<Label>($"{rows}/UpperDialog/Name");
        _lowerName = GetNode<Label>($"{rows}/LowerDialog/Name");
        _upperText = GetNode<RichTextLabel>($"{rows}/UpperDialog/Message");
        _lowerText = GetNode<RichTextLabel>($"{rows}/LowerDialog/Message");
        _continueHint = GetNode<Label>("ContinueHint");
        _robotSpeech = GetNode<RobotSpeechPlayer>("RobotSpeechPlayer");

        _robotSpeech.LineProgress += OnSpeechProgress;
        _robotSpeech.LineFinished += OnSpeechFinished;
    }

    private void ConfigureScene()
    {
        _upperAvatar.Texture = UpperAvatar;
        _lowerAvatar.Texture = LowerAvatar;
        ConfigureCharacterName(_upperName, UpperCharacterName, UpperVoiceProfile);
        ConfigureCharacterName(_lowerName, LowerCharacterName, LowerVoiceProfile);

        if (_backdrop.Material is ShaderMaterial material)
        {
            material.SetShaderParameter("blur_strength", BlurStrength);
            material.SetShaderParameter("darkness", BackdropDarkness);
        }

        _upperText.Text = string.Empty;
        _lowerText.Text = string.Empty;
        _continueHint.Hide();
    }

    private static void ConfigureCharacterName(
        Label label,
        string characterName,
        RobotVoiceProfile voiceProfile)
    {
        // An explicit scene override wins. Otherwise preserve the label text
        // exactly as authored; the profile name is only a fallback for a blank
        // label, so runtime code can never undo an editor name change again.
        if (!string.IsNullOrWhiteSpace(characterName))
            label.Text = characterName.ToUpperInvariant();
        else if (string.IsNullOrWhiteSpace(label.Text))
            label.Text = voiceProfile?.DisplayName?.ToUpperInvariant() ?? "ROBOT";
    }

    private static void EnsureAdvanceAction()
    {
        if (InputMap.HasAction("dialogue_advance"))
            return;
        InputMap.AddAction("dialogue_advance");
        InputMap.ActionAddEvent("dialogue_advance", new InputEventKey { PhysicalKeycode = Key.Space });
        InputMap.ActionAddEvent("dialogue_advance", new InputEventKey { PhysicalKeycode = Key.Enter });
    }

    private void StartLine()
    {
        DialogueLine line = Lines[_currentLineIndex];
        bool upperSpeaks = line.Speaker == DialogueSpeaker.Upper;
        _activeText = upperSpeaks ? _upperText : _lowerText;
        RobotVoiceProfile profile = upperSpeaks ? UpperVoiceProfile : LowerVoiceProfile;

        _activeText.Text = line.Text;
        SetMessageFontSize(_activeText, line.Text?.Length ?? 0);
        _activeText.VisibleCharacters = 0;
        SetSpeakerHighlight(upperSpeaks);

        _typeProgress = 0f;
        _state = DialogueState.Typing;
        _continueHint.Hide();
        _usingSpeechTiming = _robotSpeech.SpeakLine(line.Text, EmotionName(line.Emotion), profile);
    }

    // Preserve the mockup's 28 px type for normal lines, while keeping the
    // few long dispatch messages inside the authored 266 px card.
    private static void SetMessageFontSize(RichTextLabel label, int characterCount)
    {
        int fontSize = characterCount > 110 ? 20
            : characterCount > 75 ? 23
            : characterCount > 55 ? 26
            : 28;
        label.AddThemeFontSizeOverride("normal_font_size", fontSize);
    }

    private void SetSpeakerHighlight(bool upperSpeaks)
    {
        Color active = Colors.White;
        Color inactive = new(0.58f, 0.68f, 0.72f, 0.66f);
        _upperDialog.Modulate = upperSpeaks ? active : inactive;
        _lowerDialog.Modulate = upperSpeaks ? inactive : active;
    }

    private static StringName EmotionName(DialogueEmotion emotion) => emotion switch
    {
        DialogueEmotion.Urgent => RobotSpeechSynth.Urgent,
        DialogueEmotion.Sad => RobotSpeechSynth.Sad,
        DialogueEmotion.Curious => RobotSpeechSynth.Curious,
        DialogueEmotion.Happy => RobotSpeechSynth.Happy,
        _ => RobotSpeechSynth.Neutral,
    };

    public override void _Process(double delta)
    {
        if (_state != DialogueState.Typing || _usingSpeechTiming)
            return;

        int total = _activeText.GetTotalCharacterCount();
        _typeProgress += (float)delta * CharactersPerSecond;
        int shown = Mathf.Min((int)_typeProgress, total);
        _activeText.VisibleCharacters = shown;
        if (shown >= total)
            MarkLineComplete();
    }

    private void OnSpeechProgress(int visibleCharacters, double playbackSeconds)
    {
        if (_state == DialogueState.Typing && _activeText is not null)
            _activeText.VisibleCharacters = visibleCharacters;
    }

    private void OnSpeechFinished()
    {
        if (_state == DialogueState.Typing)
            MarkLineComplete();
    }

    private void MarkLineComplete()
    {
        _activeText.VisibleCharacters = -1;
        _state = DialogueState.WaitingForInput;
        _continueHint.Show();
    }

    private void SetMusicDucked(bool ducked)
    {
        if (_musicDucked == ducked)
            return;

        _musicDucked = ducked;
        MusicPlayer.Instance?.SetDialogueDuck(ducked, MusicDuckDb);
    }

    public override void _ExitTree()
    {
        // Also restore the track if another scene removes the dialogue before
        // its normal Finish path runs.
        SetMusicDucked(false);
    }

    public override void _UnhandledInput(InputEvent inputEvent)
    {
        if (_state == DialogueState.Finished || !Visible)
            return;
        if (inputEvent.IsEcho() || !inputEvent.IsActionPressed("dialogue_advance"))
            return;

        GetViewport().SetInputAsHandled();
        Advance();
    }

    private void Advance()
    {
        if (_state == DialogueState.Typing)
        {
            if (_usingSpeechTiming)
                _robotSpeech.StopLine(true);
            else
                MarkLineComplete();
            return;
        }

        if (_state != DialogueState.WaitingForInput)
            return;

        if (_currentLineIndex >= Lines.Count - 1)
        {
            Finish();
            return;
        }

        _currentLineIndex++;
        StartLine();
    }

    private void Finish()
    {
        _state = DialogueState.Finished;
        _robotSpeech.CancelLine();
        SetMusicDucked(false);
        Hide();
        GetTree().Paused = false;

        if (NextStage is not null && !_transitioned)
        {
            _transitioned = true;
            GetTree().ChangeSceneToFile(NextStage.ResourcePath);
        }
        QueueFree();
    }
}
