using Godot;

// The end-of-run score card, laid out in scenes/ui/score_card.tscn.
// The runtime sets the time text, tints the star icons, and wires the
// next-level / level-select buttons shown once the game is paused.
public partial class ScoreCard : Control
{
    private static readonly Color EarnedColor = new(1f, 0.83f, 0.2f);
    private static readonly Color MissingColor = new(0.25f, 0.26f, 0.32f);

    private const string LevelSelectScene = "res://scenes/ui/level_select.tscn";

    private Label _time;
    private TextureRect[] _stars = new TextureRect[3];
    private Button _nextLevel;
    private Button _levelSelect;
    private string _nextLevelPath;

    public override void _Ready()
    {
        _time = GetNode<Label>("Center/Panel/Box/Time");
        for (int i = 0; i < 3; i++)
            _stars[i] = GetNode<TextureRect>($"Center/Panel/Box/Stars/Star{i + 1}");

        var buttons = GetNode<HBoxContainer>("Center/Panel/Box/Buttons");
        _nextLevel = MainMenu.MenuButton("NEXT LEVEL");
        _nextLevel.Pressed += OnNextLevelPressed;
        buttons.AddChild(_nextLevel);

        _levelSelect = MainMenu.MenuButton("LEVEL SELECT");
        _levelSelect.Pressed += () => GetTree().ChangeSceneToFile(LevelSelectScene);
        buttons.AddChild(_levelSelect);
    }

    public void Show(int stars, float time, string nextLevelPath = null)
    {
        Visible = true;
        _time.Text = $"time  {time:0.0} s";
        for (int i = 0; i < 3; i++)
            _stars[i].Modulate = i < stars ? EarnedColor : MissingColor;

        _nextLevelPath = nextLevelPath;
        _nextLevel.Visible = _nextLevelPath != null;
        (_nextLevel.Visible ? _nextLevel : _levelSelect).GrabFocus();
    }

    private void OnNextLevelPressed()
    {
        if (_nextLevelPath != null)
            GetTree().ChangeSceneToFile(_nextLevelPath);
    }
}
