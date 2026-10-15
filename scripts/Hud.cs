using Godot;

// In-game HUD layer: status line, end-of-run overlays and control hints.
// Visuals live in scenes/ui/hud.tscn (the win card in scenes/ui/score_card.tscn);
// this script only drives the text.
public partial class Hud : CanvasLayer
{
	private Label _hud;
	private Label _center;
	private ScoreCard _scoreCard;

	public override void _Ready()
	{
		// the score card must receive input while the tree is paused on it
		ProcessMode = ProcessModeEnum.Always;
		_hud = GetNode<Label>("Hud");
		_center = GetNode<Label>("Center");
		_scoreCard = GetNode<ScoreCard>("ScoreCard");
	}

	public void SetStatus(string text) => _hud.Text = text;

	public void ShowEnd(string text)
	{
		_center.Text = text;
		_center.Visible = true;
	}

	public void ShowScoreCard(int stars, float time, string nextLevelPath = null)
	{
		_center.Visible = false;
		_hud.Visible = false;
		_scoreCard.Show(stars, time, nextLevelPath);
	}
}
