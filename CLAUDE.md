# Project: Dungeon VR (working title)

You are working on a VR dungeon crawler for Meta Quest, spiritually inspired by classic grid-based dungeon crawlers. This file is read by every agent at the start of every session. Read it fully before doing anything else.

## Current platform strategy (IMPORTANT)

**V0–V1 target: Windows Desktop (Unity Editor + standalone build).**
We build gameplay, dungeon generation, AI, and all core systems on desktop first so iteration is fast. Keyboard/mouse and gamepad are the input methods during this phase.

**V2+ target: Meta Quest (VR).**
Once the game plays well on desktop, we enable the Meta XR SDK, replace keyboard/mouse input with hand tracking/controllers, and tune for Quest mobile performance. The Unity project supports both build targets from the same codebase.

The VR interaction layer (hand tracking, grabbable items, gestural spells) is written when we flip to Quest — NOT during V0–V1. For now, map all VR actions to keyboard/mouse equivalents.

This does NOT change the core architecture (server-authoritative, grid movement, fixed tick). Those rules apply to both targets.

## What this project is

A first-person, grid-based dungeon crawler built initially for Windows Desktop, targeting Meta Quest 3+ for full VR. Built in Unity 6 LTS. The player controls one champion. Multiplayer support for up to 4 players is planned (V4+) but is NOT in scope for V1–V3.

This is a passion project. The human (Andrew) is the product owner and playtester. Andrew is not a programmer. Agents do the coding. Andrew reviews, tests, and decides.

## Core architectural rules — non-negotiable

1. **Server-authoritative from day one.** Even in single-player, all game state lives in a "server" layer. The client requests actions; the server validates and applies them. In V1 the server runs locally in-process. In V4+ it runs over the network. Do not write code that assumes the player IS the source of truth. If you find yourself writing `player.health -= damage` directly from input code, stop and route it through the server layer.

2. **Grid-based discrete movement.** The world is a grid of 3m x 3m tiles. The champion occupies one tile and faces one of four cardinal directions. Movement is tile-by-tile, turning is 90 degrees. Do not implement free-locomotion movement. This is a comfort decision and a netcode decision.

3. **No frame-rate-dependent logic.** Quest 3 targets 90 or 120 Hz. PCVR targets vary. All gameplay logic ticks on a fixed timestep (we will use Unity's FixedUpdate or a custom tick system, decided in V0). Never put gameplay logic in Update().

4. **Performance budget.** Quest 3 is a mobile chip. Target: 90 FPS, 72 draw calls or fewer per frame, 100k triangles visible, no dynamic shadows on dungeon geometry, baked lighting wherever possible. Every PR must include a note on performance impact.

5. **Comfort first.** Snap turn by default, smooth turn as an option. No forced camera movement. Vignette during any locomotion. The player can stand or sit — never assume room-scale.

## Folder structure

```
/Assets
  /Scripts
    /Gameplay      — owned by Gameplay Systems agent
    /VR            — owned by VR Interaction agent
    /AI            — owned by AI/Monster agent
    /Level         — owned by Level/Content agent
    /Net           — owned by Networking Architect (mostly stubs in V1)
    /Server        — the authoritative game state layer
    /Shared        — types/interfaces used across systems
  /Prefabs
  /Scenes
  /Data            — JSON/ScriptableObjects for levels, monsters, items
  /Art             — imported assets
/docs
  /agents          — per-agent role definitions
  /design          — design docs, GDD, version roadmap
  /decisions       — ADRs (architecture decision records)
```

## Naming conventions

- C# classes: PascalCase (`ChampionController`)
- Methods: PascalCase (`ApplyDamage`)
- Private fields: `_camelCase` with underscore
- Public fields: avoid; use properties
- Files: one class per file, filename matches class name
- Unity scenes: `Floor01_HallOfChampions.unity`
- Prefabs: `Champion_Default.prefab`, `Monster_Screamer.prefab`

## Workflow

1. The Orchestrator agent reads a ticket from GitHub Issues.
2. Orchestrator drafts a one-paragraph plan and assigns to a specialist agent.
3. Specialist agent works on a feature branch named `feat/<short-description>`.
4. Specialist opens a PR. The PR description must include: what changed, why, how to test in VR, and performance impact.
5. Andrew reviews and merges, or asks for changes.
6. Never push directly to `main`.

## What to do when uncertain

Ask. Do not guess on architectural decisions, monster behavior intent, or what the player should experience. Andrew is the design authority. Open a question in the PR or in the ticket and wait. Guessing wastes more time than asking.

## What to never do

- Never write code for V2+ features when working on V1 tickets. Scope creep is the enemy.
- Never copy code or assets from the original Dungeon Master game or its descendants. This project is inspired by, not derived from.
- Never commit binary assets larger than 100 MB without Git LFS.
- Never disable a failing test to make CI pass. Fix the code or fix the test.
- Never use `GameObject.Find` or `FindObjectOfType` in hot paths.
