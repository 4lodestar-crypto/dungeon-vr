# Networking Constraints (V0 Draft)

> **Living Document** — Updated for each V release. This draft captures the networking architecture decisions made during V0 design and identifies which constraints are temporary V0 compromises vs. permanent architecture mandates.

---

## Server-Authoritative Model

### Definition
The server owns **all** gameplay state. The client is a **renderer and input collector only** — it never simulates gameplay, never predicts outcomes, and never applies unvalidated state changes.

| Who | Owns | Trust Level |
|-----|------|-------------|
| **Server** | GameState, ChampionState, GridData, RNG seed, tick loop | **Authoritative** |
| **Client** | Input collection, camera pose, VFX timing, UI state | **Untrusted** — validated on server |

### Request → Validate → Apply → Emit
This four-phase pipeline is enforced by `IMovementRequestHandler` and is the **only** path by which player actions affect game state:

```
Player Input → MovementRequest → [Network Boundary in V1+] → Validate → Apply → Emit MovementResult
```

- **Request**: `MovementRequest` struct (readonly) submitted to `GameServer`
- **Validate**: Bounds check, collision check, cooldown check, turn-order check
- **Apply**: Mutate `GameState` and `ChampionState` in-place
- **Emit**: `MovementResult` struct (readonly) broadcast to all clients

### V0 In-Process Concern
In V0, the server and client run in the same process. The `GameServer` MonoBehaviour calls `IMovementRequestHandler.Validate()` and `.Apply()` synchronously. This is **intentionally designed** so that the validate-apply-emit pipeline is identical to the network-transport version — the transport layer is a drop-in replacement.

**Do not** introduce direct state access shortcuts in V0 "because it's in-process." Every V0 code path must assume the server is a remote authority.

---

## Determinism Rules

Deterministic simulation is **required** for:
- Reproducible replays and debugging
- Seamless state-sync when late-joining players connect (V3+)
- Consistent behaviour across different tick rates (if rate-matching is needed in V4+)

### Fixed Tick Rate
- **20 Hz** (`GameConstants.TICK_RATE = 20`) — 50 ms per tick
- All gameplay logic runs on this tick boundary
- Rendering and input sampling are decoupled (may run at display Hz)

### Forbidden APIs in Gameplay Logic
| Forbidden | Reason | Replacement |
|-----------|--------|-------------|
| `Time.deltaTime` | Non-deterministic across frame rates | `GameConstants.TICK_RATE`, tick counter |
| `Time.time` | Ties logic to wall clock | `GameState.TickNumber` |
| `UnityEngine.Random` | Different RNG per platform, non-reproducible | `System.Random` with server-controlled seed |
| `DateTime.Now` / `Stopwatch` | Wall-clock drift, non-deterministic replay | Tick counter in `GameState` |

### Seeded RNG
- `GameState` carries a `System.Random` instance seeded by the server at game start
- `System.Random` is used for **all** gameplay randomness: damage variance, ability targeting, proc chances
- Seed is serialized and sent to late-joining clients for deterministic catch-up
- **V0 exception**: No seeded RNG handler exists yet in `GameServer`. See `docs/design/deferred-netcode-work.md`.

### Ordered Collections — No Dictionary Keys / HashSet Iteration
Dictionary and HashSet iteration order is **not guaranteed** across Mono/.NET versions. For deterministic iteration:
- Use `List<T>` or arrays for state that requires ordered access
- If dictionary lookup is needed for random access, maintain a parallel ordered list of keys
- `foreach` over `Dictionary.Keys` or `HashSet` is forbidden in tick code

---

## Serialization Rules

### State Marking
- `GameState`, `ChampionState`, `GridData` all marked `[System.Serializable]`
- All nested data types must also be `[Serializable]`

### Type Restrictions Inside State Objects
| Type | Allowed | Notes |
|------|---------|-------|
| `int`, `float`, `bool`, `enum` | ✅ Yes | Blittable |
| `Vector3`, `Vector2Int` | ✅ Yes | Marked `[Serializable]` |
| Custom `struct` | ✅ Yes | Must be `[Serializable]` |
| `string` | ⚠️ Conditionally | Diagnostic/logging only; **not** in per-tick state mutations |
| `List<T>` | ✅ Yes | Ordered, deterministic |
| `Dictionary<K,V>` | ⚠️ Conditionally | Allowed in config/static data; **forbidden** in per-tick state if iterated |
| `MonoBehaviour` | ❌ No | Never inside `[Serializable]` state |
| `GameObject` | ❌ No | Never inside `[Serializable]` state |
| `Transform` | ❌ No | Never inside `[Serializable]` state |
| `UnityEngine.Random` | ❌ No | Non-deterministic |

