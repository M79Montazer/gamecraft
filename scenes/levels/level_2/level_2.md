# Level 2

A near-copy of level 1 with the same three rings (8/14/20), gate layout and
star count. The two collectible stars are moved to different mid-ring spots:

- `Star_-15_12` at world (-15, 1.5, 12)
- `Star_12_-15` at world (12, 1.5, -15)

Winning level 2 unlocks level 3 via `scripts/Progress.cs` (saved in
`user://progress.cfg`). There is no `intro_dialogue.tscn` in this folder, so
the level starts playing directly.

## Adding more levels

Duplicate `scenes/levels/level_template.tscn` (or an existing level) into
`scenes/levels/<name>/<name>.tscn`, rename the root node, paint the GridMap in
the Godot editor, and tune `FloorRadius` / `Rings` on the LevelRunner root.
Levels unlock in numeric order as earlier ones are completed.
