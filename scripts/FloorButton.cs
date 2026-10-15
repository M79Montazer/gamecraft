using Godot;

// Floor pressure plate: glows amber until the robot floats over it, then locks
// pressed (sinks, turns green) and opens every Door sharing its Channel.
// Detection works both ways: the body collision or the player's tall
// FeetTrigger area (the robot hovers, so usually the trigger) entering.
public partial class FloorButton : Area3D
{
	[Export] public string Channel = "";

	private MeshInstance3D _plate;
	private StandardMaterial3D _mat;
	private bool _pressed;

	public override void _Ready()
	{
		AddToGroup("floor_button");

		_plate = GetNode<MeshInstance3D>("Plate");
		// duplicate so the pressed tint can't be undone by the shared scene
		// resource or batching; we mutate our own copy from here on
		_mat = (StandardMaterial3D)_plate.MaterialOverride.Duplicate();
		_mat.EmissionEnergyMultiplier = 2.0f;
		_plate.MaterialOverride = _mat;

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

		_mat.AlbedoColor = new Color(0.15f, 0.6f, 0.25f);
		_mat.Emission = new Color(0.3f, 1f, 0.45f);
		_mat.EmissionEnergyMultiplier = 3.0f;

		var tween = CreateTween();
		tween.TweenProperty(_plate, "position:y", -0.06f, 0.15f);
		tween.TweenCallback(Callable.From(OpenDoors));
	}

	private void OpenDoors()
	{
		foreach (Node node in GetTree().GetNodesInGroup("sliding_door"))
			if (node is Door door && door.Channel == Channel)
				door.Open();
	}
}
