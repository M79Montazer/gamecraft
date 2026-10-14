using Godot;

// Floor pressure plate: glows amber until the ball rolls onto it, then locks
// pressed (sinks, turns green) and opens every Door sharing its Channel.
public partial class FloorButton : Area3D
{
	[Export] public string Channel = "";

	private const float PlateSize = 0.8f;
	private const float PlateHeight = 0.14f;

	private MeshInstance3D _plate;
	private StandardMaterial3D _mat;
	private bool _pressed;

	public override void _Ready()
	{
		AddToGroup("floor_button");

		// visuals are built at runtime, not baked — a material swap in the
		// baker's process wouldn't survive into the saved scene
		_mat = new StandardMaterial3D
		{
			AlbedoColor = new Color(0.55f, 0.35f, 0.05f),
			EmissionEnabled = true,
			Emission = new Color(1f, 0.65f, 0.1f),
			EmissionEnergyMultiplier = 2f,
		};

		AddChild(new CollisionShape3D
		{
			Name = "Shape",
			Shape = new BoxShape3D { Size = new Vector3(0.95f, 0.4f, 0.95f) },
			Position = new Vector3(0, 0.15f, 0),
		});

		_plate = new MeshInstance3D
		{
			Name = "Plate",
			Mesh = new BoxMesh { Size = new Vector3(PlateSize, PlateHeight, PlateSize) },
			MaterialOverride = _mat,
		};
		AddChild(_plate);

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
