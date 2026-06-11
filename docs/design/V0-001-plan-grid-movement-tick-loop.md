# Plan: V0-001 — Grid Movement + Server-Authoritative Tick Loop

**Version:** 1.0  
**Date:** 2026-06-06  
**Author:** Orchestrator  
**Status:** Active — delegation in progress

---

## Context — Why This Plan Exists

The original `docs/tickets/V0-001-hello-vr-world.md` described a Meta Quest VR pipeline proof (hand tracking, torch grab, door push, APK build). That ticket was written before CLAUDE.md established the **desktop-first strategy** (V0–V1 target = Windows Desktop; VR in V2+). This plan supersedes the original ticket scope for V0-001.

The actual first deliverable per the current architecture: **a Windows Desktop Unity project where a champion moves on a tile grid via a server-authoritative tick loop.**

---

## Objective

Andrew can launch the Unity 6 LTS project on Windows Desktop, use WASD to move a champion tile-by-tile across a test grid with 90° snap turns, and observe the 20 Hz server-authoritative tick loop processing his movement requests and rejecting invalid moves (walls, edges).

---

## Scope

### In scope
- Unity 6 LTS project setup for Windows Desktop standalone
- URP configured (default quality for desktop)
- Champion tile-based movement: 3m grid pitch, 4 cardinal directions, WASD input
- Server-authoritative tick loop: 20 Hz FixedUpdate, request-validate-apply-emit
- EditMode unit tests for movement + tick loop
- GitHub Actions CI for Windows build + EditMode tests
- Test infrastructure scaffold (asmdef, grid builder fixtures)

### Out of scope (deferred)
- Meta XR SDK, hand tracking, VR interaction — V2+
- Torch/door/"Hello Dungeon" scene elements (old V0-001) — V1+
- Level data format (JSON/ScriptableObject) — V1
- Monsters, AI, combat — V1+
- Free-locomotion movement — never (per CLAUDE.md rule #2)
- PlayMode tests — deferred to T3+
- APK/Android builds — V2+
- Art assets, materials, lighting — V1+

---

## Sub-tasks

| ID | Description | Assignee | Acceptance Criteria |
|---|---|---|---|
| **T1** | Create Unity 6 LTS project for Windows Desktop with URP; set up folder structure per CLAUDE.md; create test scene with flat 5×5 tile grid + wall colliders on perimeter; configure .gitignore + Git LFS | Gameplay Systems | Project opens in Unity 6 LTS Editor; `Assets/Scripts/{Gameplay,Server,Shared,VR,AI,Level,Net}` exist; `Assets/Scenes/TestGrid.unity` loads; floor tiles visible; perimeter walls block; .gitignore skips Library/Temp/Obj/Build |
| **T2** | Implement grid movement: `ChampionState` (position, facing), `MovementRequest` struct, `MovementHandler` (bounds + wall validation), WASD→MovementRequest input, first-person camera | Gameplay Systems | W = forward 1 tile; S = back 1 tile; A = rotate 90° left; D = rotate 90° right; Wall/edge rejection; Camera follows champion facing; No free/diagonal movement |
| **T3** | Implement tick loop: `GameTick` on FixedUpdate at 20 Hz, `GameState`, request queue per tick, validate-apply-emit, `MovementRequestHandler`, seeded RNG | Gameplay Systems | 20 Hz tick (50ms intervals); Queued requests processed per tick; Invalid moves rejected w/ block result; State snapshot emitted each tick; All logic in FixedUpdate (not Update); V0 exception note present |
| **T4** | Movement EditMode tests: 5 tests covering forward, wall-block, rotate-left, rotate-right, queue ordering | Gameplay Systems | All pass in EditMode; Deterministic; AAA format; No magic values |
| **T5** | Tick loop EditMode tests: 5 tests covering empty tick, queued process, 20 Hz rate, sequential ticks, invalid move safety | Gameplay Systems | All pass in EditMode; Deterministic; AAA format |
| **T6** | Set up GitHub Actions CI: trigger on push/PR to develop, Windows build, EditMode tests, NUnit XML artifacts, Library cache | QA/Test | `.github/workflows/ci.yml` exists; CI runs on PR; Build succeeds; Tests run and report pass/fail; Artifacts attached |
| **T7** | Test infrastructure scaffold: `Assets/Tests/EditMode/` + `TestAssembly.asmdef` + `TestGridBuilder.cs` fixture + PlayMode placeholder dirs | QA/Test | asmdef compiles; TestGridBuilder generates 5×5, 3×3 grids; No missing reference errors |
| **T8** | EditMode integration smoke test: instantiate champion + grid in EditMode, send MovementRequest through tick, verify position change | QA/Test | Integration test exists and passes; Covers end-to-end: input → request → tick → state change |

---

## Dependencies

```
T1 ─┬─ T2 ── T3 ──┬─ T4 (writes tests for T2)
    │              └─ T5 (writes tests for T3)
    ├─ T6 (CI needs a compilable project)
    └─ T7 (test scaffold needs project)
                   └─ T8 (needs T2+T3 to verify)
```

**Execution order:** T1 first, then T2+T6+T7 in parallel, then T3, then T4+T5+T8 in parallel.

---

## Integration Notes

- **V0 exception (mandatory comment):** `// V0-EXCEPTION: refactor through proper server layer in V1` on every file that touches the tick system or movement handler. No remote network layer — runs in-process.
- **3m tile pitch** is the shared constant. Use `const float TILE_SIZE = 3.0f`.
- **Grid data is hardcoded** for V0 — no Level/Content schema yet. A simple 2D bool array `bool[,] _walls` is fine.
- **First-person camera** at champion eye height (1.7m) looking forward. No head bob.
- **No performance budget** enforced in V0. Desktop can handle anything.
- **CI must use Unity Personal** (free tier) on GitHub Actions runners. No Unity Pro requirement.
- **PascalCase classes**, `_camelCase` private fields, one class per file, filename matches class name.
- **No `GameObject.Find`**, no static singletons for player state, no LINQ in tick code, no `Random.Range`.
- **Branch naming:** `feat/unity-project-setup` (T1), `feat/grid-movement` (T2), `feat/tick-loop` (T3), `feat/movement-tests` (T4), `feat/tick-tests` (T5), `feat/ci-pipeline` (T6), `feat/test-infrastructure` (T7), `feat/integration-smoke` (T8)
