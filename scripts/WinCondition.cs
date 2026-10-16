using Godot;

// Escape trigger: an invisible Area3D placed by hand in level scenes. Entering
// it as the player raises Entered, which LevelRunner turns into a win. Shape
// and transform are authored per instance in the editor; levels may hold any
// number of them.
public partial class WinCondition : Area3D
{
	[Signal] public delegate void EnteredEventHandler(WinCondition winCondition);

	public override void _Ready()
	{
		AddToGroup("win_condition");
		BodyEntered += OnBodyEntered;
	}

	private void OnBodyEntered(Node3D body)
	{
		if (body is Player)
			EmitSignal(SignalName.Entered, this);
	}
}
