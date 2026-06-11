# Level Format Specification (V1)

**Schema Version:** 1
**Status:** Stable — implemented in LevelLoader, GridService, LevelValidator
**Author:** Level/Content Agent

---

## Overview

Levels in Dungeon VR are **data, not scenes**. A level file describes a grid of tiles, each with a type, optional wall faces, and metadata (spawns, items, triggers). The runtime reads this data and constructs the world.

- **V0:** Hardcoded 5x5 test grid in `TestGridService.cs`. No file serialization.
- **V1:** JSON-based pipeline. Level files live under `Assets/Data/Levels/` and load via `LevelLoader` using `TilePalette` ScriptableObject.

---

## Grid Coordinate Convention

- **(0,0)** is the origin tile at world position **(0, 0, 0)**.
- **X-axis:** Right (positive X = east).
- **Z-axis:** Forward (positive Z = north, per Unity convention).
- **Y-axis:** Up (Y=0 is floor level).
- All tiles are **3m × 3m** (`GameConstants.TILE_SIZE = 3.0f`).
- Tile **pivot is at center-bottom** of the tile.
- Tile **(x, z)** occupies world space from `(x * 3, 0, z * 3)` to `(x * 3 + 3, 0, z * 3 + 3)`.
- Its **center** is at `(x * 3 + 1.5, FloorHeight, z * 3 + 1.5)`.

---

## TileType Enum

| Value    | Description                                       |
|----------|---------------------------------------------------|
| `Floor`  | Walkable ground tile. Default.                    |
| `Wall`   | Impassable obstacle. Occupies the full tile.      |
| `Door`   | Walkable when open, blocks when closed.           |
| `Trap`   | Walkable; triggers an effect when stepped on.     |
| `Altar`  | Walkable; interaction point (save, heal, etc.).   |
| `Spawn`  | Player/champion spawn point. Exactly one per map. |
| `Stairs` | Walkable; transitions to the next/previous floor. |
| `Empty`  | Void tile outside the grid bounds (no geometry).  |

---

## WallFace Enum

Describes which edges of a tile have wall geometry. Used for interior walls, room boundaries, and door frames.

| Value   | Description                        |
|---------|------------------------------------|
| `None`  | No wall on any face (open air).    |
| `North` | Wall on the +Z face.               |
| `South` | Wall on the -Z face.               |
| `East`  | Wall on the +X face.               |
| `West`  | Wall on the -X face.               |
| `All`   | Wall on all four faces (pillar).   |

WallFace is a [Flags] enum. Values can be combined via comma separation in JSON: `"north,east"` produces a wall on both the north and east faces.

---

## TileData Struct (Schema Version 1)

Each tile in the grid is described by:

| Field         | Type        | Description                                    |
|---------------|-------------|------------------------------------------------|
| `X`           | `int`       | Grid column index (0-based).                   |
| `Z`           | `int`       | Grid row index (0-based).                      |
| `Type`        | `TileType`  | The tile's primary classification.             |
| `WallFaces`   | `WallFace`  | Which edges have wall geometry.                |
| `FloorHeight` | `float`     | Y-offset for non-flat floors (elevation, V2+). |
| `Tags`        | `string[]`  | Arbitrary tags for gameplay systems.           |
| `Metadata`    | `string`    | JSON blob for extensible per-tile data.        |

---

## JSON Structure (Schema Version 1)

```json
{
  "schemaVersion": 1,
  "levelName": "Hall of Champions",
  "width": 32,
  "depth": 32,
  "tiles": [
    {
      "x": 0,
      "z": 0,
      "type": "wall",
      "wallFaces": "east,south"
    },
    {
      "x": 3,
      "z": 3,
      "type": "spawn"
    },
    {
      "x": 22,
      "z": 29,
      "type": "stairs"
    }
  ],
  "defaultTile": {
    "type": "floor"
  }
}
```

### Field Descriptions

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `schemaVersion` | `int` | Yes | Must be `1`. Rejected otherwise. |
| `levelName` | `string` | No | Display name for the level. |
| `width` | `int` | Yes | Grid width in tiles. Must be positive. |
| `depth` | `int` | Yes | Grid depth in tiles. Must be positive. |
| `tiles` | `array` | No | Explicit tile overrides. Omitted or empty = all tiles are default. |
| `defaultTile` | `object` | No | Default tile settings. `type` defaults to `"floor"` if omitted. |

### Per-Tile Fields

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `x` | `int` | Yes | Column index (0..width-1). |
| `z` | `int` | Yes | Row index (0..depth-1). |
| `type` | `string` | Yes | Tile type name (lowercase). One of: `floor`, `wall`, `door`, `trap`, `altar`, `spawn`, `stairs`, `empty`. |
| `wallFaces` | `string` | No | Wall face mask (lowercase). Single: `"north"`, `"south"`, `"east"`, `"west"`, `"all"`. Combined: `"north,east"`. |
| `floorHeight` | `float` | No | Y-offset from ground level (default: 0). |

