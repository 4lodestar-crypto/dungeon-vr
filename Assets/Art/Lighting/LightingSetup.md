# Dungeon VR — Lighting Specification
## Phase 5: Art Assets and Lighting · Council-Consolidated Deliverable

---

## 1. Visual Design Brief (Panelist 1)

### Mood Target
Dark, oppressive dungeon atmosphere. The player should feel the weight of stone around them, with pools of warm torchlight providing navigation cues. Not pitch black — the player needs to see walls at close range.

### Lighting Layers
| Layer | Source | Color | Purpose |
|-------|--------|-------|---------|
| Ambient Fill | Flat ambient mode | RGB(10,10,20) ≈ #0A0A14 | Barely-visible base so player isn't blind |
| Directional Skim | Single dim directional | RGB(38,46,64) ≈ #262E40 | Subtle blue tint from "above" — dungeon ceiling seep |
| Fog Depth | Linear fog | RGB(5,5,13) ≈ #05050D | Clamps visibility, hides geometry pop-in |
| Torch Points (×4–6) | Point lights | Warm orange #FF9930 | Navigation beacons at key positions |
| Altar Accent | Point light | Amber/gold #FFCC66 | Dramatic focal point |

### Point Light Placement Strategy
- **Spawn Room Torch** (×2): Two point lights flanking the spawn position. Range 12, intensity 0.8, warm orange.
- **Stairs Beacon** (×1): One point light near the exit stairs. Range 15, intensity 1.0, warm orange.
- **Corridor Marker** (×2): Two point lights placed in long corridors to prevent total darkness. Range 10, intensity 0.5.
- **Altar Dramatic** (×1): One point light above the altar. Range 8, intensity 1.5, amber/gold.

**Total: 6 point lights (Quest 3 pixel-light limit)**.

---

## 2. Optimization Review (Panelist 2) — Quest 3 Compliance

### Rule: Point Light Count ≤ 6
**PASS** ✅ — Exactly 6 point lights in the scene.

### Rule: No Realtime Shadows
**PASS** ✅ — All lights have `m_Shadows.m_Type: 0` (None). Shadows are too expensive on Quest 3.

### Rule: Linear Fog (Cheapest)
**PASS** ✅ — `m_FogMode: 1` (Linear). Linear fog has fixed GPU cost regardless of distance.

### Rule: Flat Ambient Mode
**PASS** ✅ — `m_AmbientMode: 0` (Flat). No skybox or gradient evaluation per frame.

### Rule: Draw Call Budget
- **Custom prefabs**: 1 material each (default), primitive meshes — extremely low poly (cube=12 tris, plane=2 tris, sphere=768 tris). Total scene: ~200 tris for a small dungeon floor.
- **Ultimate Dungeon Pack prefabs**: If swapped in, a typical ULP wall is ~200-500 tris. A full dungeon floor of 15×15 tiles with ~50% walls: ~3750 tris from walls + ~450 tris from floors. Well within Quest 3 budget (100K tris typical).
- **Material count**: Currently 1 material (default) shared across all custom prefabs = perfect batching. If ULP assets are used, material count would be ~4-6 unique materials — still good.

**PASS** ✅ — Current setup is extremely conservative. Even with ULP swap, well within budget.

### Rule: No Broken Prefabs
**WARNING** ⚠️ — All custom prefabs use `{fileID: 0}` for materials (default material). They will render as white/gray primitives. Functional but not visually appealing. Recommendation: swap in Ultimate Dungeon Pack meshes via wrapper prefabs.

### Estimated Performance Profile
| Metric | Current (Custom) | With ULP Swap |
|--------|-----------------|---------------|
| Draw calls (static) | ~50-100 | ~150-300 |
| Triangles | ~200-500 | ~5,000-15,000 |
| Point lights | 6 | 6 |
| Materials (unique) | 1 | ~6 |
| Fog cost | Linear (fixed) | Linear (fixed) |
| Shadows | None | None |
| **Quest 3 frame budget** | **Well under** | **Well under** |

