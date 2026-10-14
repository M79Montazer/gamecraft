# Level 1

First playable: three concentric rings, escape outward before the nothingness
circle hollows the floor under you.

## Layout (painted by `scripts/BakeLevel.cs`)

- Floor disc: radius 22.5 blocks around the origin, block size 1.
- Ring walls at radii **8 / 14 / 20** (`Level.Rings`), wall band 0.6 wide.
- Gates (deg, per ring): `90, 240` · `0, 150` · `60, 200, 300`, half-width 1.5,
  each marked by a 2-high emissive beacon pair.
- Two ~70° spans per ring are 2-high walls (not jumpable); the rest is 1-high.

## Scene contents

`level_1.tscn` is fully self-contained: LevelRunner (root, game state + HUD),
WorldEnvironment, Sun, GridMap (all geometry, `assets/blocks.tres` library),
Player (instance of `scenes/player.tscn`: script + sphere collision + ball.glb
model), and a fixed Camera3D aimed at the arena center — it does not follow the
ball; it is placed so the whole disc fits its 55° fov. The baker rewrites this
file — hand edits in the editor are lost on re-bake.

## Re-bake

```
dotnet build
godot --headless --path . --script res://scripts/BakeLevel.cs
```

## Adding a level

Copy the pattern: `scenes/levels/level_N/level_N.tscn`. The selector scans that
folder at runtime and lists every `.tscn` it finds (numeric sort). Simplest
route for a hand-made variant: duplicate this scene and repaint the GridMap in
the editor; bake again only if you want the generated layout.
