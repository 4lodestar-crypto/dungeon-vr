# Plan: V1 — Level Data Pipeline + Procedural Dungeon + First Content

**Version:** 1.0
**Date:** 2026-06-11
**Author:** Halk (Dungeon VR Captain)
**Status:** Draft — pending Andrew approval

---

## Context

V0-001 delivered the core loop (grid movement, 20 Hz tick, first-person camera, 14 EditMode tests) on a hardcoded 5×5 test grid with primitive placeholders. V1 replaces the hardcoded grid with a data-driven level pipeline, adds procedural dungeon generation, introduces the first monster, and replaces primitives with real tiles.

The 6 design docs written during V0 define the V1 contracts:
- `docs/design/level-format.md` — TileData schema, JSON structure
- `docs/design/ai-system-design.md` — Monster framework design
- `docs/design/networking-constraints.md` — Multiplayer compatibility rules
- `docs/design/deferred-netcode-work.md` — 5 items to fix before V4
- `docs/design/tile-prefab-requirements.md` — Art prefab specs

---

## V1 Scope

### In scope
| Area | What's delivered |
|------|-----------------|
| **Level data pipeline** | JSON tile format, TilePalette ScriptableObject, LevelLoader MonoBehaviour, GridService |
| **Editor tools** | Grid-based level editor window for Andrew (paint tiles, export JSON, run validator) |
| **Procedural dungeon** | Rooms + corridors generation, guaranteed connectivity, seed-based, interior walls |
| **First monster (Screamer)** | Basic state machine, A* pathfinding, spawn from level data, AttackRequest pattern |
| **Art assets** | Stone wall/floor tile prefabs, champion model, basic materials, lighting |
| **PlayMode tests** | Floor loading, movement integration, dungeon generation validation |
| **Level validator** | Solvability checks, tile palette completeness, no orphaned spawns |

### Out of scope (deferred)
| Feature | Target |
|---------|--------|
| VR interaction (hand tracking, grabbables) | V2 |
| Combat (damage formulas, blocking, hit resolution) | V3 |
| Inventory, items, equipment | V3 |
| Spell system, rune tracing | V3 |
| Save/Load | V3 |
| Multiplayer networking | V4 |
| Multiple floors | V5 |
| Audio integration | V5 |

---

## Milestones

### V1-001 — Level Data Pipeline
**Lead:** Level/Content agent
**Dependencies:** None (V0 shared contracts exist)

| ID | Task | Assignee | Acceptance Criteria |
|----|------|----------|-------------------|
| **T1** | Define TileData schema in `docs/design/level-format.md` | Level/Content | Schema versioned, TileType enum (8 values), TileData struct with all fields, JSON structure documented |
| **T2** | Create TilePalette ScriptableObject, `TilePalette.asset` | Level/Content | Maps TileType → prefab; validates all slot assignments; exists at `Assets/Data/Levels/TilePalette.asset` |
| **T3** | Implement LevelLoader MonoBehaviour | Level/Content | Reads JSON via TextAsset, iterates tiles, instantiates from TilePalette at correct grid positions, registers in GridService, emits LevelLoadedEvent |
| **T4** | Implement GridService (runtime grid query) | Level/Content | Implements IGridQueryService (already defined), O(1) TileAt, zero-alloc IsPassable, spawn point registration |
| **T5** | Build level editor tool window | Level/Content | Custom Editor window `LevelEditorWindow.cs` — grid view, tile palette paint brush, export/import JSON, validator button. Must be usable by non-programmer |
| **T6** | Build level validator | Level/Content | Checks: player start exists, exit exists, exit reachable, no orphaned spawns, no overlapping tiles, all palette refs valid, JSON deserializes cleanly |
| **T7** | Create Floor 1 JSON data file | Level/Content | Floor01.json — 32×32 grid with rooms + corridors, guaranteed connectivity, player start + exit + spawn points + altar + trap |
| **T8** | Create Floor 1 thin loader scene | Level/Content | `Floor01_HallOfChampions.unity` — only contains LevelLoader + directional light + post-process volume (no baked geometry) |
| **T9** | EditMode tests for level pipeline | QA/Test | Load JSON → validate → instantiate → verify tile count, positions, connectivity |
| **T10** | PlayMode test for floor loading | QA/Test | Load scene → wait for LevelLoadedEvent → verify all tiles present, champion at spawn point |

### V1-002 — Procedural Dungeon Generation
**Lead:** Level/Content agent
**Dependencies:** V1-001 (needs TileData schema and palette)

