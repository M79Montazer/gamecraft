using Godot;

// Toggle lever: every time the robot walks into it, it flips (swaps the left /
// right lever models) and toggles every Door in either of its two channels
// (ChannelA / ChannelB) — doors that were open close, closed ones open. Re-triggerable, but only after
// CooldownSeconds have passed since the last toggle. Models are
// switch_lever_left.glb / switch_lever_right.glb, instanced as sibling nodes
// in scenes/switch_button.tscn (placeholder until the artist's real asset).
// Detection works both ways: the body collision or the player's tall
// FeetTrigger area (the robot hovers, so usually the trigger) entering.
public partial class SwitchButton : Area3D
{
	[Export] public string ChannelA = "";

	[Export] public string ChannelB = "";

	[Export] public float CooldownSeconds = 2.0f;

	private Node3D _leverLeft;
	private Node3D _leverRight;
	private bool _leftIsUp = true;
	private ulong _readyAtTicks;

	public override void _Ready()
	{
		AddToGroup("switch_button");

		_leverLeft = GetNode<Node3D>("LeverLeft");
		_leverRight = GetNode<Node3D>("LeverRight");

		// the body filter matters: without it the GridMap floor itself can fire
		// BodyEntered (see FloorButton) — here it would toggle doors on load
		BodyEntered += OnBodyEntered;
		AreaEntered += OnAreaEntered;
	}

	private void OnBodyEntered(Node3D body)
	{
		if (body is Player)
			OnPlayer();
	}

	private void OnAreaEntered(Area3D area)
	{
		if (area.IsInGroup("player_feet"))
			OnPlayer();
	}

	private void OnPlayer()
	{
		ulong now = Time.GetTicksMsec();
		if (now < _readyAtTicks)
			return;
		_readyAtTicks = now + (ulong)(CooldownSeconds * 1000.0f);

		_leftIsUp = !_leftIsUp;
		_leverLeft.Visible = _leftIsUp;
		_leverRight.Visible = !_leftIsUp;

		foreach (Node node in GetTree().GetNodesInGroup("sliding_door"))
			if (node is Door door && (door.Channel == ChannelA || door.Channel == ChannelB))
				door.Toggle();
	}
}
