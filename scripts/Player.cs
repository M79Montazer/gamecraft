using Godot;

public partial class Player : CharacterBody3D
{
	[Export] public float MoveSpeed = 7f;
	[Export] public float Acceleration = 40f;
	[Export] public float AirControl = 0.5f;
	[Export] public float JumpVelocity = 8f;
	[Export] public float JumpMultiplier = 1f; // raised to 2 while standing on a JumpPad
	[Export] public float Gravity = 25f;

	private const float RollRadius = 0.4f;

	private MeshInstance3D _mesh;

	public override void _Ready()
	{
		// player.tscn instances assets/ball.glb as the BallModel child; the
		// mesh may sit one node down depending on the glb's root
		_mesh = FindFirstMesh(GetNodeOrNull("BallModel"));
		if (_mesh == null)
			GD.PushError($"{Name}: no MeshInstance3D found under BallModel — ball won't roll");
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

	public override void _PhysicsProcess(double delta)
	{
		Vector2 input = Input.GetVector("move_left", "move_right", "move_forward", "move_back");
		Vector3 dir = new Vector3(input.X, 0, input.Y);
		if (dir.LengthSquared() > 1f)
			dir = dir.Normalized();

		float accel = Acceleration * (IsOnFloor() ? 1f : AirControl);
		Vector3 horizontal = new Vector3(Velocity.X, 0, Velocity.Z).MoveToward(dir * MoveSpeed, accel * (float)delta);

		float vertical = Velocity.Y;
		if (IsOnFloor() && Input.IsActionJustPressed("jump"))
			vertical = JumpVelocity * JumpMultiplier;
		else
			vertical -= Gravity * (float)delta;

		Velocity = new Vector3(horizontal.X, vertical, horizontal.Z);
		MoveAndSlide();
		Roll((float)delta);
	}

	private void Roll(float delta)
	{
		Vector3 horizontal = new Vector3(Velocity.X, 0, Velocity.Z);
		if (horizontal.LengthSquared() < 0.01f)
			return;

		Vector3 axis = Vector3.Up.Cross(horizontal).Normalized();
		float angle = horizontal.Length() / RollRadius * delta;
		_mesh.Basis = new Basis(axis, angle) * _mesh.Basis;
		_mesh.Basis = _mesh.Basis.Orthonormalized();
	}
}