| ID | Task | Assignee | Acceptance Criteria |
|----|------|----------|-------------------|
| **T11** | Implement dungeon generator algorithm | Level/Content | Generates rooms (random size/position) + corridors (L-shaped), guaranteed connectivity via BFS flood fill, seed-based RNG |
| **T12** | Implement generator → TileData converter | Level/Content | Generator output maps to TileData[] with correct types, wall faces, metadata |
| **T13** | Add generator parameters ScriptableObject | Level/Content | Seed, room count, room size range, corridor width, floor index |
| **T14** | Integration: generator feeds LevelLoader | Level/Content | Generator produces valid output → LevelLoader consumes it → scene builds correctly |
| **T15** | EditMode tests for generator | QA/Test | 10+ seeds produce valid solvable floors, no overlapping rooms, all corridors connect, no orphaned rooms |

### V1-003 — First Monster (Screamer)
**Lead:** AI/Monster agent
**Dependencies:** V1-001 (needs spawn point data from level loader)

| ID | Task | Assignee | Acceptance Criteria |
|----|------|----------|-------------------|
| **T16** | Implement MonsterDefinition ScriptableObject | AI/Monster | `[CreateAssetMenu]` with stats, archetype, behavior params. Create Screamer definition asset |
| **T17** | Implement basic state machine | AI/Monster | States: Idle → Alert (champion in range) → Attack (adjacent). Grid-based detection only |
| **T18** | Implement A* pathfinding on tile grid | AI/Monster | Tile-to-tile path, blocking tiles (walls, closed doors), path caching, O(1) neighbor lookups |
| **T19** | Wire spawn system to LevelLoader | AI/Monster | Level loaded → spawn points registered → monsters created → state machine tick starts |
| **T20** | Wire AttackRequest through Gameplay Systems | AI/Monster | Monster constructs AttackRequest, calls IMovementRequestHandler (placeholder — V3 adds real combat resolution) |
| **T21** | Create Screamer placeholder prefab | Art/Asset | Primitive with red material, collider, eye-level orientation |
| **T22** | EditMode tests for AI | QA/Test | State transitions correct, pathfinding finds valid paths, 0.5ms combined tick budget measured |
| **T23** | PlayMode test for monster spawn | QA/Test | Level loads → Screamer spawns at tile → champion moves in range → monster alerts → approaches |

### V1-004 — Art Assets & Lighting
**Lead:** Art/Asset agent
**Dependencies:** V1-001 (needs tile prefab requirements)

| ID | Task | Assignee | Acceptance Criteria |
|----|------|----------|-------------------|
| **T24** | Create stone wall tile prefab | Art/Asset | 3×3m, box collider, URP Lit material, baked lightmap static, LOD group (3 levels) |
| **T25** | Create stone floor tile prefab | Art/Asset | 3×3m, no collider, URP Lit material, baked lightmap static, matches wall aesthetic |
| **T26** | Create champion model prefab | Art/Asset | Humanoid capsule or simple mesh, eye-height, first-person compatible (invisible from inside) |
| **T27** | Create door prefab (placeholder) | Art/Asset | Simple cube with push-collider zone, pivot at edge, interactive marker |
| **T28** | Create altar / trap trigger prefabs | Art/Asset | Primitive-based with distinct colors (gold for altar, red for trap), interaction zones |
| **T29** | Setup baked lighting | Art/Asset | Directional light, light probes, reflection probe(s), bake for Floor 1 scene |
| **T30** | Create material palette | Art/Asset | Shared materials (not per-instance), URP Lit shaders, properly compressed for Quest future |
| **T31** | Replace primitives in editor tool palette | Art/Asset | TilePalette updated to use new prefabs, old primitives removed |

### V1-005 — CI & Infrastructure
**Lead:** QA/Test agent
**Dependencies:** V1-001 (needs compilable project with new assemblies)

| ID | Task | Assignee | Acceptance Criteria |
|----|------|----------|-------------------|
| **T32** | Enable real Unity build in CI | QA/Test | GitHub Actions workflow runs Unity batchmode build for Windows, outputs executable artifact |
| **T33** | Enable EditMode tests in CI | QA/Test | Unity batchmode runs EditMode tests, publishes NUnit XML results, fails CI on test failure |
| **T34** | Enable PlayMode tests in CI | QA/Test | Unity batchmode runs PlayMode tests, publishes results |
| **T35** | Set up Library caching in CI | QA/Test | Library folder cached between runs (speeds up build), cache keyed by packages-lock hash |

---

## Dependency Graph

```
V1-001 (Level Pipeline)
  ├── V1-002 (Procgen) — depends on level data format
  ├── V1-003 (Monster) — depends on spawn points
  └── V1-004 (Art) — depends on prefab requirements
V1-005 (CI) — depends on project compiling
```

### Execution Order

