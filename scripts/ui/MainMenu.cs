using Godot;

// Main menu: layout lives in scenes/main.tscn; this only wires the buttons.
public partial class MainMenu : Control
{
	private const string LevelSelectScene = "res://scenes/ui/level_select.tscn";

	// Designer spec from assets/UI_html/Button_ui.html.
	private static readonly Color Accent = new("87d1ef");
	private static readonly Color Bg = new(0.0014f, 0.0731f, 0.1026f);
	private static readonly Color BgBright = new(0.02f, 0.11f, 0.15f);

	public override void _Ready()
	{
		var play = GetNode<Button>("Center/Row/ButtonsContainer/Play");
		var quit = GetNode<Button>("Center/Row/ButtonsContainer/Quit");

		ApplyButtonStyle(play);
		ApplyButtonStyle(quit);

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
		ApplyButtonStyle(b);
		return b;
	}

	internal static void ApplyButtonStyle(Button b)
	{
		b.AddThemeFontSizeOverride("font_size", 22);
		b.AddThemeFontOverride("font", GD.Load<Font>("res://assets/fonts/Sarpanch-SemiBold.ttf"));
		b.AddThemeColorOverride("font_color", Accent);
		b.AddThemeColorOverride("font_hover_color", new Color(1f, 1f, 1f));
		b.AddThemeColorOverride("font_pressed_color", Accent);
		b.AddThemeColorOverride("font_focus_color", Accent);

		var normal = new StyleBoxFlat
		{
			BgColor = Bg,
			BorderColor = Accent,
			ContentMarginLeft = 24f,
			ContentMarginRight = 24f,
			ContentMarginTop = 10f,
			ContentMarginBottom = 10f,
		};
		normal.SetBorderWidthAll(2);
		normal.SetCornerRadiusAll(20);

		// Glow approximates the HTML box-shadow (0 0 38px on hover, 16px on press).
		var hover = (StyleBoxFlat)normal.Duplicate();
		hover.BgColor = BgBright;
		hover.ShadowColor = new Color(Accent, 0.55f);
		hover.ShadowSize = 19;

		var pressed = (StyleBoxFlat)normal.Duplicate();
		pressed.ShadowColor = new Color(Accent, 0.3f);
		pressed.ShadowSize = 8;

		var focus = (StyleBoxFlat)normal.Duplicate();
		focus.DrawCenter = false;

		b.AddThemeStyleboxOverride("normal", normal);
		b.AddThemeStyleboxOverride("hover", hover);
		b.AddThemeStyleboxOverride("pressed", pressed);
		b.AddThemeStyleboxOverride("focus", focus);
	}
}
