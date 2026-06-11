# Deferred Netcode Work (V0 Draft)

> **Entries added during V0-001.** This document captures networking and architecture tasks that were identified during V0 design but consciously deferred. Each entry includes priority, target version, and a brief rationale so future implementors understand the trade-off.

---

## P1: Singletons in V0 code

| Field | Value |
|-------|-------|
| **Priority** | P1 (Must fix before multiplayer) |
| **Target** | V2 |
| **Owner** | Gameplay Systems |
| **Dependencies** | DI container implementation |

**Description**
`InputQueueBridge` uses a static-like access pattern (`InputQueueBridge.Instance`) in V0. This makes it impossible to:
- Host multiple game sessions in the same process
- Write isolated unit tests that inject a mock input queue
- Transition to a remote-server architecture cleanly

**Proposed Solution**
- Implement a lightweight DI container or service locator (V1)
- Register `IInputQueue` as a scoped service
- `InputQueueBridge` resolves via constructor injection, not static access
- Remove `// V0-EXCEPTION` comment markers

**Status**
Deferred to V2. Blocked by V1 DI container.

---

## P1: Direct state access by camera

| Field | Value |
|-------|-------|
| **Priority** | P1 (Must fix before multiplayer) |
| **Target** | V2 |
| **Owner** | VR Interaction |
| **Dependencies** | State snapshot system, broadcast system |

**Description**
`PlayerCameraController` reads `ChampionState` fields directly from the authoritative `GameServer.GameState`. This works in V0 because server and client are in-process, but in a networked architecture:
- The camera would read **live mutable state** that the server is simultaneously writing
- There is no interpolation buffer for smooth movement at 20 Hz ticks
- Late-joining players have no snapshot to interpolate from

**Proposed Solution**
- Implement a state snapshot system (`IGameStateSnapshotProvider`)
- `GameServer` emits immutable snapshots at the end of each tick
- `PlayerCameraController` reads from the most recent snapshot, interpolating toward the next
- Snapshots include tick number for ordering and interpolation

**Status**
Deferred to V2. V0 camera reads live state with `// V0-EXCEPTION` marker.

---

## P2: No serialization test suite

| Field | Value |
|-------|-------|
| **Priority** | P2 (Should fix before release) |
| **Target** | V1 |
| **Owner** | QA / Test |
| **Dependencies** | None (can be written independently) |

**Description**
`GameState`, `ChampionState`, and `GridData` are all marked `[System.Serializable]`, but no round-trip serialization tests exist. This means:
- A field added to `GameState` that is not `[Serializable]` will fail silently at runtime
- Binary format changes may break save/load and network sync
- No regression detection for serialization format changes

**Proposed Solution**
- Write an EditMode test suite in `Tests/EditMode/Serialization/`
- For each state type:
  1. Construct a representative instance with non-default values
  2. Serialize to JSON (or binary via `BinaryFormatter` in V1)
  3. Deserialize back
  4. Assert every field matches
- Add a test that verifies all `[Serializable]` types have no non-serializable fields (reflection-based)

**Status**
Deferred to V1. Not blocking V0.

---

## P1: Hardcoded grid data

| Field | Value |
|-------|-------|
| **Priority** | P1 (Must fix before level design pipeline) |
| **Target** | V1 |
| **Owner** | Level / Content |
| **Dependencies** | JSON file pipeline, content loading system |

**Description**
`GridData` in V0 is hardcoded in a script — tile positions, walkability, and room metadata are set in C#. This makes:
- Level iteration require code changes and recompilation
- Multiple levels impossible without code duplication
- Network sync of level data non-trivial — level data must be authored and loaded, not compiled

**Proposed Solution**
- Define a JSON schema for grid data (tile coordinates, walkability, room metadata, spawn points)
- Write a `LevelLoader` that reads JSON from `Resources/` or `StreamingAssets`
- `GameServer` loads `GridData` from the level file at game start
- Level/Content team authors levels in JSON (or via a future editor tool)

**Status**
Deferred to V1. V0 uses hardcoded grid with `// V0-EXCEPTION` marker.

---

## P2: Tick system not yet RNG-integrated

| Field | Value |
|-------|-------|
| **Priority** | P2 (Should fix before any RNG-dependent gameplay) |
| **Target** | V1 |
| **Owner** | Gameplay Systems |
| **Dependencies** | None (independent addition) |

**Description**
V0 `GameServer` has no seeded RNG handler. `System.Random` is not yet instantiated or stored in `GameState`. In V0, if any gameplay system calls `UnityEngine.Random`, it will be non-deterministic across runs and platforms.

**Proposed Solution**
- Add `ISeededRng` interface with `Next()`, `Next(min, max)`, `NextFloat()` methods
- Add `System.Random Rng` field to `GameState`
- `GameServer` creates and seeds the RNG at game start (`new System.Random(serverProvidedSeed)`)
- All gameplay systems receive `ISeededRng` via constructor injection
- Add deterministic RNG test: same seed produces same sequence every time

**Status**
Deferred to V1. V0 gameplay should avoid randomness; if unavoidable, use `System.Random` manually but without integration into `GameState`.

---

## Summary Table

| ID | Priority | Item | Target | Owner |
|----|----------|------|--------|-------|
| DNW-001 | P1 | Singletons in V0 code (InputQueueBridge) | V2 | Gameplay Systems |
| DNW-002 | P1 | Direct state access by camera | V2 | VR Interaction |
| DNW-003 | P2 | No serialization test suite | V1 | QA / Test |
| DNW-004 | P1 | Hardcoded grid data | V1 | Level / Content |
| DNW-005 | P2 | Tick system not yet RNG-integrated | V1 | Gameplay Systems |