```
Phase 1: T1–T8 (Level/Content: schema, palette, loader, editor tool, validator, floor JSON, scene)
Phase 2: T9–T10 (QA/Test: level pipeline tests) + T24–T28 (Art: tile prefabs)
Phase 3: T11–T15 (Level/Content: procgen + generator tests)
Phase 4: T16–T23 (AI/Monster: Screamer + AI tests)
Phase 5: T29–T31 (Art: lighting, materials, palette swap)
Phase 6: T32–T35 (QA/Test: CI pipeline activation)
```

---

## Shared Contracts (already defined in V0)

These interfaces are agreed and will NOT change during V1:

| Contract | File | Owner |
|----------|------|-------|
| `MovementRequest` | `Assets/Scripts/Shared/Requests/MovementRequest.cs` | Gameplay Systems |
| `MovementResult` | `Assets/Scripts/Shared/Results/MovementResult.cs` | Gameplay Systems |
| `IGridQueryService` | `Assets/Scripts/Shared/Interfaces/IGridQueryService.cs` | Level/Content |
| `IMovementRequestHandler` | `Assets/Scripts/Shared/Interfaces/IRequestHandler.cs` | Gameplay Systems |
| `ChampionState` | `Assets/Scripts/Shared/GameState.cs` | Gameplay Systems |
| `GameState` | `Assets/Scripts/Shared/GameState.cs` | Server |
| `TileCoord` | `Assets/Scripts/Shared/Data/TileCoord.cs` | Level/Content |
| `GameConstants` | `Assets/Scripts/Shared/Constants.cs` | Shared |

---

## New Interfaces (defined in V1 design docs)

| Contract | Defined In | Notes |
|----------|-----------|-------|
| `TileType` enum | `docs/design/level-format.md` | 8 values: Floor, Wall, Door, Trap, Altar, Spawn, Stairs, Empty |
| `TileData` struct | `docs/design/level-format.md` | Schema version 0, JSON-serializable |
| `ITilePalette` | Level/Content agent | Maps TileType → prefab |
| `ILevelLoader` | Level/Content agent | Loads → validates → instantiates → registers |
| `IGridPathfinder` | `docs/design/ai-system-design.md` | A* on tile grid |
| `IMonsterState` | `Assets/Scripts/AI/Interfaces/IMonsterBehavior.cs` | Defined in V0 as stub |
| `IMonsterBehavior` | `Assets/Scripts/AI/Interfaces/IMonsterBehavior.cs` | Defined in V0 as stub |
| `MonsterDefinition` | `Assets/Scripts/AI/Data/MonsterDefinition.cs` | ScriptableObject (stub exists) |

---

## Performance Targets (V1)

| Metric | Target | Measured By |
|--------|--------|-------------|
| Floor load time (32×32) | < 1 second | QA/Test PlayMode benchmark |
| GridService.TileAt() | O(1), zero allocation | Code review + test |
| All-AI tick budget | < 0.5ms combined | QA/Test performance test |
| Draw calls | < 72 (desktop — no budget yet) | Editor Stats window |
| Editor tool frame rate (32×32 grid) | Smooth | Manual test |

---

## V0 Exceptions Expiring

| Exception | V0 File | Expires V1 | Replacement |
|-----------|---------|------------|-------------|
| In-process server | GameServer | V1 | Proper server layer abstraction |
| Hardcoded grid | GridBuilder | V1 | Level data JSON pipeline |
| FindObjectOfType patterns | InputQueueBridge, PlayerInputHandler, PlayerCameraController | V1 | Service locator or DI |
| Primitive placeholders | PrefabBuilder, PrefabProvider | V1 | Real tile prefabs from Art/Asset |

---

## Risk Register

| Risk | Likelihood | Impact | Mitigation |
|------|-----------|--------|------------|
| Level editor tool too complex for Andrew | Medium | High | Test with Andrew early; simplify UI; add tooltips everywhere |
| Procgen produces unsolvable floors | Low | High | Validator catches every output; multiple seed regression tests |
| AI tick budget exceeded with 20+ monsters | Medium | Medium | Time-budgeted dispatcher; distant monsters skip ticks |
| Art prefabs don't fit 3m grid pivot | Low | Medium | Clear prefab requirements doc; test each prefab immediately |
| JSON schema changes break V0 tests | Low | Medium | Schema version field; migration path documented |

---

## Files to Create

```
docs/plans/V1-plan-level-pipeline-content.md
docs/tickets/V1-001-level-pipeline.md
docs/tickets/V1-002-procedural-dungeon.md
docs/tickets/V1-003-first-monster.md
docs/tickets/V1-004-art-assets.md
docs/tickets/V1-005-ci-infrastructure.md
```

---

## Approval

This plan is a draft pending Andrew's review. Once approved:
1. I create tickets for V1-001
2. Deploy Level/Content, Art/Asset, and QA/Test agents in parallel (Phase 1)
3. Review and iterate per V0 workflow
