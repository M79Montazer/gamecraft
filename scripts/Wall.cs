using Godot;

// Free-placeable wall scene: same mesh pair as the walls3.tres GridMap items
// (solid + hollow twin) but placed as a standalone scene so the level designer
// can rotate/scale walls at arbitrary angles and corners. The nothingness
// circle swaps Solid for Hollow, then frees the wall, on the same fuse rhythm
// as GridMap blocks; timing bookkeeping lives in Nothingness, this node stays
// dumb.
public partial class Wall : StaticBody3D
{
    public bool Scheduled;
    public bool Hollowed;
    public float HollowAt;
    public float DestroyAt;

    public override void _Ready()
    {
        AddToGroup("free_wall");
    }

    public void MakeHollow()
    {
        if (Hollowed)
            return;
        Hollowed = true;
        GetNode<Node3D>("Solid").Visible = false;
        GetNode<Node3D>("Hollow").Visible = true;
    }
}
