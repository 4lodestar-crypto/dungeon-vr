# Architecture Decision Record

## ADR-002: V0-001 Implementation — Project Setup + All 8 Sub-tasks Deployed

**Date:** 2026-06-06
**Status:** Complete — awaiting Andrew review

### Context

After ADR-001 scope confirmation, all 8 sub-tasks (T1–T8) were implemented by the Orchestrator acting as the sole agent (no specialist subagents available). Unity was not installed on the build machine, so all files were created manually as text.

### Decision

All 8 sub-tasks implemented in a single commit on `develop` (0bfa0ec):

| ID | Description | Status |
|----|-------------|--------|
| T1 | Unity 6 LTS project for Windows Desktop with URP, TestGrid.unity, folder structure | Done |
| T2 | Grid movement: ChampionState, MovementHandler, PlayerInput (WASD), PlayerCamera | Done |
| T3 | Tick loop: GameTick at 20 Hz FixedUpdate, GameState, ITickableSystem | Done |
| T4 | Movement EditMode tests (6 tests: forward, wall, rotate L/R, off-grid, multi) | Done |
| T5 | Tick loop EditMode tests (5 tests: empty, queue, 20Hz, sequential, invalid) | Done |
| T6 | GitHub Actions CI (lint, build-windows, test-editmode, coverage, artifacts) | Done |
| T7 | TestAssembly.asmdef, TestGridBuilder fixture, placeholder test directories | Done |
| T8 | Integration smoke tests (5 tests: forward, wall, multi, rotate+move, empty) | Done |

### Files created (31 total)

- 11 C# source files (Gameplay:5, Server:3, Shared:3)
- 3 test files (MovementHandlerTests, GameTickTests, GridMovementIntegrationTests)
- 1 TestGridBuilder fixture
- 1 TestAssembly.asmdef
- 1 CI workflow (.github/workflows/ci.yml)
- 1 Editor build script (Assets/Editor/BuildScript.cs)
- 1 TestGrid.unity (YAML scene)
- Unity project config (Package manifest, ProjectVersion, TimeManager, ProjectSettings)
- .gitattributes, .gitignore
- 6 .gitkeep files for empty directories

### Notes

- Unity 6 LTS (6000.0.23f1) specified in ProjectVersion.txt
- 20 Hz tick rate set in TimeManager.asset (Fixed Timestep = 0.05)
- All V0-EXCEPTION comments placed on files that bypass server layer
- Scene YAML uses placeholder script GUIDs — will need Unity Editor re-import
- AI, VR, Level, Net, Art specialist directories created and empty

## ADR-001: V0-001 Scope Change — Desktop-First Grid Movement + Tick Loop

**Date:** 2026-06-06
**Status:** Active (provisional — awaiting Andrew confirmation)

### Context

The original `docs/tickets/V0-001-hello-vr-world.md` described a Meta Quest VR pipeline proof (hand tracking, torch grab, door push, APK build). This was written before `CLAUDE.md` established the **desktop-first strategy** (V0–V1 target = Windows Desktop; VR in V2+).

The new CLAUDE.md strategy renders the original ticket scope invalid for V0. A scope correction is needed.

### Decision

V0-001 is re-scoped to: **a Windows Desktop Unity project with grid-based movement and a server-authoritative tick loop.** The old VR-specific deliverables are deferred:

| Old Scope (superseded) | New Scope (replacement) | Reason |
|---|---|---|
| Meta XR SDK + Quest 3 build target | Unity 6 LTS Windows Desktop project | Desktop-first per CLAUDE.md |
| Hand tracking + controller rig | WASD keyboard input | VR deferred to V2+ |
| Grabbable torch + pushable door | Grid movement system (tile-by-tile, 90° turns) | Core foundation needed first |
| APK build | Windows standalone build | Desktop-first |
| "Hello Dungeon" scene | TestGrid.unity with wall collision | Minimal proof-of-concept |

### Rationale

1. CLAUDE.md explicitly states desktop-first. VR scope in V0 contradicts this.
2. The grid movement and tick loop are the architectural foundation everything else builds on. Getting these right in V0 prevents refactoring later.
3. A desktop build + EditMode tests is faster to iterate on than VR APK + PlayMode tests.

### Consequences

- The old `docs/tickets/V0-001-hello-vr-world.md` should be moved to `docs/tickets/archive/` or marked "superseded" once Andrew confirms this decision.
- V2+ will revisit VR-specific deliverables (hand tracking, grabbable objects, APK builds) when the Meta XR SDK is enabled.
- Gameplay Systems owns the bulk of V0-001 work. VR Interaction is not needed until V2+.
- Art/Asset, AI/Monster, Level/Content, and Networking Architect are not active in V0-001.

### Provisional status

This decision is provisional until Andrew confirms. If Andrew prefers the original VR pipeline scope, revert to the old plan. See escalation in orchestrator summary.
