# Level Format Specification (V0 Draft)

**Schema Version:** 0 (pre-release)
**Status:** Draft — subject to change in V1
**Author:** Level/Content Agent

---

## Overview

Levels in Dungeon VR are **data, not scenes**. A level file describes a grid of tiles, each with a type, optional wall faces, and metadata (spawns, items, triggers). The runtime reads this data and constructs the world.

- **V0:** Hardcoded 5x5 test grid in `TestGridService.cs`. No file serialization.
- **V1+:** JSON-based pipeline. Level files will live under `Assets/Levels/` and load via a `LevelLoader` service.

---

## Grid Coordinate Convention

- **(0,0)** is the origin tile at world position **(0, 0, 0)**.
- **X-axis:** Right (positive X = east).
- **Z-axis:** Forward (positive Z = north/south, per Unity convention).
- **Y-axis:** Up (Y=0 is floor level).
- All tiles are **3m × 3m** (`GameConstants.TILE_SIZE = 3.0f`).
- Tile **pivot is at center-bottom** of the tile.
- Tile **(x, z)** occupies world space from `(x * 3, 0, z * 3)` to `(x * 3 + 3, 0, z * 3 + 3)`.
- Its **center** is at `(x * 3 + 1.5, 0, z * 3 + 1.5)`.

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

> **V0 note:** Walls are handled as full-tile blockers via the `bool[,] Walls` array. Per-face wall rendering comes in V1 when level data supports `TileData.WallFaces`.

---

## TileData Struct (V1 Target)

Each tile in the grid is described by:

| Field         | Type        | Description                                    |
|---------------|-------------|------------------------------------------------|
| `Type`        | `TileType`  | The tile's primary classification.             |
| `WallFaces`   | `WallFace`  | Which edges have wall geometry.                |
| `FloorHeight` | `float`     | Y-offset for non-flat floors (elevation).      |
| `Tags`        | `string[]`  | Arbitrary tags for gameplay systems.           |
| `Metadata`    | `string`    | JSON blob for extensible per-tile data.        |

---

## Proposed JSON Structure (V1+)

```json
{
  "schemaVersion": 1,
  "levelName": "Hall of Champions",
  "width": 5,
  "depth": 5,
  "tiles": [
    {
      "x": 0,
      "z": 0,
      "type": "wall",
      "wallFaces": "all"
    },
    {
      "x": 2,
      "z": 2,
      "type": "spawn"
    },
    {
      "x": 3,
      "z": 3,
      "type": "stairs"
    }
  ],
  "defaultTile": {
    "type": "floor"
  }
}
```

> `defaultTile` fills any coordinate not explicitly listed. This keeps the file compact for mostly-open grids.

---

## V0 Hardcoded Grid

The V0 test grid (built in `TestGridService.BuildDefaultGrid()`) is:

```
Legend:  W = Wall (blocked),  . = Floor (walkable)

x:  0 1 2 3 4
z  +----------
0  | W W W W W
1  | W . . . W
2  | W . . . W
3  | W . . . W
4  | W W W W W
```

- **Dimensions:** 5 × 5 tiles
- **Perimeter:** All tiles on x=0, x=4, z=0, z=4 are walls
- **Interior:** A 3×3 open area at (1..3, 1..3) is walkable
- **Spawn:** The champion spawns at (2, 2) — center of the open area
- **Validation:** Bounds check in `IsWalkable()` rejects coordinates outside [0..4]

---

## Tile Prefab Requirements (V1+)

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

## Future Considerations (V2+)

- **Multi-floor levels:** A `floors` array, each with its own grid and tileset.
- **Room templates:** Pre-authored room blueprints that can be stamped into the grid.
- **Procedural generation:** `TileType` probability tables and room-packing algorithms.
- **Streaming:** Large levels load/unload chunks based on player position.
