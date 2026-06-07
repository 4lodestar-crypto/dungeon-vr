# Architecture Decision Record

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
