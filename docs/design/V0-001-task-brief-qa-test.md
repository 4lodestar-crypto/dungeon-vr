# Task Brief: QA/Test — V0-001 CI + Test Infrastructure

**To:** QA/Test specialist
**Ticket:** V0-001 — Hello VR World (desktop-first version: grid movement + server-authoritative tick loop)
**Branch:** `feat/ci-pipeline` (T6), `feat/test-infrastructure` (T7), `feat/integration-smoke` (T8)

---

## Executive Summary

You are setting up the **quality foundation** for Dungeon VR: the CI pipeline that builds Windows Desktop standalone, runs EditMode tests, and the test infrastructure scaffold that specialists will use for all future tests. Read `CLAUDE.md`, your role file at `docs/agents/qa-test.md`, and the full plan at `docs/design/V0-001-plan-grid-movement-tick-loop.md`.

**Important context:** V0-001 targets Windows Desktop only (no VR, no Meta XR SDK, no APK builds). The old ticket describing a VR Quest pipeline is superseded by CLAUDE.md's desktop-first strategy.

**Your deliverables:**
1. GitHub Actions CI workflow that builds Windows + runs EditMode tests
2. Test infrastructure scaffold (asmdef, TestGridBuilder fixture)
3. Integration smoke test that verifies movement + tick loop end-to-end

---

## Sub-tasks (do in order: T6+T7 parallel, then T8)

### T6: GitHub Actions CI Pipeline
Create `.github/workflows/ci.yml`:

**Triggers:**
- `push` to `develop`, `feat/*`
- `pull_request` targeting `develop`

**Jobs:**

| Job | Runner | Steps | Timeout |
|---|---|---|---|
| `lint` | ubuntu-latest | Check YAML validity, verify .gitignore covers Unity files | 2 min |
| `build-windows` | windows-latest (or ubuntu-latest with Unity headless) | Unity `-buildTarget Win64` standalone build; cache Library folder; output Build/ artifact | 20 min |
| `test-editmode` | ubuntu-latest (Unity Linux batchmode) | Unity `-runEditorTests` on EditMode tests; output NUnit XML results | 10 min |
| `coverage` | ubuntu-latest | Collect Unity Code Coverage data; publish HTML report as artifact; enforce 60% line coverage gate | 10 min |

**Key CI notes:**
- Use `unity-ci/actions` for Unity on GitHub Actions (free Personal license tier)
- Library folder caching: keyed by `hashFiles('Packages/manifest.json')` + Unity version
- The build stage should succeed even without the Tick/Movement code being complete — test stage may fail gracefully if no tests exist yet
- Artifacts: Windows build .exe + Data folder, NUnit XML test results, HTML coverage report
- No Android/APK builds in V0
- **No Unity Pro license required** — use Unity Personal (free) via `unity-ci/actions`

**Branch:** `feat/ci-pipeline`

### T7: Test Infrastructure Scaffold
Create the test project structure:

**`Assets/Tests/TestAssembly.asmdef`**
- Assembly name: `DungeonVR.Tests`
- References: `UnityEngine.TestRunner`, `NUnit.Framework`, `UnityEditor.TestTools`
- Platform: Editor only
- No root namespace

**Directory structure:**
```
Assets/Tests/
  TestAssembly.asmdef
  EditMode/
    Systems/         (placeholder .gitkeep)
    Utilities/       (placeholder .gitkeep)
    Fixtures/
      TestGridBuilder.cs
    Integration/     (placeholder .gitkeep — T8 fills this)
  PlayMode/
    Systems/         (placeholder .gitkeep)
    Performance/
      Baselines/     (placeholder .gitkeep)
      Reports/       (placeholder .gitkeep — gitignored)
    Fixtures/        (placeholder .gitkeep)
```

**`TestGridBuilder.cs` — critical shared fixture:**
The gameplay systems specialist will use this to test movement + tick. It must:

