using Godot;

// The end-of-run score card, laid out entirely in scenes/ui/score_card.tscn.
// The runtime only sets the time text and tints the star icons.
public partial class ScoreCard : Control
{
    private static readonly Color EarnedColor = new(1f, 0.83f, 0.2f);
    private static readonly Color MissingColor = new(0.25f, 0.26f, 0.32f);

    private Label _time;
    private TextureRect[] _stars = new TextureRect[3];

    public override void _Ready()
    {
        _time = GetNode<Label>("Center/Panel/Box/Time");
        for (int i = 0; i < 3; i++)
            _stars[i] = GetNode<TextureRect>($"Center/Panel/Box/Stars/Star{i + 1}");
    }

    public void Show(int stars, float time)
    {
        Visible = true;
        _time.Text = $"time  {time:0.0} s";
        for (int i = 0; i < 3; i++)
            _stars[i].Modulate = i < stars ? EarnedColor : MissingColor;
    }
}
