# Hollow3d — AGENTS.md

Godot 4.7.2 (Mono/C#) 2.5D puzzle game, game-jam theme "Hollow". A ball escapes
outward across a disc of GridMap blocks while a growing "nothingness" circle
turns blocks into wireframes, then deletes them. WASD + Space, fixed angled
top-down camera.

## Commands

```sh
dotnet build                                                # compile C#
```
## Architecture

- **Scene flow**: `scenes/main.tscn` (MainMenu) → `scenes/ui/level_select.tscn`
  (scans `scenes/levels/*/` at runtime via DirAccess, one button per `.tscn`)
  → level scene. Esc goes back; R restarts a level.
- **Levels**: `scenes/levels/<name>/<name>.tscn` are fully self-contained
  (LevelRunner root, WorldEnvironment, Sun, GridMap, Player instance, fixed
  Camera3D). Levels are **hand-authored in the Godot editor** — no generator
  script. Duplicate `scenes/levels/level_template.tscn`, paint the GridMap,
  save as `<name>.tscn`; tune `FloorRadius` (win/escape distance from origin)
  and `Rings` (HUD zones) exports on the LevelRunner root per level.
  `scenes/levels/level_1/level_1.md` documents layout + how to add levels.
- **GridMap**: level geometry lives in one or more GridMap nodes (e.g.
  `GridMap` with `assets/blocks.tres`: 0 floor, 1 wall, 2 beacon, 3 hollow;
  ids in `BlockItem`; level_1 also has `GridMap3` with
  `assets/walls_n_doors/v4/walls3.tres`). LevelRunner collects **every**
  GridMap child of the level root for hollowing/destruction = `SetCellItem`
  swaps (Level.cs wraps this). Each library's solid→hollow mapping is resolved
  by item-name convention: `hollow_X` is the hollow variant of `X`, or a plain
  `hollow` item is the universal hollow for the whole library — new libraries
  need no code changes if they follow that naming.
  MeshLibrary has no per-item material — tinted variants duplicate the mesh and
  `SurfaceSetMaterial`.
- **Player**: `scenes/player.tscn` (script + sphere collision + ball.glb
  instance) is shared by all levels; edit it there, not per level.
- **Free walls**: `scenes/walls/wall.tscn` / `half_wall.tscn` are standalone
  wall scenes for placing walls at arbitrary angles/scales (no GridMap grid).
  Each is a `StaticBody3D` with script `scripts/Wall.cs` and `Solid`/`Hollow`
  model children — the nothingness swaps Solid→Hollow then frees the wall on
  the same fuse rhythm as GridMap blocks (group `free_wall`, handled by
  `Nothingness.ConsumeWalls`). Model scale ~300 matches the GridMap
  `cell_scale`; collision boxes are rough and tuned per-instance in the editor.
- **UI** (menus, HUD): built in code, not editor scenes — keep that pattern.
- when adding a new scene or node or object, unless necessary, don't have anything generated in runtime (any Godot nodes).
  have them a node or a separate scene editable in the Godot editor 

## Gotchas

- GridMap cell mapping: with default centering, `MapToLocal(cell)` = cell +
  half cell; the map node is offset (-0.5, 0, -0.5) so cell (i,k,j) sits at
  world (i, 0.5+k, j). `Level` computes block distance via
  `ToGlobal(MapToLocal(cell))` — keep it that way if touching cell math.
- Godot C# is case-sensitive on Vector3 members (`.Z`, `.X` — not `.z`/`.x`).
- `using Godot;` + `using System;` → `Environment` is ambiguous; alias
  `using Environment = Godot.Environment;`.
- Node `_Ready` order isn't guaranteed across siblings; `GameAssets.EnsureLoaded()`
  is lazy/self-guarding for that reason — call it, don't assume Load ran.
- Renderer is `mobile` + Jolt physics (project.godot); input actions are
  registered at runtime in `LevelRunner.EnsureAction`, not in project.godot.

## MCP
- for blender MCP, Launch once per session: `"D:/Blender 5.2/blender.exe" --online-mode &`
(port 9876).

## Godot headless
- do not run a headless Godot. after a build with no errors, tell the user to check and test it in Godot themself