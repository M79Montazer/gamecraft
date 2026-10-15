using Godot;
using System.Collections.Generic;

// StageDialogue — the entire stage-introduction dialogue system in one script.
//
// Responsibilities:
//   * Builds the two-character dialogue layout in code (avatar + dialogue box
//     per character, matching the project's code-built UI convention).
//   * Plays a list of DialogueLine resources with a typewriter effect.
//   * Highlights the border of whoever is currently speaking.
//   * Listens for the "dialogue_advance" input (Space / Enter) to reveal
//     skipped text, move to the next line, and finally close the dialogue.
//   * On close, unpauses the game and — if NextStage is assigned — loads
//     that scene (a stage transition). Otherwise gameplay simply resumes.
//
// It is instantiated by LevelRunner as an overlay on top of the level while
// the whole scene tree is PAUSED, which is why ProcessMode is set to Always:
// a paused node would never receive _Process or input events otherwise.
public partial class StageDialogue : Control
{
    // The three phases of a dialogue run:
    //   Typing          — the current line is being revealed character by character.
    //   WaitingForInput — the line is fully visible; waiting for the player to advance.
    //   Finished        — the dialogue is done; the node is hidden/freed.
    private enum DialogueState { Typing, WaitingForInput, Finished }

    [ExportGroup("Dialogue")]
    // The conversation itself: edit this array in the Inspector.
    // Each entry says which character speaks and what text they say.
    [Export]
    public Godot.Collections.Array<DialogueLine> Lines { get; set; } = new();

    // Optional: scene loaded when the dialogue finishes. Leave empty for a
    // plain intro overlay (the level you are already in just resumes).
    [Export]
    public PackedScene NextStage { get; set; }

    [ExportGroup("Typewriter")]
    // How many characters per second are revealed while typing.
    [Export]
    public float CharactersPerSecond { get; set; } = 35.0f;

    [ExportGroup("Avatars")]
    // Optional avatar images. Drag any picture from the FileSystem dock onto
    // these slots in the Inspector; if left empty, a colored placeholder shows.
    [Export] public Texture2D UpperAvatar { get; set; }
    [Export] public Texture2D LowerAvatar { get; set; }
    [Export] public Color UpperAvatarColor { get; set; } = new(0.15f, 0.55f, 0.62f);
    [Export] public Color LowerAvatarColor { get; set; } = new(0.55f, 0.30f, 0.62f);

    // Border colors for the dialogue boxes: NormalBorder is the idle look,
    // ActiveBorder marks the character who is currently speaking.
    [ExportGroup("Colors")]
    [Export] public Color NormalBorderColor { get; set; } = new(0.35f, 0.36f, 0.44f);
    [Export] public Color ActiveBorderColor { get; set; } = new(1.0f, 0.82f, 0.30f);
    [Export] public Color BoxBackgroundColor { get; set; } = new(0.05f, 0.05f, 0.10f, 0.92f);

    // How strongly the level map is blurred behind the dialogue (0 = sharp).
    [ExportGroup("Background")]
    [Export] public float BlurStrength { get; set; } = 2.0f;

    // How much the blurred map is darkened for text readability (0 = none).
    [Export] public float BackdropDarkness { get; set; } = 0.35f;

    // Background color of the card behind both dialogue groups; the alpha
    // (0-1) controls how see-through the card is.
    [Export] public Color CardBackgroundColor { get; set; } = new(0.02f, 0.02f, 0.05f, 0.55f);

    // --- node references (created in BuildLayout) ---
    private RichTextLabel _upperText;
    private RichTextLabel _lowerText;
    private PanelContainer _upperBox;
    private PanelContainer _lowerBox;
    private Label _continueHint;

    // Border styles per box: slot 0 = idle, slot 1 = highlighted (active
    // speaker). Index 0 of the array = upper box, index 1 = lower box.
    private readonly List<StyleBoxFlat>[] _boxStyles = new List<StyleBoxFlat>[2];

    // --- dialogue run state ---
    private DialogueState _state = DialogueState.Finished;
    private int _currentLineIndex;      // index into Lines of the line being played
    private float _typeProgress;        // characters revealed so far (fractional)
    private RichTextLabel _activeText;  // label of whoever is currently speaking
    private bool _transitioned;         // guards against loading the next stage twice

    // True when there is something to play. LevelRunner checks this to decide
    // whether to pause the game or skip the dialogue entirely.
    public bool HasLines => Lines is { Count: > 0 };

    public override void _Ready()
    {
        // Run even while the tree is paused (the game is paused during the intro).
        ProcessMode = ProcessModeEnum.Always;

        // Fill the whole screen and never swallow mouse clicks aimed at the game.
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        MouseFilter = MouseFilterEnum.Ignore;

        EnsureAdvanceAction();
        BuildLayout();

        if (HasLines)
        {
            _currentLineIndex = 0;
            StartLine();
        }
        else
        {
            // Nothing to say: hide immediately (spec: skip straight to the stage).
            Hide();
            _state = DialogueState.Finished;
        }
    }

