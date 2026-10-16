# Level 1

First playable: three concentric rings, escape outward before the nothingness
circle hollows the floor under you.

## Layout

- Floor disc: radius 22.5 blocks around the origin, block size 1.
- Ring walls at radii **8 / 14 / 20** (the `Rings` export on LevelRunner),
  wall band 0.6 wide.
- Gates (deg, per ring): `90, 240` · `0, 150` · `60, 200, 300`, half-width 1.5,
  each marked by a 2-high emissive beacon pair.
- Two ~70° spans per ring are 2-high walls (not jumpable); the rest is 1-high.

## Scene contents

`level_1.tscn` is fully self-contained: LevelRunner (root, game state + HUD,
per-level `FloorRadius` / `Rings` exports), WorldEnvironment, Sun, GridMap
(all geometry, `assets/blocks.tres` library), Player (instance of
`scenes/player.tscn`: script + sphere collision + ball.glb model), and a fixed
Camera3D aimed at the arena center — it does not follow the ball; it is placed
so the whole disc fits its 55° fov.

## Editing

This scene is hand-edited in the Godot editor like any other scene — there is
no generator script anymore. Select the GridMap node and use the GridMap paint
tool; palette ids come from `assets/blocks.tres` (`BlockItem` in Level.cs):
**0** floor · **1** wall · **2** beacon · **3** hollow (pre-hollowed shell).

## Adding a level

Duplicate `scenes/levels/level_template.tscn` (root + GridMap wired to
`assets/blocks.tres` + Player + Camera), paint the GridMap, drop in doors /
buttons / jump pads as needed, and save as
`scenes/levels/<name>/<name>.tscn`. The selector scans `scenes/levels/*/` at
runtime and lists every `.tscn` it finds. Tune `FloorRadius` (win/escape
distance from the origin) and `Rings` (HUD zone boundaries) on the LevelRunner
root in the inspector to match your layout.

### Doors, buttons and switches

- `scenes/door.tscn` — sliding gate. Set `Channel`; a door opens (and stays
  open) when any floor button with that channel is pressed.
- `scenes/floor_button.tscn` — one-use pressure plate. Set `Channel` to open
  every door sharing it.
- `scenes/switch_button.tscn` — re-usable toggle lever. Walking into it flips
  the lever and **toggles** every door in both of its channels, `ChannelA` and
  `ChannelB` (open doors close back up, closed ones open). Other exports:
  `CooldownSeconds` (time before it can trigger again, default 2s). Give it
  two channels, put one door on each, and put a floor button on the first
  channel for the open-one/close-the-other puzzle pattern. Its placeholder
  models are `assets/switch_lever_left.glb` / `switch_lever_right.glb` (swap
  the `LeverLeft` / `LeverRight` instances when the real asset lands).
