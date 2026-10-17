using Godot;

// Lose card shown when the ball falls out of the world, laid out in
// scenes/ui/lose_card.tscn per the designer mock
// assets/UI_html/win_Lose.html. The win card lives in
// scenes/ui/win_card.tscn. Buttons use the shared MainMenu style.
public partial class LoseCard : Control
{
    private const string LevelSelectScene = "res://scenes/ui/level_select.tscn";
    private const string MenuScene = "res://scenes/main.tscn";

    private Button _retry;

    public override void _Ready()
    {
        var buttons = GetNode<HBoxContainer>("Center/Card/Box/Buttons");

        _retry = MainMenu.MenuButton("RETRY");
        _retry.Pressed += () => GetTree().ReloadCurrentScene();
        buttons.AddChild(_retry);

        var levelSelect = MainMenu.MenuButton("LEVEL SELECT");
        levelSelect.Pressed += () => GetTree().ChangeSceneToFile(LevelSelectScene);
        buttons.AddChild(levelSelect);

        var mainMenu = MainMenu.MenuButton("MAIN MENU");
        mainMenu.Pressed += () => GetTree().ChangeSceneToFile(MenuScene);
        buttons.AddChild(mainMenu);
    }

    public void Show()
    {
        Visible = true;
        SfxPlayer.Play(Sfx.Lose);
        _retry.GrabFocus();
    }
}
