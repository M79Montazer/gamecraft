using Godot;

// The player is a floating robot: a CharacterBody3D whose core sphere never
// touches the ground — a downward GroundRay holds it at HoverHeight above
// whatever is below (spring + damping), so it glides over the floor and falls
// into hollowed holes. Because the body collision sits too high to reach the
// floor props, a tall FeetTrigger Area3D (long in Y, reaching the floor)
// overlaps FloorButtons and JumpPads instead.
public partial class Player : CharacterBody3D
{
	[Export] public float MoveSpeed = 7f;
	[Export] public float Acceleration = 40f;
	[Export] public float JumpVelocity = 8f;
	[Export] public float JumpMultiplier = 1f; // raised to 2 while above a JumpPad
	[Export] public float Gravity = 25f;
	[Export] public float HoverHeight = 0.95f; // core height above the ground
	[Export] public float HoverSpring = 60f;   // height error -> upward accel
	[Export] public float HoverDamping = 14f;  // vertical speed damping while hovering

	private RayCast3D _groundRay;
	private Node3D _modelPivot;
	private float _time;
	private float _yaw;
	// true from the moment of a jump until the hover spring takes over again,
	// so gravity (not the spring) shapes the jump arc like it did for the ball
	private bool _airborne;

	public override void _Ready()
	{
		_groundRay = GetNode<RayCast3D>("GroundRay");
		_modelPivot = GetNode<Node3D>("ModelPivot");

		// the fbx carries a leftover camera from the DCC; it must never render
		foreach (Node child in _modelPivot.FindChildren("*", "Camera3D", true, false))
			child.QueueFree();
	}

	public override void _PhysicsProcess(double delta)
	{
		float dt = (float)delta;
		_time += dt;

		Vector2 input = Input.GetVector("move_left", "move_right", "move_forward", "move_back");
		Vector3 dir = CameraRelativeDir(input);
		if (dir.LengthSquared() > 1f)
			dir = dir.Normalized();

		Vector3 horizontal = new Vector3(Velocity.X, 0, Velocity.Z)
			.MoveToward(dir * MoveSpeed, Acceleration * dt);

		bool hovering = IsHovering();
		float vertical = Velocity.Y;
		if (_airborne)
		{
			vertical -= Gravity * dt;
			// falling back through hover height ends the airborne arc
			if (vertical < 0f && hovering)
				_airborne = false;
		}
		else if (hovering)
		{
			if (Input.IsActionJustPressed("jump"))
			{
				_airborne = true;
				vertical = JumpVelocity * JumpMultiplier;
			}
			else
			{
				// acceleration form (not direct velocity) — the explicit
				// velocity form goes unstable at 60 fps physics
				float heightErr = HoverHeight - HeightAboveGround();
				vertical += (heightErr * HoverSpring - Velocity.Y * HoverDamping) * dt;
			}
		}
		else
		{
			// nothing below (hollowed edge): fall into the void
			vertical -= Gravity * dt;
		}

		Velocity = new Vector3(horizontal.X, vertical, horizontal.Z);
		MoveAndSlide();
		Animate(dt, horizontal);
	}

	// WASD relative to whichever camera is current: forward is the camera's
	// flattened view direction. For the fixed overview camera (yaw 0) this
	// reduces to the old world-axis mapping.
	private Vector3 CameraRelativeDir(Vector2 input)
	{
		var cam = GetViewport().GetCamera3D();
		if (cam == null)
			return new Vector3(input.X, 0, input.Y);
		Basis b = cam.GlobalTransform.Basis;
		Vector3 forward = new Vector3(-b.Z.X, 0, -b.Z.Z);
		Vector3 right = new Vector3(b.X.X, 0, b.X.Z);
		return right * input.X + forward * -input.Y;
	}

	private bool IsHovering()
	{
		return _groundRay.IsColliding() && HeightAboveGround() < HoverHeight + 0.3f;
	}

	private float HeightAboveGround()
	{
		return GlobalPosition.Y - _groundRay.GetCollisionPoint().Y;
	}

	// Graceful idle float: gentle bob and slow tilt wobble; while moving, the
	// robot smoothly turns to face its travel direction and leans into it.
	private void Animate(float dt, Vector3 horizontal)
	{
		float speed = horizontal.Length();
		if (speed > 0.5f)
		{
			// +PI/2 because the fbx model faces -X at yaw 0, not -Z
			float targetYaw = Mathf.Atan2(horizontal.X, horizontal.Z) + Mathf.Pi / 2f;
			_yaw = Mathf.LerpAngle(_yaw, targetYaw, 1f - Mathf.Exp(-6f * dt));
		}

		float bob = Mathf.Sin(_time * 2.4f) * 0.06f;
		float lean = Mathf.Clamp(speed * 0.02f, 0f, 0.15f);
		_modelPivot.Position = new Vector3(0, bob, 0);
		_modelPivot.Rotation = new Vector3(
			Mathf.Cos(_time * 1.9f) * 0.03f + lean,
			_yaw,
			Mathf.Sin(_time * 1.7f) * 0.03f);
	}
}
