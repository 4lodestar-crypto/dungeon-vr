# V1 Phase 3+ Deployment — Agent Instructions

**From:** Halk (Captain)
**Date:** 2026-06-12
**Target:** Procedural dungeon generation, Screamer monster, CI infrastructure

## Context

V0-001 (grid movement, tick loop) ✅ — committed d566a07
V1-001 Phase 1 (level pipeline code) ✅ — committed 4f2e517
Art pack imported (Ultimate Low Poly Dungeon) ✅ — committed ec739f3
V1-001 Phase 2 (TilePalette asset, prefab baking, scene wiring) — **IN PROGRESS** — Admiral is creating TilePalette.asset and wiring Floor01 scene via Unity Editor. Completion expected soon.

## Dependency Chain

```
Phase 2 (art wired) ─┬─ Phase 3 (procgen) ── Phase 4 (Screamer) ── Phase 6 (CI)
                      └─ Phase 5 (lighting)
```

## Agent Assignments (Phase 3+)

### Phase 3: Procedural Dungeon Generation (T11-T15)
**Lead:** level-content specialist
**Depends on:** Phase 2 completing (TilePalette asset on disk)

T11: Dungeon generator algorithm — rooms + corridors, guaranteed connectivity via BFS flood fill, seed-based RNG
T12: Generator → TileData[] converter — maps dungeon layout to level format
T13: Generator parameters ScriptableObject — seed, room count, room size range
T14: Integration — generator output feeds LevelLoader
T15: EditMode tests — 10+ seeds produce valid solvable floors

### Phase 4: Screamer Monster (T16-T23)
**Lead:** ai-monster specialist
**Depends on:** Phase 3 completing (spawn points in level data)

T16: MonsterDefinition ScriptableObject — stats, archetype, behavior params
T17: Basic state machine — Idle → Alert → Attack
T18: A* pathfinding on tile grid — blocking tiles, path caching
T19: Spawn system wired to LevelLoader
T20: AttackRequest pattern (placeholder, real combat in V3)
T21: Screamer placeholder prefab
T22-T23: EditMode + PlayMode tests

### Phase 5: Art Assets & Lighting (T29-T31)
**Lead:** art-asset specialist
**Depends on:** Phase 3 completing (prefab requirements stable)

T29: Baked lighting setup — directional light, light probes
T30: Shared material palette — URP Lit, properly compressed
T31: Replace primitives with art pack prefabs in TilePalette

### Phase 6: CI Infrastructure (T32-T35)
**Lead:** qa-test + networking-architect specialists
**Depends on:** Phase 1 scripts compiling (already done)

T32: Unity build in GitHub Actions (Windows standalone)
T33: EditMode tests in CI
T34: PlayMode tests in CI
T35: Library caching

## Execution Order

1. Wait for Admiral completion signal (phase 2)
2. Deploy Phase 3 (level-content)
3. Phase 4 (ai-monster) starts 1 step behind Phase 3
4. Phase 5 (art-asset) and Phase 6 (qa-test) run in parallel after Phase 3 data is stable
5. Verify each phase before advancing

## Verification Gates

Each phase must pass before the next starts:
- Phase 3: Procedural dungeon visible in Editor, 10+ seed tests pass
- Phase 4: Screamer spawns and follows champion, tests pass
- Phase 5: Lighting looks correct, art pack replaces all primitives
- Phase 6: CI builds green, all tests pass in GitHub Actions