    // Registers the "dialogue_advance" action in the InputMap at runtime, the
    // same pattern LevelRunner uses for movement keys. Space and Enter both
    // advance the dialogue. Existing actions are never touched.
    private static void EnsureAdvanceAction()
    {
        if (InputMap.HasAction("dialogue_advance"))
            return;
        InputMap.AddAction("dialogue_advance");
        InputMap.ActionAddEvent("dialogue_advance", new InputEventKey { PhysicalKeycode = Key.Space });
        InputMap.ActionAddEvent("dialogue_advance", new InputEventKey { PhysicalKeycode = Key.Enter });
    }

    // Builds the on-screen layout in code:
    //   * a full-screen blurred backdrop over the paused level map,
    //   * the two character groups centered on screen as one package:
    //         Upper:  [avatar][dialogue box]
    //         Lower:  [dialogue box][avatar]
    // A CenterContainer keeps the package centered at any window size.
    private void BuildLayout()
    {
        AddChild(MakeBlurBackdrop());

        _upperText = MakeTextLabel();
        _lowerText = MakeTextLabel();

        // Upper character: avatar left, box right. Lower character: box left,
        // avatar right. The VBox stacks them; the CenterContainer centers both.
        _upperBox = MakeDialogueBox(_upperText, 0);
        _lowerBox = MakeDialogueBox(_lowerText, 1);

        var column = new VBoxContainer();
        column.AddThemeConstantOverride("separation", 70);
        column.AddChild(MakeGroup(MakeAvatar(UpperAvatarColor, UpperAvatar), _upperBox));
        column.AddChild(MakeGroup(_lowerBox, MakeAvatar(LowerAvatarColor, LowerAvatar)));

        // Semi-transparent panel behind the whole package so both conversations
        // read as one card over the blurred map (PanelContainer is the only
        // container that can draw a StyleBox background).
        var card = new PanelContainer();
        var cardStyle = new StyleBoxFlat
        {
            BgColor = CardBackgroundColor,
        };
        cardStyle.SetCornerRadiusAll(14);
        cardStyle.SetContentMarginAll(30);
        card.AddThemeStyleboxOverride("panel", cardStyle);
        card.AddChild(column);

        var center = new CenterContainer();
        center.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        center.AddChild(card);
        AddChild(center);

        // Small hint shown once a line is fully revealed.
        _continueHint = new Label { Text = "Space / Enter", Modulate = new Color(1, 1, 1, 0.6f) };
        AddChild(_continueHint);
        _continueHint.SetAnchorsAndOffsetsPreset(LayoutPreset.BottomRight);
        _continueHint.OffsetLeft = -220;
        _continueHint.OffsetTop = -50;
        _continueHint.OffsetRight = -40;
        _continueHint.OffsetBottom = -20;
        _continueHint.Hide();
    }

    // An HBoxContainer that lays its two children side by side.
    private static HBoxContainer MakeGroup(Control left, Control right)
    {
        var box = new HBoxContainer();
        box.AddThemeConstantOverride("separation", 16);
        box.AddChild(left);
        box.AddChild(right);
        return box;
    }

    // Full-screen ColorRect using the blur shader: it draws the level behind
    // it blurred and darkened. Added as the dialogue's first child, so it sits
    // behind the dialogue boxes and disappears with them on QueueFree.
    private ColorRect MakeBlurBackdrop()
    {
        var shader = GD.Load<Shader>("res://scenes/ui/Dialogue/blur_background.gdshader");
        var material = new ShaderMaterial { Shader = shader };
        material.SetShaderParameter("blur_strength", BlurStrength);
        material.SetShaderParameter("darkness", BackdropDarkness);

        var rect = new ColorRect { Material = material };
        rect.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        rect.MouseFilter = MouseFilterEnum.Ignore;
        return rect;
    }

    // A dialogue box: dark panel with a border, holding one RichTextLabel.
    // Two StyleBoxFlats are created per box (idle + highlighted) and swapped
    // in SetSpeakerHighlight as the conversation moves between characters.
    private PanelContainer MakeDialogueBox(RichTextLabel text, int boxIndex)
    {
        var normal = MakeBorder(BoxBackgroundColor, NormalBorderColor);
        var active = MakeBorder(BoxBackgroundColor, ActiveBorderColor);
        _boxStyles[boxIndex] = new List<StyleBoxFlat> { normal, active };

        var panel = new PanelContainer { CustomMinimumSize = new Vector2(480, 0) };
        panel.AddThemeStyleboxOverride("panel", normal);

        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_left", 14);
        margin.AddThemeConstantOverride("margin_top", 14);
        margin.AddThemeConstantOverride("margin_right", 14);
        margin.AddThemeConstantOverride("margin_bottom", 14);
        panel.AddChild(margin);
        margin.AddChild(text);
        return panel;
    }

