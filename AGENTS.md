# Hollow3d — AGENTS.md

Godot 4.7.2 (Mono/C#) 2.5D puzzle game, game-jam theme "Hollow". A ball escapes
outward across a disc of GridMap blocks while a growing "nothingness" circle
turns blocks into wireframes, then deletes them. WASD + Space, fixed angled
top-down camera.

## Commands

```sh
dotnet build                                                # compile C#
```

- Godot exe lives at `D:/Godot_v4.7.2-stable_mono_win64/` (use the `_console` exe for headless).
- Headless runs print RID/ObjectDB "leaked at exit" noise on quit — filter it, it is not a failure.
- A C# `SceneTree` script is only runnable after `dotnet build` (class must be in the assembly); build error output can be missed when piping to `grep -c`, read the full build tail.

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
- **GridMap**: all level geometry is one GridMap using `assets/blocks.tres`
  (MeshLibrary: 0 floor, 1 wall, 2 beacon, 3 hollow; ids in `BlockItem`).
  Hollowing/destruction = `SetCellItem` swaps (Level.cs wraps this).
  MeshLibrary has no per-item material — tinted variants duplicate the mesh and
  `SurfaceSetMaterial`.
- **Player**: `scenes/player.tscn` (script + sphere collision + ball.glb
  instance) is shared by all levels; edit it there, not per level.
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

##MCP
- for blender MCP, Launch once per session: `"D:/Blender 5.2/blender.exe" --online-mode &`
(port 9876).