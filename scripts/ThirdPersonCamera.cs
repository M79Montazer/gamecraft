using Godot;

// Mouse-orbited third-person camera: smoothly follows its target (the Player)
// while mouse motion orbits it around the target's yaw/pitch. Only reacts to
// the mouse while it is the Current camera; a short ray keeps it from clipping
// into GridMap blocks between the pivot and the desired position.
public partial class ThirdPersonCamera : Camera3D
{
	[Export] public NodePath TargetPath = "../Player";
	[Export] public float Distance = 7f;
	[Export] public float Height = 2.5f;   // pivot lifted above the target
	[Export] public float MouseSensitivity = 0.003f;
	[Export] public float MinPitch = -1.2f; // radians, looking down
	[Export] public float MaxPitch = -0.1f; // radians, near-horizontal
	[Export] public float FollowSpeed = 12f;

	private Node3D _target;
	private float _yaw;
	private float _pitch = -0.6f;

	public override void _Ready()
	{
		_target = GetNodeOrNull<Node3D>(TargetPath);
		if (_target == null)
			GD.PushWarning("ThirdPersonCamera: target not found, camera will stay put");
	}

	public override void _UnhandledInput(InputEvent e)
	{
		if (!Current || e is not InputEventMouseMotion motion)
			return;
		_yaw -= motion.Relative.X * MouseSensitivity;
		_pitch = Mathf.Clamp(_pitch - motion.Relative.Y * MouseSensitivity, MinPitch, MaxPitch);
	}

	public override void _PhysicsProcess(double delta)
	{
		if (_target == null)
			return;
		float dt = (float)delta;
		Vector3 pivot = _target.GlobalPosition + new Vector3(0, Height, 0);
		Quaternion rot = new Quaternion(Vector3.Up, _yaw) * new Quaternion(Vector3.Right, _pitch);
		Vector3 desired = pivot + rot * new Vector3(0, 0, Distance);

		// pull in along the view line when a block sits between pivot and camera
		var query = PhysicsRayQueryParameters3D.Create(pivot, desired);
		if (_target is CollisionObject3D body)
			query.Exclude = new Godot.Collections.Array<Rid> { body.GetRid() };
		var hit = GetWorld3D().DirectSpaceState.IntersectRay(query);
		if (hit.Count > 0)
			desired = (Vector3)hit["position"];

		GlobalPosition = GlobalPosition.Lerp(desired, 1f - Mathf.Exp(-FollowSpeed * dt));
		LookAt(pivot);
	}
}