    private static StyleBoxFlat MakeBorder(Color background, Color border)
    {
        var style = new StyleBoxFlat { BgColor = background, BorderColor = border };
        style.SetBorderWidthAll(3);
        style.SetCornerRadiusAll(8);
        return style;
    }

    private static RichTextLabel MakeTextLabel()
    {
        return new RichTextLabel
        {
            FitContent = true,    // box height follows the text
            ScrollActive = false, // never show scrollbars
            CustomMinimumSize = new Vector2(450, 0),
        };
    }

    // Avatar = colored panel (placeholder) with an optional image on top.
    private static PanelContainer MakeAvatar(Color color, Texture2D texture)
    {
        var panel = new PanelContainer { CustomMinimumSize = new Vector2(96, 96) };
        var style = new StyleBoxFlat { BgColor = color };
        style.SetCornerRadiusAll(8);
        panel.AddThemeStyleboxOverride("panel", style);

        if (texture != null)
        {
            var rect = new TextureRect
            {
                Texture = texture,
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            };
            panel.AddChild(rect);
        }
        return panel;
    }

    // Starts playing line _currentLineIndex:
    //   * clears ONLY the speaking character's box (the other character keeps
    //     their previous line visible, which is the desired behavior),
    //   * highlights that character's border and un-highlights the other,
    //   * resets the typewriter for the new text.
    private void StartLine()
    {
        DialogueLine line = Lines[_currentLineIndex];

        bool upperSpeaks = line.Speaker == DialogueSpeaker.Upper;
        _activeText = upperSpeaks ? _upperText : _lowerText;

        // Remove the previous text of the character about to speak.
        _activeText.Text = line.Text;
        _activeText.VisibleCharacters = 0;

        SetSpeakerHighlight(upperSpeaks ? 0 : 1);

        _typeProgress = 0f;
        _state = DialogueState.Typing;
        _continueHint.Hide();
    }

    // Swaps the border style of both boxes so only the speaker (given as
    // 0 = upper or 1 = lower) uses the highlighted variant.
    private void SetSpeakerHighlight(int speakerIndex)
    {
        for (int i = 0; i < 2; i++)
        {
            PanelContainer box = i == 0 ? _upperBox : _lowerBox;
            box.AddThemeStyleboxOverride("panel", _boxStyles[i][i == speakerIndex ? 1 : 0]);
        }
    }

    // The typewriter: every frame while Typing, grow _typeProgress by
    // (speed * delta) characters and reveal that many via VisibleCharacters.
    // No timers or async tasks are involved, so restarting or skipping can
    // never leave a stale animation running.
    public override void _Process(double delta)
    {
        if (_state != DialogueState.Typing)
            return;

        int total = _activeText.GetTotalCharacterCount();
        _typeProgress += (float)delta * CharactersPerSecond;
        int shown = Mathf.Min((int)_typeProgress, total);
        _activeText.VisibleCharacters = shown;

        if (shown >= total)
        {
            // Line fully revealed: wait for the next press before advancing.
            _state = DialogueState.WaitingForInput;
            _continueHint.Show();
        }
    }

    // Called for input events not consumed by the UI. One press per physical
    // key press: IsEcho() filters held-key auto-repeat so a held key cannot
    // skip several lines at once.
    public override void _UnhandledInput(InputEvent e)
    {
        if (_state == DialogueState.Finished || !Visible)
            return;
        if (e.IsEcho() || !e.IsActionPressed("dialogue_advance"))
            return;

        GetViewport().SetInputAsHandled();
        Advance();
    }

    // Moves the dialogue forward one step. Called once per fresh key press.
    private void Advance()
    {
        switch (_state)
        {
            case DialogueState.Typing:
                // First press while typing: reveal the whole line but do NOT
                // advance — the same press must not also skip to the next line.
                _typeProgress = float.MaxValue;
                _activeText.VisibleCharacters = -1; // -1 shows every character
                _state = DialogueState.WaitingForInput;
                _continueHint.Show();
                return;

            case DialogueState.WaitingForInput:
                if (_currentLineIndex >= Lines.Count - 1)
                {
                    // Last line was already fully shown: this press closes.
                    Finish();
                    return;
                }
                _currentLineIndex++;
                StartLine();
                return;
        }
    }

    // Ends the dialogue: hide the UI, unpause the game, and — only if a next
    // stage was assigned — switch to it exactly once (_transitioned guard).
    private void Finish()
    {
        _state = DialogueState.Finished;
        Hide();
        GetTree().Paused = false;

        if (NextStage != null && !_transitioned)
        {
            _transitioned = true;
            GetTree().ChangeSceneToFile(NextStage.ResourcePath);
        }
        QueueFree();
    }
}
