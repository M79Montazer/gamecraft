using Godot;

// Main menu: layout lives in scenes/main.tscn; this only wires the buttons.
public partial class MainMenu : Control
{
	private const string LevelSelectScene = "res://scenes/ui/level_select.tscn";

	public override void _Ready()
	{
		var play = GetNode<Button>("Center/Box/Play");
		var quit = GetNode<Button>("Center/Box/Quit");

		play.Pressed += () => GetTree().ChangeSceneToFile(LevelSelectScene);
		quit.Pressed += () => GetTree().Quit();

		play.GrabFocus();
	}

	internal static Button MenuButton(string text)
	{
		var b = new Button
		{
			Text = text,
			CustomMinimumSize = new Vector2(260f, 52f),
			FocusMode = FocusModeEnum.All,
		};
		b.AddThemeFontSizeOverride("font_size", 22);

		var normal = new StyleBoxFlat
		{
			BgColor = new Color(0.06f, 0.08f, 0.14f, 0.9f),
			BorderColor = new Color(0.25f, 0.5f, 0.6f, 0.5f),
			ContentMarginLeft = 24f,
			ContentMarginRight = 24f,
			ContentMarginTop = 10f,
			ContentMarginBottom = 10f,
		};
		normal.SetBorderWidthAll(1);
		normal.SetCornerRadiusAll(6);

		var hover = (StyleBoxFlat)normal.Duplicate();
		hover.BgColor = new Color(0.1f, 0.16f, 0.24f, 0.95f);
		hover.BorderColor = new Color(0.35f, 0.8f, 0.95f, 0.9f);

		var pressed = (StyleBoxFlat)normal.Duplicate();
		pressed.BgColor = new Color(0.04f, 0.06f, 0.1f);

		var focus = (StyleBoxFlat)normal.Duplicate();
		focus.BorderColor = new Color(0.35f, 0.8f, 0.95f);
		focus.SetBorderWidthAll(2);
		focus.DrawCenter = false;

		b.AddThemeStyleboxOverride("normal", normal);
		b.AddThemeStyleboxOverride("hover", hover);
		b.AddThemeStyleboxOverride("pressed", pressed);
		b.AddThemeStyleboxOverride("focus", focus);
		return b;
	}
}
