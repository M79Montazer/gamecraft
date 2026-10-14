using Godot;

// Jump pad: while the ball rests on it, Player.JumpMultiplier is raised so the
// next jump launches twice as high. Visual pulse on entry as feedback.
public partial class JumpPad : Area3D
{
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
			_mat = new StandardMaterial3D
			{
				AlbedoColor = new Color(1f, 0.45f, 0.05f),
				EmissionEnabled = true,
				Emission = new Color(1f, 0.5f, 0.1f),
				EmissionEnergyMultiplier = 2.0f,
			};
			_pad.MaterialOverride = _mat;
		}
		else
			GD.PushError($"{Name}: no MeshInstance3D under PadModel — pad won't glow");

		BodyEntered += OnBodyEntered;
		BodyExited += OnBodyExited;
	}

	private void OnBodyEntered(Node3D body)
	{
		if (body is not Player player)
			return;
		player.JumpMultiplier = 2f;

		if (_mat == null)
			return;
		var tween = CreateTween();
		tween.TweenProperty(_mat, "emission_energy_multiplier", 6.0f, 0.1f);
		tween.TweenProperty(_mat, "emission_energy_multiplier", 2.0f, 0.4f);
	}

	private void OnBodyExited(Node3D body)
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
