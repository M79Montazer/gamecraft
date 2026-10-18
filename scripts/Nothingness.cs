using Godot;
using System.Collections.Generic;

public partial class Nothingness : Node3D
{
    public Level Level;
    public bool Active = true;

    // set by LevelRunner per level; GrowthSpeed mirrors its
    // NothingnessSpeed export, so it is not an editor knob here anymore
    public float GrowthSpeed = 0.9f;
    public float StartDelay = 0f;

    [Export] public float StartRadius = 1.5f;
    [Export] public float RingSpeedup = 1.15f;
    [Export] public float FuseTime = 3.0f;
    [Export] public float HollowBand = 2.0f;

    private float _radius;
    private float _time;
    private float _speed;
    private float _delayRemaining;
    private int _blocksPtr;
    private int _ringsCrossed;
    private readonly List<Block> _pending = new();
    private readonly List<Wall> _pendingWalls = new();

    private MeshInstance3D _disc;

    public override void _Ready()
    {
        _radius = StartRadius;
        _speed = GrowthSpeed;
        _delayRemaining = StartDelay;

        _disc = GetNode<MeshInstance3D>("Disc");
        // the void is not there yet during the grace period; the disc only
        // appears once spreading begins
        if (StartDelay > 0f)
            _disc.Visible = false;
    }

    public override void _Process(double delta)
    {
        if (!Active)
            return;

        float dt = (float)delta;

        // grace period: no growth, hollowing or consuming until it runs out
        if (_delayRemaining > 0f)
        {
            _delayRemaining -= dt;
            if (_delayRemaining > 0f)
                return;
            _disc.Visible = true;
        }

        _time += dt;
        _radius += _speed * dt;

        while (_ringsCrossed < Level.Rings.Length && _radius >= Level.Rings[_ringsCrossed])
        {
            _speed *= RingSpeedup;
            _ringsCrossed++;
        }

        // hollow everything the circle has just reached
        List<Block> blocks = Level.SortedBlocks;
        while (_blocksPtr < blocks.Count && blocks[_blocksPtr].Dist <= _radius)
        {
            Block b = blocks[_blocksPtr];
            _blocksPtr++;
            if (b.State != Block.BlockState.Solid)
                continue;
            b.HollowAt = _time + (float)GD.RandRange(0.0, 0.5);
            b.DestroyAt = b.HollowAt + FuseTime + (float)GD.RandRange(0.0, 0.3);
            _pending.Add(b);
        }

        // then destroy them once the fuse runs out
        for (int i = _pending.Count - 1; i >= 0; i--)
        {
            Block b = _pending[i];
            if (b.State == Block.BlockState.Solid)
            {
                if (_time >= b.HollowAt)
                    Level.MakeHollow(b);
            }
            else if (_time >= b.DestroyAt)
            {
                Level.Destroy(b);
                _pending.RemoveAt(i);
            }
        }

        // free-placed wall scenes (scenes/walls) are not GridMap cells but get
        // the same rhythm as blocks: swap to the hollow mesh on a short fuse,
        // then disappear
        ConsumeWalls();
        for (int i = _pendingWalls.Count - 1; i >= 0; i--)
        {
            Wall w = _pendingWalls[i];
            if (!w.Hollowed)
            {
                if (_time >= w.HollowAt)
                    w.MakeHollow();
            }
            else if (_time >= w.DestroyAt)
            {
                w.QueueFree();
                _pendingWalls.RemoveAt(i);
            }
        }

        // standalone props (doors, floor buttons, jump pads) are not GridMap
        // cells, so the circle consumes them directly: free them once the rim
        // reaches
        ConsumeProps();

        // the void disc lags behind the hollowing front so the crumbling
        // wireframe blocks stay visible in a band behind the glowing rim
        float discRadius = Mathf.Max(0.01f, _radius - HollowBand);
        _disc.Scale = new Vector3(discRadius, 1, discRadius);
    }

    private void ConsumeWalls()
    {
        Vector3 center = GlobalTransform.Origin;
        foreach (Node node in GetTree().GetNodesInGroup("free_wall"))
            if (node is Wall wall && !wall.Scheduled)
            {
                Vector3 p = wall.GlobalTransform.Origin;
                float dist = Mathf.Sqrt(
                    (p.X - center.X) * (p.X - center.X) +
                    (p.Z - center.Z) * (p.Z - center.Z));
                if (dist <= _radius)
                {
                    wall.Scheduled = true;
                    wall.HollowAt = _time + (float)GD.RandRange(0.0, 0.5);
                    wall.DestroyAt = wall.HollowAt + FuseTime + (float)GD.RandRange(0.0, 0.3);
                    _pendingWalls.Add(wall);
                }
            }
    }

    private void ConsumeProps()
    {
        Vector3 center = GlobalTransform.Origin;
        foreach (string group in new[] { "sliding_door", "floor_button", "jump_pad", "star", "switch_button", "prop" })
            foreach (Node node in GetTree().GetNodesInGroup(group))
                if (node is Node3D prop)
                {
                    Vector3 p = prop.GlobalTransform.Origin;
                    float dist = Mathf.Sqrt(
                        (p.X - center.X) * (p.X - center.X) +
                        (p.Z - center.Z) * (p.Z - center.Z));
                    if (dist <= _radius)
                        prop.QueueFree();
                }
    }
}
