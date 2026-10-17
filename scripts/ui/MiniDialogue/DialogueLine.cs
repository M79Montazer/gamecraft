using Godot;

// One entry in a stage-intro conversation: who says it and what they say.
// It is a Resource so lines can be added/edited directly in the Inspector
// (on the StageDialogue node's "Lines" array) without touching any code.
[GlobalClass]
public partial class DialogueLine : Resource
{
    // Which of the two characters speaks this line (drives which box is used
    // and which border gets highlighted while the line types out).
    [Export]
    public DialogueSpeaker Speaker { get; set; } = DialogueSpeaker.Upper;

    // The text typed into that character's dialogue box.
    [Export]
    public string Text { get; set; } = "";

    // Optional delivery style for the procedural robot voice. Existing lines
    // remain neutral; authors can tune each line from its Inspector resource.
    [Export]
    public DialogueEmotion Emotion { get; set; } = DialogueEmotion.Neutral;
}

// The two fixed characters of the dialogue layout.
public enum DialogueSpeaker
{
    Upper, // upper-left character: avatar on the left, box on the right
    Lower, // lower-left character: box on the left, avatar on the right
}

public enum DialogueEmotion
{
    Neutral,
    Urgent,
    Sad,
    Curious,
    Happy,
}
