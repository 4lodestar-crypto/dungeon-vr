# Tile Prefab Requirements (V0 Draft)

## Updated: V0-001

Requirements for all tile prefabs for the Dungeon VR project.

---

### Core Grid Constants

| Property     | Value        | Source                        |
|-------------|-------------|-------------------------------|
| Grid cell    | 3 m × 3 m   | `GameConstants.TILE_SIZE`     |
| Pivot        | center-bottom at Y=0 | Design convention    |
| Render pipeline | URP Lit  | Project settings              |

---

### Prefab Requirements

| # | Requirement | V0 Status | V1 Plan |
|---|------------|-----------|---------|
| 1 | **Grid cell**: every tile occupies exactly 3 m × 3 m in XZ | ✅ Done | — |
| 2 | **Pivot**: tile root transform origin at Y=0, centered in XZ | ✅ Done | — |
| 3 | **Walls**: include a BoxCollider matching visual bounds | ✅ Done | Tune collider shape |
| 4 | **Floors**: no collider (or a trigger for gameplay events) | ✅ No collider | Trigger zone for footstep FX |
| 5 | **Materials**: URP Lit shader on all renderers | ✅ URP Lit / Standard fallback | Shared material atlas |
| 6 | **LOD groups**: not required in V0 | ❌ Skipped | LOD0–LOD2 added |
| 7 | **GPU instancing**: tiles should share materials where possible | ⚠️ Per-instance mats (V0 placeholder) | Shared material block |

---

### Current Prefab List (V0 Placeholders)

Built programmatically via `PrefabBuilder` (`Assets/Scripts/Editor/PrefabBuilder.cs`).

| Prefab | Primitive | Scale | Material | Collider |
|--------|-----------|-------|----------|----------|
| `Floor_Tile_Stone` | Plane | (3, 1, 3) | `#CCCCCC` (light gray) | None |
| `Wall_Tile_Stone` | Cube | (3, 3, 0.5) | `#666666` (dark gray) | Box (auto) |
| `Champion_Default` | Capsule | H = 1.8 | `#4488FF` (blue) | Capsule (auto) |

---

### Loading Strategy (V0)

```
Caller → PrefabProvider.GetXxxPrefab()
          ├── Resources.Load("Prefabs/Xxx")   → cached, returned
          └── null → PrefabBuilder.BuildXxx()  → cached, returned
```

- **V0**: Runtime fallback building via `PrefabBuilder` (no `.prefab` assets required)
- **V1**: Pre-built `.prefab` files placed in `Assets/Resources/Prefabs/`; the runtime builder is removed

---

### Material Convention

| Label | Hex       | RGB            | Used By           |
|-------|-----------|----------------|-------------------|
| Light gray | `#CCCCCC` | (204, 204, 204) | Floor tiles       |
| Dark gray  | `#666666` | (102, 102, 102) | Wall tiles        |
| Blue       | `#4488FF` | (68, 136, 255)  | Champion capsule  |

---

### Known V0 Limitations

1. Materials are **per-instance** — no GPU instancing across tiles.
2. No LOD groups — full-detail meshes at all distances.
3. Floor tiles receive shadows but cast none by default.
4. Champion capsule uses primitive collider — not fine-tuned for VR hand presence.
