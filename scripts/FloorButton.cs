using Godot;

// Floor pressure plate: shows the red model until the robot floats over it,
// then locks pressed (swaps to the green model, sinks) and opens every Door
// sharing its Channel. Models are button_red.fbx / button_green.fbx, instanced
// as sibling nodes in scenes/floor_button.tscn. Detection works both ways: the
// body collision or the player's tall FeetTrigger area (the robot hovers, so
// usually the trigger) entering.
public partial class FloorButton : Area3D
{
	[Export] public string Channel = "";

	private Node3D _redModel;
	private Node3D _greenModel;
	private bool _pressed;

	public override void _Ready()
	{
		AddToGroup("floor_button");

		_redModel = GetNode<Node3D>("RedModel");
		_greenModel = GetNode<Node3D>("GreenModel");

		// the body filter matters: the plate's collision box slightly penetrates
		// the GridMap floor, so the floor body itself fires BodyEntered at the
		// first physics frame — without the filter every door opens on load
		BodyEntered += OnBodyEntered;
		AreaEntered += OnAreaEntered;
	}

	private void OnBodyEntered(Node3D body)
	{
		if (body is Player)
			OnPlayer(body);
	}

	private void OnAreaEntered(Area3D area)
	{
		if (area.IsInGroup("player_feet"))
			OnPlayer(area);
	}

	private void OnPlayer(Node3D _)
	{
		if (_pressed)
			return;
		_pressed = true;

		_redModel.Visible = false;
		_greenModel.Visible = true;

		var tween = CreateTween();
		tween.TweenProperty(_greenModel, "position:y", -0.06f, 0.15f);
		tween.TweenCallback(Callable.From(OpenDoors));
	}

	private void OpenDoors()
	{
		foreach (Node node in GetTree().GetNodesInGroup("sliding_door"))
			if (node is Door door && door.Channel == Channel)
				door.Open();
	}
}
