# AI System Design (V0 Draft — Pre-Implementation)

| Property        | Value                        |
|-----------------|------------------------------|
| **Target**      | V1 implementation            |
| **Status**      | Draft / V0 contract stubs    |
| **Owner**       | AI / Monster Agent           |

---

## 1. Philosophy & Constraints

The AI system is **server-authoritative** and **deterministic**. No AI logic runs inside
`MonoBehaviour.Update`, no physics queries are used for perception, and no
`UnityEngine.Random` calls are ever made. Frame-rate independence is guaranteed by
driving all timers and state transitions from a fixed **server tick rate** (20 Hz,
as defined in `GameConstants.TICK_RATE`).

The core design principle: **AI requests, Gameplay applies**. Monsters never directly
reduce HP, destroy objects, or move transforms. Instead they emit structured request
structs (`AttackRequest`, `MoveRequest`) that the Gameplay Systems layer validates
and applies.

---

## 2. Architecture Overview

### 2.1 Layering

```
┌─────────────────────────────────────┐
│        VR / Presentation Layer      │  Visuals, audio, haptics
├─────────────────────────────────────┤
│         Gameplay Systems            │  Damage resolution, movement, spawning
├─────────────────────────────────────┤
│        AI / Monster Layer           │  State machines, behavior trees, perception
├─────────────────────────────────────┤
│         Server Entity/Grid          │  Tick loop, entity registry, tile grid
└─────────────────────────────────────┘
```

### 2.2 Server-side Monster State

Monster state lives in a **plain C# struct/class** owned by the server entity registry —
not on a `MonoBehaviour`. The `IMonsterState` interface provides a read-only snapshot
for AI evaluators. The authoritative state mutate methods live on the server entity
class (`ServerMonsterEntity`) which is not accessible to AI code directly.

```
IMonsterState (read-only query interface, used by AI evaluators)
       ▲
       │  projects
       │
ServerMonsterEntity (authoritative state, owns HP, position, timers)
```

### 2.3 Grid-based Pathfinding

Pathfinding uses **A\* on the tile grid** (`TileCoord`). No NavMesh, no Physics queries.
The pathfinder reads a cost grid from `IGridQueryService` which surfaces tile weights
(terrain type, blocked-by-entity, etc.). The AI requests a path via the pathfinding
subsystem and receives a `List<TileCoord>` waypoint list.

- **Heuristic**: Octile (diagonal-capable) for 8-directional movement.
- **Cache**: Path results are cached for `PathCacheTtlTicks` (default 30 ticks = 1.5 s).
- **Fail state**: If no path exists, the monster remains in its current state and retries
  next tick (or transitions to Idle).

### 2.4 Per-tick AI Dispatcher

Every server tick, the **AI Dispatcher** iterates alive monsters and calls
`IMonsterBehavior.Tick(context)` on each. The dispatcher enforces a **combined budget
of 0.5 ms** across all monsters. If budget is exhausted, remaining monsters skip
evaluation for that tick (they keep their last action/direction).

**Budget enforcement:**

```
float elapsed = Stopwatch.GetElapsedMs();
foreach (var monster in aliveMonsters)
{
    if (elapsed > 0.5f) break;   // out of budget
    monster.AI.Tick(currentContext);
    elapsed = Stopwatch.GetElapsedMs();
}
```

> **V0 note**: Budget enforcement is defined here but will not be wired until V1.
> V0 stubs accept the `BudgetMs` field in `MonsterContext` but ignore it.

### 2.5 Event-driven Damage Flow

```
MonsterBehavior.Tick()
     │
     ▼
Evaluates → decides to attack
     │
     ▼
Creates AttackRequest (target EntityId, damage, source EntityId)
     │
     ▼
Gameplay Systems validate & apply
     │
     ▼
Damage event published → hurt/death cues fire
     │
     ▼
ServerMonsterEntity.CurrentHP updated → MonsterContext reflects next tick
```

**Key rule**: AI code NEVER calls `entity.TakeDamage()`. It constructs an
`AttackRequest` and hands it off. The Gameplay Systems layer owns damage resolution
(including armor modifiers, invincibility frames, etc.).

---

## 3. Behavior Model — State Machine First

### 3.1 Recommendation

Use a **finite state machine (FSM)** as the primary model for V1. Behavior trees
(using a library like NodeCanvas or a custom tree interpreter) can be added for
complex monsters (Boss archetype) in V2 if needed.

**Rationale:**
- FSM is simpler to implement, debug, and profile.
- Dungeon monsters have a small number of clearly defined states (Idle, Patrol, Alert,
  Attack, Cooldown, Hurt, Death).
- Transitions are triggered by discrete events (player detection, HP threshold, timer
  expiry) which map naturally to FSM transitions.
- Behavior trees shine for complex priority/fallback logic that dungeon monsters
  rarely need.

### 3.2 MonsterStateId Transition Map (Conceptual)

```
                      ┌─── Hurt ───► Death
                      │     │
                      │     ▼
Idle ──► Patrol ──► Alert ──► Attack ──► Cooldown
  ▲         ▲         │         │
  └─────────┴─────────┴─────────┘
```