---

## 3. Pipeline Integration Report (Panelist 3)

### GUID Verification
| TileType | TilePalette GUID | Actual File GUID | Match? |
|----------|-----------------|------------------|--------|
| Floor | f15cb95137b544d49bca40f73dca52bf | f15cb95137b544d49bca40f73dca52bf (Floor_Tile.prefab) | ✅ |
| Wall | 1ea3cd5036fcbe742ac887f5adc60f1b | 1ea3cd5036fcbe742ac887f5adc60f1b (Wall_Tile.prefab) | ✅ |
| Door | f1aa3a001e5648442aee64b905f8ac90 | f1aa3a001e5648442aee64b905f8ac90 (Door_Tile.prefab) | ✅ |
| Trap | 175510f219d809140a7c48271924f8b4 | 175510f219d809140a7c48271924f8b4 (Trap_Trigger_Tile.prefab) | ✅ |
| Altar | 349e9fb5696f3c942bbc1850acc7f262 | 349e9fb5696f3c942bbc1850acc7f262 (Altar_Tile.prefab) | ✅ |
| Stairs | c1d9807b1a294cf428a9dc75591c86f9 | c1d9807b1a294cf428a9dc75591c86f9 (Stairs_Tile.prefab) | ✅ |
| Spawn | b9adaca4a733cde44a877ec0841e63d2 | b9adaca4a733cde44a877ec0841e63d2 (Champion.prefab) | ✅ |

**All GUIDs verified. TilePalette.asset is internally consistent — no changes needed.**

### Prefab Structure Verification
All 6 custom tile prefabs have valid MeshFilter + MeshRenderer components. All are correctly scaled for TILE_SIZE=3.0f. No broken mesh references (all point to Unity built-in primitives, which are always available).

### Spawn Prefab Note
The Spawn tile references `Champion.prefab` — a humanoid capsule with Champion_Mesh (capsule collider). This is unusual for a spawn marker but functional. The LevelLoader will instantiate a Champion at spawn positions.

### ITilePalette Interface Compliance
✅ `GetPrefab(TileType)` returns valid prefabs for all 7 non-Empty types
✅ `IsComplete` would evaluate to true (all non-Empty types have non-null prefabs)
✅ `GetRotationForWallFaces` logic is unaffected by lighting changes
✅ `InstantiateTiles` in LevelLoader is unaffected

---

## 4. Changes Applied to ProceduralTest.unity

### RenderSettings (Fog & Ambient)
```
m_Fog: 0 → 1 (enabled)
m_FogColor: (0.5,0.5,0.5) → (0.02,0.02,0.05) near-black
m_FogMode: 3 → 1 (Linear)
m_LinearFogStart: 0 → 5
m_LinearFogEnd: 300 → 60
m_AmbientMode: 0 → 0 (Flat, unchanged)
m_AmbientSkyColor: (0.212,0.227,0.259) → (0.04,0.04,0.08)
```

### Directional Light
```
m_Color: (1,1,1) → (0.15,0.18,0.25) dim blue-tinted
m_Intensity: 1 → 0.12
m_Shadows.m_Type: 0 → 0 (no shadows, unchanged)
m_Lightmapping: 4 → 0 (Realtime)
```

### Added Point Lights (×6)
Six new GameObject+Light pairs added, all children of a new "DungeonLighting" root:
1. **SpawnTorch_A** — (-3, 2.5, 3), orange, range 12, intensity 0.8
2. **SpawnTorch_B** — (3, 2.5, -3), orange, range 12, intensity 0.8
3. **StairsBeacon** — (0, 3.0, 0), warm, range 15, intensity 1.0
4. **CorridorLight_N** — (0, 2.5, -15), dim orange, range 10, intensity 0.5
5. **CorridorLight_E** — (15, 2.5, 0), dim orange, range 10, intensity 0.5
6. **AltarDramatic** — (0, 2.0, 0), amber/gold, range 8, intensity 1.5
