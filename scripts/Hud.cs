using Godot;

// In-game HUD layer: status line, end-of-run overlay and control hints.
// Visuals live in scenes/ui/hud.tscn; this script only drives the text.
public partial class Hud : CanvasLayer
{
    private Label _hud;
    private Label _center;

    public override void _Ready()
    {
        _hud = GetNode<Label>("Hud");
        _center = GetNode<Label>("Center");
    }

    public void SetStatus(string text) => _hud.Text = text;

    public void ShowEnd(string text)
    {
        _center.Text = text;
        _center.Visible = true;
    }
}