### Request/Result Patterns
- `MovementRequest` — `readonly struct`
- `MovementResult` — `readonly struct`
- All request/result types are `[Serializable]` and contain only blittable fields

### Serialization Formats (Future Roadmap)
| Release | Debug Format | Release Format |
|---------|-------------|----------------|
| V0 | N/A (in-process) | N/A (in-process) |
| V1 | JSON | Binary (custom or MessagePack) |
| V2+ | JSON with schema validation | Binary with optional compression |

---

## Static vs Instance

### Rule
**No static singletons for player-specific state.**

### What Is Allowed
- `GameConstants` — static class with `const` values. Read-only, shared, safe.
- `Pool<GameObject>` — instance-based object pools held by the owning system manager.
- Reference-passing via `MonoBehaviour` fields assigned in `Start()` or via inspector.

### What Is Forbidden
- `public static ChampionState CurrentChampion` — makes it impossible to support multiple players/champions.
- `public static GameServer Instance` — prevents multiple game instances, breaks test isolation.
- `FindObjectOfType<GameServer>()` in hot paths — use `[SerializeField] private GameServer gameServer` and assign in inspector/DI.

### V0 Exception: InputQueueBridge
`InputQueueBridge` uses a static-like access pattern (`InputQueueBridge.Instance`) in V0. This is a **V0-EXCEPTION** and must be refactored to proper dependency injection in V2.

> **Deferred**: See `docs/design/deferred-netcode-work.md` — entry: "Singletons in V0 code"

### V0 Exception: GameServer Lookup
`GameServer` is a `MonoBehaviour` found by serialized reference. In V0, it is placed on the same GameObject as the client systems. **DO NOT** use `GameObject.Find("GameServer")` or `FindObjectOfType<GameServer>()` — assign via inspector. In V1, a DI container replaces direct references.

---

## Allocation Guidelines

### Zero-Allocation Tick Code
The tick loop (20 Hz) must produce **zero managed heap allocations per tick** after warmup. Use:

| Technique | When |
|-----------|------|
| Pooled `List<T>` | Any temporary list in validate/apply |
| Pooled `Queue<T>` | Input queue processing |
| `NativeArray<T>` | Burst-compatible paths (future) |
| `struct` returns | Avoid heap-allocated class instances |
| Pre-allocated arrays | Fixed-size lookup tables, grid data |

### Forbidden in Tick Code
- `new List<T>()` or `new Dictionary<K,V>()` — use pooled collections
- LINQ queries (`Select`, `Where`, `ToList`, `Any`, `All`) — hidden allocations
- String concatenation (`+` operator, `StringBuilder` allocation per frame) — use string.Format only in debug/non-tick paths
- Boxing (`(object)myStruct`, `int.ToString()`) — use explicit formatting only in debug paths

---

## V0 Exceptions

These are **documented, time-limited exceptions** to the constraints above. Each carries an expiry version after which it becomes a blocker bug.

| Exception | File / System | Expires | Reason |
|-----------|--------------|---------|--------|
| In-process server | `GameServer` | **V1** | No network transport yet; server runs synchronously in the same process |
| `InputQueueBridge` singleton | `InputQueueBridge` | **V2** | DI container not set up; static access pattern for V0 convenience |
| Direct `ChampionState` read in camera | `PlayerCameraController` | **V2** | State snapshot/broadcast system not built; camera reads live state directly |
| `GameObject.Find` / `FindObjectOfType` | Various V0 scripts | **V1** | DI container in V1 replaces all find-based lookups |
| No seeded RNG handler | `GameServer` | **V1** | RNG integration deferred; see deferred work doc |
| No serialization test suite | `GameState` / tests | **V1** | Round-trip serialization tests not yet written |
| Hardcoded grid data | `GridData` | **V1** | Level/Content pipeline not built; grid is hardcoded |

---

## Deferred Work Pointer

All V0 deferred networking and architecture items are tracked in:

➡ **`docs/design/deferred-netcode-work.md`**

Check that document before implementing any V0 networking feature — it may already be deferred with a plan.
