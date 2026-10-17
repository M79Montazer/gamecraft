using Godot;

// In-game HUD layer: status line, pause overlay, end-of-run overlays and
// control hints. Visuals live in scenes/ui/hud.tscn (the win card in
// scenes/ui/win_card.tscn, the lose card in scenes/ui/lose_card.tscn, the
// pause menu in scenes/ui/pause_menu.tscn); this script only drives visibility.
public partial class Hud : CanvasLayer
{
	private Label _hud;
	private Label _hint;
	private WinCard _winCard;
	private LoseCard _loseCard;
	private PauseMenu _pauseMenu;

	// the LevelRunner opens/closes the pause overlay and subscribes to its
	// Resumed event, so the instance is exposed instead of a pass-through
	public PauseMenu PauseMenu => _pauseMenu;

	public override void _Ready()
	{
		// the end cards must receive input while the tree is paused on them
		ProcessMode = ProcessModeEnum.Always;
		_hud = GetNode<Label>("Hud");
		_hint = GetNode<Label>("Hint");
		_winCard = GetNode<WinCard>("WinCard");
		_loseCard = GetNode<LoseCard>("LoseCard");
		_pauseMenu = GetNode<PauseMenu>("PauseMenu");
	}

	public void SetStatus(string text) => _hud.Text = text;

	public void ShowLose()
	{
		_hud.Visible = false;
		_hint.Visible = false;
		_loseCard.Show();
	}

	public void ShowScoreCard(int stars, float time, string nextLevelPath = null)
	{
		_hud.Visible = false;
		_hint.Visible = false;
		_winCard.Show(stars, time, nextLevelPath);
	}
}
