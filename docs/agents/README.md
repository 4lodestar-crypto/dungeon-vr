# Agent Roster

This is the team. Each agent has its own file in this folder defining its role, directory ownership, rules, and escalation paths. Agents read their own file at the start of every session. The Orchestrator reads them all.

## The roster

| Agent | File | Owns | Active in |
|---|---|---|---|
| Orchestrator | `orchestrator.md` | Coordination, PR review, decision log | All versions |
| Gameplay Systems | `gameplay-systems.md` | Core mechanics, server-side logic | All versions |
| VR Interaction | `vr-interaction.md` | Meta XR SDK, input, comfort | All versions |
| AI / Monster | `ai-monster.md` | Behaviors, pathfinding | V1+ |
| Level / Content | `level-content.md` | Dungeon data, editor tools, procgen | All versions |
| Networking Architect | `networking-architect.md` | Server pattern, future netcode | Advisory V0–V3, active V4+ |
| QA / Test | `qa-test.md` | Tests, CI, regression prevention | V1+ |
| Art / Asset | `art-asset.md` | Importing, optimizing for Quest | V1+ (mostly V2+) |

## The human

**Andrew** is the product owner, designer, and playtester. He is not a programmer. He makes design decisions, approves architectural choices, plays the builds, and reports what's wrong. He merges PRs. He sources art assets.

## How communication works

- Andrew talks to the Orchestrator.
- The Orchestrator talks to specialists.
- Specialists coordinate with each other on integration points, but never override the Orchestrator.
- Anyone can escalate to Andrew when blocked.

## Session start checklist (every agent)

1. Read `/CLAUDE.md`.
2. Read your own role file in `/docs/agents/`.
3. Read the active ticket and any linked design docs.
4. Confirm understanding before writing code.
