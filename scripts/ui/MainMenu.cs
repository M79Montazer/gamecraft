using Godot;

// Main menu: layout lives in scenes/main.tscn; this only wires the buttons.
public partial class MainMenu : Control
{
	private const string LevelSelectScene = "res://scenes/ui/level_select.tscn";
	private const string SettingsScene = "res://scenes/ui/settings.tscn";

	// Designer spec from assets/UI_html/Button_ui.html.
	private static readonly Color Accent = new("87d1ef");
	private static readonly Color Bg = new(0.0014f, 0.0731f, 0.1026f);
	private static readonly Color BgBright = new(0.02f, 0.11f, 0.15f);

	public override void _Ready()
	{
		MusicPlayer.Instance.Play(0);

		var play = GetNode<Button>("Center/Row/ButtonsContainer/Play");
		var settings = GetNode<Button>("Center/Row/ButtonsContainer/Settings");
		var quit = GetNode<Button>("Center/Row/ButtonsContainer/Quit");

		ApplyButtonStyle(play);
		ApplyButtonStyle(settings);
		ApplyButtonStyle(quit);

		play.Pressed += () => GetTree().ChangeSceneToFile(LevelSelectScene);
		settings.Pressed += () => GetTree().ChangeSceneToFile(SettingsScene);
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

	// Volume slider styled to the theme; 0..100 with 1% steps, value01 = 0..1.
	internal static HSlider MenuSlider(float value01)
	{
		var slider = new HSlider
		{
			MinValue = 0,
			MaxValue = 100,
			Step = 1,
			Value = value01 * 100f,
			CustomMinimumSize = new Vector2(0f, 24f),
			SizeFlagsVertical = SizeFlags.ShrinkCenter,
			FocusMode = FocusModeEnum.All,
		};

		var groove = new StyleBoxFlat
		{
			BgColor = Bg,
			BorderColor = Accent,
			ContentMarginTop = 8f,
			ContentMarginBottom = 8f,
		};
		groove.SetBorderWidthAll(2);
		groove.SetCornerRadiusAll(10);

		var fill = new StyleBoxFlat { BgColor = Accent };
		fill.SetCornerRadiusAll(10);

		var fillFocus = new StyleBoxFlat { BgColor = new Color(1f, 1f, 1f) };
		fillFocus.SetCornerRadiusAll(10);

		slider.AddThemeStyleboxOverride("slider", groove);
		slider.AddThemeStyleboxOverride("grabber_area", fill);
		slider.AddThemeStyleboxOverride("grabber_area_highlight", fillFocus);
		slider.AddThemeIconOverride("grabber", GrabberIcon);
		slider.AddThemeIconOverride("grabber_highlight", GrabberIcon);
		slider.AddThemeIconOverride("grabber_disabled", GrabberIcon);
		return slider;
	}

	// Label + slider row sized to line up with MenuButton's 260px buttons.
	internal static HBoxContainer SliderRow(string text, HSlider slider)
	{
		var label = new Label { Text = text, CustomMinimumSize = new Vector2(90f, 0f) };
		label.AddThemeFontSizeOverride("font_size", 22);
		label.AddThemeFontOverride("font", GD.Load<Font>("res://assets/fonts/Sarpanch-SemiBold.ttf"));
		label.AddThemeColorOverride("font_color", Accent);

		slider.SizeFlagsHorizontal = SizeFlags.ExpandFill;

		var row = new HBoxContainer { CustomMinimumSize = new Vector2(300f, 52f) };
		row.AddChild(label);
		row.AddChild(slider);
		return row;
	}

	private static Texture2D _grabberIcon;

	// circular knob drawn in code so sliders match the accent theme without
	// needing an icon asset
	private static Texture2D GrabberIcon => _grabberIcon ??= CreateGrabberIcon();

	private static Texture2D CreateGrabberIcon()
	{
		const int size = 24;
		const float radius = 11f;

		var img = Image.CreateEmpty(size, size, false, Image.Format.Rgba8);
		var center = new Vector2(size / 2f - 0.5f, size / 2f - 0.5f);
		for (int y = 0; y < size; y++)
		{
			for (int x = 0; x < size; x++)
			{
				float d = center.DistanceTo(new Vector2(x, y));
				if (d > radius)
					continue;
				// one-pixel alpha falloff smooths the edge
				float edge = Mathf.Clamp(radius - d, 0f, 1f);
				var color = d > radius - 4f ? new Color(1f, 1f, 1f) : Accent;
				img.SetPixel(x, y, new Color(color, edge));
			}
		}
		return ImageTexture.CreateFromImage(img);
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

		// every styled button gets the standard UI sounds — this is the shared
		// choke point for all menu buttons (level-select cards wire their own).
		// Mouse hover only: menus GrabFocus() on open, and FocusEntered would
		// play a spurious hover sound every time one appears.
		b.MouseEntered += () => SfxPlayer.Play(Sfx.UiHover);
		b.Pressed += () => SfxPlayer.Play(Sfx.UiClick);
	}
}
