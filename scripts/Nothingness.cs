using Godot;
using System.Collections.Generic;

public partial class Nothingness : Node3D
{
    public Level Level;
    public bool Active = true;

    [Export] public float StartRadius = 1.5f;
    [Export] public float GrowthSpeed = 0.9f;
    [Export] public float RingSpeedup = 1.15f;
    [Export] public float FuseTime = 3.0f;
    [Export] public float HollowBand = 2.0f;

    private float _radius;
    private float _time;
    private float _speed;
    private int _blocksPtr;
    private int _ringsCrossed;
    private readonly List<Block> _pending = new();

    private MeshInstance3D _disc;
    private MeshInstance3D _edge;

    public override void _Ready()
    {
        _radius = StartRadius;
        _speed = GrowthSpeed;

        var discMat = new StandardMaterial3D
        {
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
            AlbedoColor = new Color(0.01f, 0.005f, 0.03f),
        };
        _disc = new MeshInstance3D
        {
            Mesh = new CylinderMesh { TopRadius = 1f, BottomRadius = 1f, Height = 0.05f, RadialSegments = 64 },
            MaterialOverride = discMat,
            Position = new Vector3(0, 1.03f, 0),
            Scale = new Vector3(_radius, 1, _radius),
        };
        AddChild(_disc);

        var edgeMat = new StandardMaterial3D
        {
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
            AlbedoColor = new Color(0.6f, 1f, 1f),
            EmissionEnabled = true,
            Emission = new Color(0.35f, 0.95f, 1f),
            EmissionEnergyMultiplier = 4f,
        };
        _edge = new MeshInstance3D
        {
            Mesh = new TorusMesh { InnerRadius = 0.96f, OuterRadius = 1.0f },
            MaterialOverride = edgeMat,
            Position = new Vector3(0, 1.06f, 0),
            Scale = new Vector3(_radius, 1, _radius),
        };
        AddChild(_edge);
    }

    public override void _Process(double delta)
    {
        if (!Active)
            return;

        float dt = (float)delta;
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
                {
                    b.MeshInstance.Mesh = GameAssets.Hollow;
                    b.MeshInstance.MaterialOverride = null;
                    b.State = Block.BlockState.Hollow;
                }
            }
            else if (_time >= b.DestroyAt)
            {
                b.Body.QueueFree();
                b.State = Block.BlockState.Gone;
                _pending.RemoveAt(i);
            }
        }

        // the void disc lags behind the hollowing front so the crumbling
        // wireframe blocks stay visible in a band behind the glowing rim
        float discRadius = Mathf.Max(0.01f, _radius - HollowBand);
        _disc.Scale = new Vector3(discRadius, 1, discRadius);
        _edge.Scale = new Vector3(discRadius, 1, discRadius);
    }
}
