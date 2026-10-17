using Godot;

// Win card shown after escaping a level, laid out in scenes/ui/win_card.tscn
// per the designer mock assets/UI_html/win_Lose.html. The lose card lives in
// scenes/ui/lose_card.tscn. Buttons use the shared MainMenu style.
public partial class WinCard : Control
{
    private static readonly Color Accent = new("87d1ef");

    private const string LevelSelectScene = "res://scenes/ui/level_select.tscn";
    private const string MenuScene = "res://scenes/main.tscn";

    private StarCluster _starCluster;
    private Label _time;
    private Button _nextLevel;
    private Button _retry;
    private string _nextLevelPath;

    public override void _Ready()
    {
        var stars = GetNode<CenterContainer>("Center/Card/Box/Stars");
        _time = GetNode<Label>("Center/Card/Box/Time");

        // same cluster as the level-select cards, drawn at the mock's size
        _starCluster = new StarCluster { Color = Accent, Scale = new Vector2(1.4f, 1.4f) };
        stars.AddChild(_starCluster);

        var buttons = GetNode<HBoxContainer>("Center/Card/Box/Buttons");
        _nextLevel = MainMenu.MenuButton("NEXT LEVEL");
        _nextLevel.Pressed += OnNextLevelPressed;
        buttons.AddChild(_nextLevel);

        _retry = MainMenu.MenuButton("RETRY");
        _retry.Pressed += () => GetTree().ReloadCurrentScene();
        buttons.AddChild(_retry);

        var mainMenu = MainMenu.MenuButton("MAIN MENU");
        mainMenu.Pressed += () => GetTree().ChangeSceneToFile(MenuScene);
        buttons.AddChild(mainMenu);
    }

    // NEXT LEVEL hides on the last level and focus shifts to RETRY.
    public void Show(int stars, float time, string nextLevelPath = null)
    {
        _nextLevelPath = nextLevelPath;
        _starCluster.Filled = stars;
        _starCluster.QueueRedraw();
        _time.Text = $"{time:0.0} s";
        Visible = true;
        _nextLevel.Visible = nextLevelPath != null;
        (_nextLevel.Visible ? _nextLevel : _retry).GrabFocus();
    }

    private void OnNextLevelPressed()
    {
        if (_nextLevelPath != null)
            GetTree().ChangeSceneToFile(_nextLevelPath);
    }
}