```csharp
public static class TestGridBuilder
{
    /// Returns a 5x5 bool[,] where edges are walls (true) and interior is walkable (false)
    public static bool[,] Create5x5PerimeterWalled() { ... }
    
    /// Returns a 3x3 bool[,] with no walls (all false)
    public static bool[,] Create3x3Empty() { ... }
    
    /// Returns a ChampionState at position (2,2) facing FacingDirection.North
    public static ChampionState CreateDefaultChampion() { ... }
    
    /// Returns a new GameTick pre-configured with the given grid
    public static GameTick CreateTickForGrid(bool[,] walls) { ... }
}
```

Note: `ChampionState`, `FacingDirection`, `GameTick` classes are created by Gameplay Systems (T2, T3). If those don't exist yet, keep `TestGridBuilder` as a standalone generator of `bool[,]` grids and add the champion/tick helper methods after T3 merges.

**Branch:** `feat/test-infrastructure`

### T8: Integration Smoke Test (EditMode)
Create `Assets/Tests/EditMode/Integration/GridMovementIntegrationTests.cs`:

```csharp
[Test]
public void Champion_MoveForwardOnEmptyGrid_AdvancesOneTile()
{
    // Arrange: create 5x5 empty grid, champion at (2,2) facing North
    var grid = TestGridBuilder.Create5x5PerimeterWalled();
    var champion = /* create champion at (2,2) North */;
    var tick = /* create GameTick with grid + champion */;
    
    // Act: enqueue MovementRequest(Forward), run one tick
    tick.EnqueueRequest(new MovementRequest(Direction.North, tickNumber: 1));
    tick.ExecuteTick();
    
    // Assert: champion now at (2,3)
    Assert.AreEqual(new Vector2Int(2, 3), champion.GridPosition);
}
```

**Additional tests to include (same file):**

| Test | What It Verifies |
|---|---|
| `Champion_MoveIntoWall_StaysInPlace` | From edge (2,4) facing North (wall at row 4), W → position stays (2,4), result.Success = false |
| `Champion_MultipleMoves_AcrossMultipleTicks_ArrivesCorrectly` | Queue 3 forward moves, run 3 ticks → position advanced 3 tiles |
| `Champion_RotateLeftAndMove_ChangesDirection` | Rotate left (facing West), move forward → position.x decreases by 1 |
| `System_EmptyTick_DoesNotChangeState` | Run tick with no requests → champion unchanged |

**Branch:** `feat/integration-smoke`

---

## Coordination with Gameplay Systems

- Gameplay Systems owns T1 (project setup) — they create the Unity project first
- You start T6 and T7 after T1 merges (you need a compilable project structure)
- T8 depends on T2 (movement) + T3 (tick loop) — wait for those to merge before writing
- The `TestGridBuilder` fixture (T7) may need updates after Gameplay Systems creates `ChampionState` / `GameTick` / `MovementRequest`. Coordinate: write the `bool[,]` grid generator first, add champion/tick helper overloads after T3 merges
- If `TestGridBuilder` references classes that don't exist yet, use a simple interface or abstract the grid-only portion

## Acceptance Criteria Summary

- [ ] T6: `.github/workflows/ci.yml` exists; triggers on push/PR to develop; builds Windows; runs EditMode tests; publishes artifacts
- [ ] T7: `Assets/Tests/TestAssembly.asmdef` compiles; `TestGridBuilder.cs` generates 5×5 and 3×3 grids; placeholder dirs exist
- [ ] T8: Integration tests pass in EditMode; cover forward move, wall block, multi-move, rotate+move, empty tick
- [ ] All tests deterministic: no timing dependencies, no random, no network
- [ ] Tests follow AAA pattern with blank-line separation
- [ ] No `[Ignore]` or `[Explicit]` — if a test can't pass yet because dependencies are missing, don't write it yet
- [ ] CI uses Unity Personal (free), not Unity Pro

## PR Requirements
Each sub-task gets its own PR. PR title format: `[V0-001] {short summary}`. Description includes: What changed, Why, How to test, Performance impact.