### Default Tile Behavior

Any coordinate not explicitly listed in `tiles` is filled with the `defaultTile` specification. If `defaultTile` is also omitted, all unspecified coordinates become `Floor` tiles. This keeps the file compact for mostly-open grids.

### Coordinate Ordering

Tiles in the `tiles` array can appear in any order. If the same coordinate appears twice, the last occurrence wins (a warning is logged).

---

## Validation Rules (LevelValidator)

When a level is loaded, the `LevelValidator` performs these checks:

### Required Checks
1. **Schema version** must be `1`
2. **Grid dimensions** must be positive
3. **Exactly 1 Spawn tile** must exist
4. **At least 1 Stairs tile** must exist
5. **No overlapping coordinates** — duplicate tiles flagged as warnings
6. **All tiles within bounds** — coordinates must be 0..width-1, 0..depth-1
7. **Palette completeness** — all non-Empty TileType values must have prefabs assigned
8. **Prefab availability** — every tile type used in the level must exist in the palette

### Solvability Check
BFS flood fill from the Spawn tile through all walkable tiles:
- At least one Stairs tile must be reachable
- All walkable tiles should ideally be reachable (warnings for orphaned areas)

### Walkable Tiles
The following TileTypes are considered walkable for validation purposes: Floor, Door, Trap, Altar, Spawn, Stairs
The following TileTypes block movement: Wall, Empty

---

## Tile Palette (ScriptableObject)

The `TilePalette` ScriptableObject (`DungeonVR.Level.Data.TilePalette`) maps each TileType to a Unity prefab:

| TileType | Prefab Slot | Required |
|----------|-------------|----------|
| Floor    | `_floorPrefab` | Yes |
| Wall     | `_wallPrefab` | Yes |
| Door     | `_doorPrefab` | Yes |
| Trap     | `_trapPrefab` | Yes |
| Altar    | `_altarPrefab` | Yes |
| Spawn    | `_spawnPrefab` | Yes |
| Stairs   | `_stairsPrefab` | Yes |
| Empty    | (none) | No — void tile has no geometry |

Create via: Assets → Create → DungeonVR → Tile Palette

---

## Level Loading Pipeline

```
JSON TextAsset
    ↓ JsonUtility.Deserialize
LevelDataJson (schema v1)
    ↓ BuildTileData (fill defaults)
TileData[]
    ↓ LevelValidator.Validate
    ↓ LevelValidator.IsSolvable
Validated TileData[]
    ↓ LevelLoader.InstantiateTiles (using TilePalette)
GameObjects in scene
    ↓ GridService.RegisterFromTiles
Runtime grid queries
    ↓ LevelLoaded event fired
Gameplay systems begin
```

### Key Components

| Component | File | Role |
|-----------|------|------|
| `TilePalette` | `Assets/Scripts/Level/Data/TilePalette.cs` | ScriptableObject asset mapping TileType → prefab |
| `LevelLoader` | `Assets/Scripts/Level/Components/LevelLoader.cs` | MonoBehaviour: deserialize, validate, instantiate, register |
| `GridService` | `Assets/Scripts/Level/Logic/GridService.cs` | Runtime grid queries, O(1) IsWalkable, spawn point registration |
| `LevelValidator` | `Assets/Scripts/Level/Logic/LevelValidator.cs` | Validation + BFS solvability check |
| `LevelEditorWindow` | `Assets/Editor/LevelEditor/LevelEditorWindow.cs` | Editor tool for designing levels |

---

## Tile Prefab Requirements

All tile prefabs must adhere to these rules:

| Requirement        | Value                                |
|--------------------|--------------------------------------|
| Tile footprint     | 3m × 3m (matches `TILE_SIZE`)        |
| Pivot position     | Center-bottom of the tile            |
| Floor level        | Y = 0                                |
| Wall height        | 3m (one tile height)                 |
| Collision          | Mesh collider or box collider        |
| Layer              | `Environment`                        |

---

## Performance Targets (V1)

| Metric | Target | Measured By |
|--------|--------|-------------|
| Floor load time (32×32) | < 1 second | PlayMode benchmark |
| GridService.IsWalkable() | O(1), zero allocation | Code review + test |
| GridService.GetTileCenter() | O(1), zero allocation | Code review + test |
| Editor tool frame rate (32×32 grid) | Smooth | Manual test |

---

## Future Considerations (V2+)

- **Multi-floor levels:** A `floors` array, each with its own grid and tileset.
- **Room templates:** Pre-authored room blueprints that can be stamped into the grid.
- **Procedural generation:** `TileType` probability tables and room-packing algorithms.
- **Streaming:** Large levels load/unload chunks based on player position.
- **Metadata field:** Per-tile JSON blobs for spawn configs, item drops, trigger parameters.
