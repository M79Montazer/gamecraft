using Godot;

// Jump pad: while the robot hovers over it, Player.JumpMultiplier is raised so
// the next jump launches twice as high. Visual pulse on entry as feedback.
// The robot's body floats too high to touch the pad, so its tall FeetTrigger
// area (group "player_feet") is what normally trips entry/exit.
public partial class JumpPad : Area3D
{
	// how much higher the jump goes while hovering this pad (1 = normal jump)
	[Export] public float JumpMultiplier { get; set; } = 2f;

	private MeshInstance3D _pad;
	private StandardMaterial3D _mat;

	public override void _Ready()
	{
		AddToGroup("jump_pad");

		// the glb root is a plain Node3D; the material must go on the mesh
		// instance inside it, in our own copy so the pulse stays per-pad
		_pad = FindFirstMesh(GetNodeOrNull("PadModel"));
		if (_pad != null)
		{
			// duplicate the glTF's own material and add glow on top, so the
			// model keeps its look instead of being flattened by an override
			_mat = _pad.GetActiveMaterial(0) as StandardMaterial3D;
			if (_mat != null)
				_mat = (StandardMaterial3D)_mat.Duplicate();
			else
				_mat = new StandardMaterial3D { AlbedoColor = new Color(1f, 0.45f, 0.05f) };
			_mat.EmissionEnabled = true;
			_mat.Emission = new Color(1f, 0.5f, 0.1f);
			_mat.EmissionEnergyMultiplier = 2.0f;
			_pad.SetSurfaceOverrideMaterial(0, _mat);
		}
		//else
		//	GD.PushWarning($"{Name}: no MeshInstance3D under PadModel — pad won't glow");

		BodyEntered += OnEntered;
		BodyExited += OnExited;
		AreaEntered += OnAreaEntered;
		AreaExited += OnAreaExited;
	}

	private void OnAreaEntered(Area3D area)
	{
		if (area.IsInGroup("player_feet"))
			OnEntered(area.GetParent() as Player);
	}

	private void OnAreaExited(Area3D area)
	{
		if (area.IsInGroup("player_feet"))
			OnExited(area.GetParent() as Player);
	}

	private void OnEntered(Node3D body)
	{
		if (body is not Player player)
			return;
		player.JumpMultiplier = JumpMultiplier;

		if (_mat == null)
			return;
		var tween = CreateTween();
		tween.TweenProperty(_mat, "emission_energy_multiplier", 6.0f, 0.1f);
		tween.TweenProperty(_mat, "emission_energy_multiplier", 2.0f, 0.4f);
	}

	private void OnExited(Node3D body)
	{
		if (body is Player player)
			player.JumpMultiplier = 1f;
	}

	private static MeshInstance3D FindFirstMesh(Node node)
	{
		if (node == null)
			return null;
		if (node is MeshInstance3D mi && mi.Mesh != null)
			return mi;
		foreach (Node child in node.GetChildren())
		{
			MeshInstance3D found = FindFirstMesh(child);
			if (found != null)
				return found;
		}
		return null;
	}
}
