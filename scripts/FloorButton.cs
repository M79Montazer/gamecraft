using Godot;

// Floor pressure plate: glows amber until the ball rolls onto it, then locks
// pressed (sinks, turns green) and opens every Door sharing its Channel.
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
		_mat = (StandardMaterial3D)_plate.MaterialOverride;

		BodyEntered += OnBodyEntered;
	}

	private void OnBodyEntered(Node3D body)
	{
		if (_pressed || body is not Player)
			return;
		_pressed = true;

		_mat.AlbedoColor = new Color(0.15f, 0.6f, 0.25f);
		_mat.Emission = new Color(0.3f, 1f, 0.45f);

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
