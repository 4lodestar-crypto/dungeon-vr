# Task Brief: Gameplay Systems — V0-001 Grid Movement + Tick Loop

**To:** Gameplay Systems specialist
**Ticket:** V0-001 — Hello VR World (desktop-first version: grid movement + server-authoritative tick loop)
**Branch:** `feat/grid-movement` (T2), `feat/unity-project-setup` (T1), `feat/tick-loop` (T3), plus your test branches

---

## Executive Summary

You are implementing the **core foundation** of Dungeon VR: the tile-based movement system and the server-authoritative tick loop on Windows Desktop. This is V0 — we prove the architecture works before building game content. Read `CLAUDE.md` at the repo root, your role file at `docs/agents/gameplay-systems.md`, and the full plan at `docs/design/V0-001-plan-grid-movement-tick-loop.md`.

**Important context shift:** The old `docs/tickets/V0-001-hello-vr-world.md` described a VR Quest pipeline. That scope is **superseded** by CLAUDE.md's desktop-first strategy. The real V0-001 (per ARCHITECTURE.md and CLAUDE.md) is: set up the Unity project, implement grid movement, implement the tick loop. No VR. No Meta XR SDK. Windows Desktop only.

---

## Sub-tasks (do them in order)

### T1: Unity Project Setup
- Create a Unity 6 LTS project at `/c/Users/4Andr/dungeon-vr/` for Windows Desktop
- Use URP (Universal Render Pipeline) — default Desktop quality preset
- Create the folder structure per CLAUDE.md:
  - `Assets/Scripts/Gameplay/`
  - `Assets/Scripts/Server/`
  - `Assets/Scripts/Shared/`
  - `Assets/Scripts/VR/` (empty for now)
  - `Assets/Scripts/AI/` (empty)
  - `Assets/Scripts/Level/` (empty)
  - `Assets/Scripts/Net/` (empty)
  - `Assets/Prefabs/`
  - `Assets/Scenes/`
  - `Assets/Tests/`
- Create `Assets/Scenes/TestGrid.unity` — a simple flat 5×5 tile grid:
  - Floor tiles: basic cubes or planes (grey material)
  - Perimeter walls: cubes along the edges
  - Grid pitch: 3.0 units between tile centers
  - A simple `GridData.cs` MonoBehaviour that stores a `bool[,] _walls` for the 5×5 grid
- Accept that no art assets exist — use primitive cubes/planes with default materials
- V0 exception note: no server layer, no networking — tick loop runs in-process
- **Branch:** `feat/unity-project-setup`
- **Coordinate with:** QA/Test (they need the project to create CI + test scaffold)

