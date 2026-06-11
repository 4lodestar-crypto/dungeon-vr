# Project: Dungeon VR

You are working on a VR dungeon crawler for Meta Quest, spiritually inspired by classic grid-based dungeon crawlers. Read this fully before doing anything else.

## Current platform strategy

**V0–V1 target: Windows Desktop (Unity Editor + standalone build).**
We build gameplay, dungeon generation, AI, and all core systems on desktop first so iteration is fast. Keyboard/mouse and gamepad are the input methods during this phase.

**V2+ target: Meta Quest (VR).**
Once the game plays well on desktop, we enable the Meta XR SDK, replace keyboard/mouse input with hand tracking/controllers, and tune for Quest mobile performance. The Unity project supports both build targets from the same codebase.

The VR interaction layer (hand tracking, grabbable items, gestural spells) is written when we flip to Quest — NOT during V0–V1. For now, map all VR actions to keyboard/mouse equivalents.

This does NOT change the core architecture (server-authoritative, grid movement, fixed tick). Those rules apply to both targets.

## What this project is

A first-person, grid-based dungeon crawler built initially for Windows Desktop, targeting Meta Quest 3+ for full VR. Built in Unity 6 LTS. The player controls one champion. Multiplayer support for up to 4 players is planned (V4+) but is NOT in scope for V1–V3.

The human (Andrew) is the product owner and playtester. Andrew is not a programmer. Agents do the coding. Andrew reviews, tests, and decides.

## Core architectural rules — non-negotiable

1. **Server-authoritative from day one.** Even in single-player, all game state lives in a "server" layer. The client requests actions; the server validates and applies them. In V1 the server runs locally in-process. In V4+ it runs over the network. Do not write code that assumes the player IS the source of truth.

2. **Grid-based discrete movement.** The world is a grid of 3m x 3m tiles. The champion occupies one tile and faces one of four cardinal directions. Movement is tile-by-tile, turning is 90 degrees. Do not implement free-locomotion movement. This is a comfort decision and a netcode decision.

3. **No frame-rate-dependent logic.** All gameplay logic ticks on a fixed timestep (20 Hz FixedUpdate). Never put gameplay logic in Update().

4. **Performance budget.** Quest 3 is a mobile chip. Target: 90 FPS, 72 draw calls or fewer per frame, 100k triangles visible, no dynamic shadows on dungeon geometry, baked lighting wherever possible. Every PR must include a note on performance impact.

5. **Comfort first.** Snap turn by default, smooth turn as an option. No forced camera movement. Vignette during any locomotion. The player can stand or sit — never assume room-scale.

## Folder structure

```
/Assets
  /Scripts
    /Gameplay      — owned by Gameplay Systems agent
      /Components  — PlayerInput, PlayerCamera, GridData
      /Level       — IDungeonGenerator, RandomRoomGenerator
      /Logic       — MovementHandler, ChampionState
    /VR            — owned by VR Interaction agent (stubs in V0)
    /AI            — owned by AI/Monster agent (stubs in V0)
    /Level         — owned by Level/Content agent (DungeonGridSpawner)
    /Net           — owned by Networking Architect (stubs in V0)
    /Server        — authoritative game state layer
    /Shared        — types/interfaces used across systems
      /Requests    — MovementRequest etc.
      /Results     — MovementResult etc.
  /Prefabs
  /Scenes
  /Data
  /Art
  /Editor
  /Tests
    /EditMode
      /Fixtures
      /Systems
      /Integration
    /PlayMode
/docs
  /agents          — per-agent role definitions
  /design          — GDD, V0-001 plan, decisions log
```

## Naming conventions

- C# classes: PascalCase (`ChampionController`)
- Methods: PascalCase (`ApplyDamage`)
- Private fields: `_camelCase` with underscore
- Public fields: avoid; use properties
- Files: one class per file, filename matches class name
- Unity scenes: `Floor01_HallOfChampions.unity`
- Prefabs: `Champion_Default.prefab`, `Monster_Screamer.prefab`

## Current V0-001 scope

**Deliverable:** Andrew can launch the Unity project, see a generated dungeon with rooms + corridors, walk a champion tile-by-tile with WASD, turn 90° with A/D, be blocked by walls, and observe the 20 Hz tick loop processing movement requests.

**Key specs:**
- 3m tile pitch (`const float TILE_SIZE = 3.0f`)
- 20 Hz FixedUpdate tick
- Procedural dungeon generation (rooms + corridors, connectivity guaranteed)
- V0-EXCEPTION comment on any file bypassing server layer
- No VR packages or Meta XR SDK references

## Workflow

1. Orchestrator reads the ticket and assigns tasks to specialist agents.
2. Specialist works on a feature branch named `feat/<short-description>`.
3. Specialist opens a PR. PR description includes: what changed, why, how to test, performance impact.
4. Andrew reviews and merges, or asks for changes.
5. Never push directly to `main`.

## What to never do

- Never write code for V2+ features when working on V1 tickets. Scope creep is the enemy.
- Never use `GameObject.Find` or `FindObjectOfType` in hot paths.
- Never disable a failing test to make CI pass. Fix the code or fix the test.
- No LINQ in tick code, no `Random.Range`, no static singletons for player state.