| Transition | Trigger |
|-----------|---------|
| Idle → Patrol | Timer expiry or spawn activation |
| Patrol → Alert | Player within detection range & line-of-sight |
| Alert → Attack | Path to player found & within attack range |
| Attack → Cooldown | Attack executed |
| Cooldown → Alert | Cooldown timer expires |
| Any → Hurt | Damage received |
| Hurt → Death | HP ≤ 0 |
| Hurt → Alert | Hurt animation/cooldown timer expires |
| Any → Idle | Loss of player detection, patrol reset |

### 3.3 State Implementations (V1)

Each state is a class implementing `IMonsterStateHandler` (V1 interface):

```
interface IMonsterStateHandler
{
    MonsterStateId StateId { get; }
    void OnEnter(MonsterContext ctx);
    void Tick(MonsterContext ctx);
    void OnExit(MonsterContext ctx);
    MonsterStateId EvaluateTransition(MonsterContext ctx);
}
```

**V0 stubs** only define `IMonsterBehavior` with `Tick()` and `Evaluate()` — no
individual state handlers yet.

---

## 4. Spawn Flow

```
Level/Content Tile Data
       │
       ▼
ISpawnPointProvider              <── V1 interface: "give me all spawn points for floor N"
       │
       ▼
AI Spawner
  - reads MonsterSpawnTable for current floor
  - filters entries by floor range
  - weighted random selection (seeded server RNG)
  - builds SpawnRequest for each selected point
       │
       ▼
IMonsterSpawnHandler.TrySpawn()
  - creates ServerMonsterEntity
  - assigns EntityId
  - initialises MonsterDefinition stats
  - schedules activation after SpawnDelayTicks
       │
       ▼
Server Entity Registry           <── holds all active monster entities
```

### 4.1 ISpawnPointProvider (V1 stub contract)

```csharp
public interface ISpawnPointProvider
{
    IReadOnlyList<TileCoord> GetSpawnPoints(int floorDepth);
}
```

Not created in V0 — documented here for V1.

---

## 5. Visual / Audio Cue Spec Pattern

Monster definitions reference cues via **string keys** (`spawnCueKey`, `attackCueKey`,
etc.). The VR / Art layer implements `IMonsterCueProvider` (V1 interface) which
resolves keys to concrete visual, audio, or haptic effects.

```csharp
public interface IMonsterCueProvider
{
    void PlayCue(string cueKey, TileCoord worldPosition, int entityId);
    void StopCue(string cueKey, int entityId);
}
```

**Advantages:**
- AI system is completely decoupled from presentation.
- Cues can be swapped, muted, or overloaded per platform (PC VR vs Quest) without
  touching AI code.
- Testable — cue provider can be a no-op in headless server tests.

---

## 6. Determinism Guarantees

| Concern | Solution |
|---------|----------|
| Random numbers | Seeded `System.Random` from server tick seed. No `UnityEngine.Random`. |
| Timers | Tick-count based (`int remainingTicks`), not `Time.deltaTime`. |
| Perception | Grid queries only — no `Physics.Raycast`, no `OverlapSphere`. |
| Movement | Tile-based path + interpolation calculated from fixed tick. |
| Damage | Resolved server-side with predictable formulas. No floating-point drift. |

**Seeded RNG pattern:**

```csharp
// V1 pattern — not in V0 stubs
var rng = new System.Random(seed: tickSeed ^ monsterEntityId);
float roll = (float)rng.NextDouble();  // [0, 1)
```

---

## 7. V0 Stub Interfaces (Created)

| File | Purpose |
|------|---------|
| `Assets/Scripts/AI/Interfaces/IMonsterBehavior.cs` | Core AI contracts: `IMonsterState`, `IMonsterBehavior`, `MonsterContext`, `SpawnRequest`, `IMonsterSpawnHandler`, `IGridQueryService` (empty stub) |
| `Assets/Scripts/AI/Data/MonsterDefinition.cs` | ScriptableObject definition asset with all stat fields, cue keys |
| `Assets/Scripts/AI/Data/MonsterSpawnTable.cs` | ScriptableObject spawn table with `MonsterSpawnEntry[]` |

---

## 8. V1 Implementation Order (Suggested)

1. **Grid pathfinding** — A\* on `TileCoord` with `IGridQueryService` cost provider.
2. **State machine skeleton** — `IMonsterStateHandler` + dispatcher + first state (Idle).
3. **Patrol & Alert** — path following + player detection queries.
4. **Attack flow** — `AttackRequest` creation, Gameplay Systems integration.
5. **Spawn pipeline** — `ISpawnPointProvider` → `IMonsterSpawnHandler` → entity registry.
6. **Cues** — `IMonsterCueProvider` implementation for VR art layer.
7. **Budget enforcement** — 0.5 ms combined budget in dispatcher.
8. **Determinism pass** — audit all RNG, timer, and perception paths.

---

## 9. Extended GameConstants (Referenced)

```csharp
namespace DungeonVR.Shared
{
    public static class GameConstants
    {
        public const float TILE_SIZE = 3f;         // meters per tile edge
        public const int    TICK_RATE = 20;         // server ticks per second
    }
}
```

```csharp
namespace DungeonVR.Shared.Data
{
    public struct TileCoord
    {
        public int X;
        public int Z;
        // Equality operators, Manhattan/Chebyshev distance helpers, etc.
    }
}
```

These constants live in the `DungeonVR.Shared` assembly and are referenced by the
AI layer. They are **not** duplicated in the AI system.