### T2: Grid Movement System
All movement logic in `Assets/Scripts/Gameplay/` (pure C# logic) with MonoBehaviour glue in `Assets/Scripts/Gameplay/Components/`.

**Core files to create:**

| File | Purpose |
|---|---|
| `Assets/Scripts/Shared/Requests/MovementRequest.cs` | `readonly struct` with `Direction (Vector2Int)` and `TickNumber (int)` |
| `Assets/Scripts/Gameplay/ChampionState.cs` | Pure class: `GridPosition (Vector2Int)`, `FacingDirection (enum N/E/S/W)` |
| `Assets/Scripts/Gameplay/Logic/MovementHandler.cs` | Validates move: bounds check (0,0–4,4), wall check against `GridData`, applies position or facing change |
| `Assets/Scripts/Gameplay/Components/PlayerInput.cs` | MonoBehaviour: reads WASD, creates `MovementRequest`, enqueues to the tick system. W=forward, S=backward, A=rotate 90° CCW, D=rotate 90° CW |
| `Assets/Scripts/Gameplay/Components/PlayerCamera.cs` | MonoBehaviour: first-person camera at eye height (1.7m), follows champion facing direction |

**Movement rules:**
- W = move 1 tile in facing direction (3.0 units)
- S = move 1 tile in opposite direction
- A = rotate 90° counter-clockwise (in place, no translation)
- D = rotate 90° clockwise (in place, no translation)
- Moving into a wall or off-grid: rejected, champion stays in place
- No diagonal movement
- No free-locomotion / smooth movement
- Movement requests are queued to the tick system, NOT applied directly from input

**V0 exception:** For V0, MovementRequests go directly to the tick system. In V1, they route through a proper server request layer. Add `// V0-EXCEPTION: refactor through server layer in V1` comment.

**Branch:** `feat/grid-movement`

### T3: Server-Authoritative Tick Loop
All tick logic in `Assets/Scripts/Server/`.

**Core files to create:**

| File | Purpose |
|---|---|
| `Assets/Scripts/Server/GameTick.cs` | MonoBehaviour on FixedUpdate: maintains 20 Hz tick rate, processes request queue each tick, emits state snapshots |
| `Assets/Scripts/Server/GameState.cs` | Holds champion state (position, facing), seeded RNG (`System.Random` with fixed seed for debug), list of pending requests |
| `Assets/Scripts/Server/ITickableSystem.cs` | Interface: `void OnTick(int tickNumber, GameState state)` — for plugging in movement, future combat etc. |
| `Assets/Scripts/Shared/Results/MovementResult.cs` | `readonly struct` with `bool Success`, `Vector2Int NewPosition`, `string BlockReason` |

**Tick loop behavior:**
- 20 Hz = 50 ms between ticks (Unity's FixedUpdate set to 0.05s)
- Each tick: collect all queued requests → validate → apply valid ones → emit state
- After each tick: `Debug.Log($"Tick {tickNumber}: Champion at ({x},{y}) facing {direction}")`
- Seeded RNG: `new System.Random(42)` for deterministic debug runs
- Thread safety not needed (everything on main thread in V0)
- No allocations per tick: use structs, pooled queues (or simple `List<MovementRequest>` cleared each tick)

**Branch:** `feat/tick-loop`

### T4: Movement EditMode Tests
Create in `Assets/Tests/EditMode/` (add asmdef reference as needed):

| Test Name | What It Verifies |
|---|---|
| `MovementHandler_ForwardFromOrigin_AdvancesOneTile` | From (2,2) facing North, W → position is (2,3) |
| `MovementHandler_MoveIntoWall_StaysInPlace` | From edge tile facing wall, W → position unchanged, result.Success = false |
| `MovementHandler_RotateLeft_ChangesFacing90` | From facing North, A → facing West |
| `MovementHandler_RotateRight_ChangesFacing90` | From facing North, D → facing East |
| `MovementHandler_MultipleMoves_QueueProcessesInOrder` | Queue 2 forward moves → tick processes both → position advanced 2 tiles |

**Branch:** `feat/movement-tests`

### T5: Tick Loop EditMode Tests
Create in `Assets/Tests/EditMode/`:

| Test Name | What It Verifies |
|---|---|
| `GameTick_WithNoRequests_TickDoesNothing` | Tick runs with empty queue → no state change |
| `GameTick_WithQueuedRequest_ProcessesAndClearsQueue` | Enqueue 1 move → tick → queue empty, position changed |
| `GameTick_TickRate_Is20Hz` | `Time.fixedDeltaTime == 0.05f` after setup |
| `GameTick_MultipleTicks_ProcessSequentially` | 3 ticks with 1 request each → all 3 processed in order |
| `GameTick_InvalidMove_DoesNotChangeState` | Move into wall → tick → position unchanged, state snapshot reflects failure |

**Branch:** `feat/tick-tests`

---

## Integration & Coordination

### Coordinate with QA/Test
- After T1 is done: notify QA/Test so they can start T6 (CI) and T7 (test scaffold)
- After T2+T3 are done: QA/Test writes T8 (integration smoke test)
- Your test files (T4, T5) should use `TestGridBuilder` fixture that QA/Test creates in T7
  - If QA/Test hasn't created `TestGridBuilder` yet, create your own minimal version and refactor after T7 merges

### Acceptance Criteria Summary
- [ ] T1: Project opens in Unity 6 LTS Editor, folder structure exists, TestGrid.unity loads, walls block
- [ ] T2: WASD moves/rotates champion on grid, walls/edges block, camera follows, no free locomotion
- [ ] T3: FixedUpdate at 0.05s, queued requests process per tick, invalid moves rejected, state snapshots emitted
- [ ] T4: All 5 movement tests pass in EditMode
- [ ] T5: All 5 tick tests pass in EditMode
- [ ] All code in correct folders per CLAUDE.md
- [ ] V0 exception note present on tick/tick-adjacent files
- [ ] No `GameObject.Find`, no static singletons, no LINQ in tick code, no `Random.Range`
- [ ] PascalCase, `_camelCase`, one class per file, filename matches class

### PR Requirements
Each sub-task gets its own PR. PR title format: `[V0-001] {short summary}`. Description must include: What changed, Why, How to test (actionable steps for a non-programmer), Performance impact, V0 exception note if applicable.
