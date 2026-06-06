# Ticket V0-001: Hello VR World — End-to-End Pipeline Proof

**Version:** V0
**Priority:** Critical (blocks all other work)
**Owner:** Orchestrator coordinates; specialists assigned below
**Estimated effort:** 1–2 weeks part-time

---

## Goal

Prove that the entire development pipeline works end-to-end before we commit to building actual game content. By the end of this ticket, Andrew should be able to:

1. Put on his Quest 3
2. Launch the build from the Quest app drawer
3. See his hands in a small stone room
4. Walk to a torch on the wall
5. Grab the torch (it lights when held)
6. See the room light up around him
7. Walk to a door
8. Push the door open with his hand
9. See "Hello, Dungeon" written on the wall of the next room

If this works, V1 is "more of the same." If this doesn't work, we have learned what's broken before sinking months into game content.

## Why this ticket exists

Most passion projects fail not because the game is too hard to build, but because the pipeline never works reliably. We're spending V0 making the pipeline boring and reliable so V1+ can be exciting.

## Scope

**In scope:**
- Unity 6 LTS project setup with Meta XR SDK
- Universal Render Pipeline configured for Quest 3 mobile
- One Unity scene with two small rooms separated by a door
- Player rig with hand tracking and controller support
- One grabbable, lightable torch
- One pushable door
- A static "Hello, Dungeon" mesh-text in the second room
- Build script that produces a deployable APK
- GitHub Actions CI that builds the APK on every PR
- Git LFS configured for binary assets

**Out of scope (do not build):**
- Grid-based movement system (V1)
- Inventory body slots (V1)
- Monsters (V1)
- Combat (V1)
- Spells (V1)
- Level data format (V1)
- Server layer (V1) — for V0 we go direct, with a note that V1 will refactor through server
- Anything else

## Architectural exception (V0 only)

Normally per `CLAUDE.md` rule #1, every state change routes through the server layer. For V0, the torch lighting and door opening are direct interactions — no server layer yet. The Gameplay Systems agent should note in their PR: "V1 will refactor torch/door into request → server → state pattern."

This is the only allowed exception. From V1 onward the rule is absolute.

## Assignments

### VR Interaction (lead on this ticket)
- Set up the Meta XR SDK
- Configure the Quest 3 build target
- Implement player rig with hand tracking
- Implement grab interaction for the torch
- Implement push interaction for the door
- Configure comfort settings (snap turn, vignette stubs — not used yet but in place)

### Gameplay Systems
- Implement the torch lighting behavior (when held, the point light turns on)
- Implement the door opening behavior (rotates on hinge when pushed)
- These are direct MonoBehaviours for V0 — see exception above.

### Art / Asset
- Source or place placeholder assets: stone room cubes, torch model (free asset from Unity Asset Store is fine), door model, mesh text
- Configure URP mobile materials
- Set up baked lighting for the static room geometry

### QA / Test
- Set up GitHub Actions CI to build the APK
- Write a smoke test: scene loads, player rig spawns, frame time under 11ms (90 FPS)
- Performance baseline: log draw calls and triangle count

### Networking Architect (advisory)
- Review the V0 exception note
- Write `/docs/design/networking-constraints.md` so V1 can route correctly
- No code

### Level / Content
- Not active in V0. Wait for V1.

### AI / Monster
- Not active in V0. Wait for V1.

## Acceptance criteria

- [ ] Unity 6 LTS project committed to GitHub with Git LFS
- [ ] Meta XR SDK installed and configured for Quest 3
- [ ] URP configured with mobile/Quest preset
- [ ] Build script produces a working APK
- [ ] APK deploys to Quest 3 via developer mode (Andrew confirms)
- [ ] Player can see hands/controllers in VR
- [ ] Player can walk around using thumbstick locomotion (free movement is fine for V0; V1 replaces with grid)
- [ ] Player can grab the torch and it lights
- [ ] Player can push the door and it opens
- [ ] Player can read "Hello, Dungeon" in the second room
- [ ] CI passes on the final PR
- [ ] Frame time under 11ms on Quest 3 (90 FPS)
- [ ] All code in correct folders per `CLAUDE.md`
- [ ] All agent role files exist and were read

## Definition of done

Andrew puts on his Quest. He installs the build via SideQuest or Meta Quest Developer Hub. He launches it. He completes the steps in the Goal section above. He reports back. If everything works, we close this ticket and open V1-001.

## Risks and known issues

- **Meta XR SDK version mismatches** are the most common Quest dev nightmare. Pin the version explicitly in the project and document it. Do not auto-update.
- **Git LFS quota** on free GitHub is 1GB storage + 1GB bandwidth/month. Asset-heavy projects exceed this fast. Decision needed before V2: pay for LFS storage or self-host LFS.
- **Andrew's Quest must be in developer mode.** This requires a Meta organization account. If not done already, do it in week 1.
- **macOS vs Windows for building.** Android builds work from both, but iOS/macOS users hit signing complications. Decide Andrew's primary dev machine in V0 week 1.

## Notes for the Orchestrator

Open this as `V0-001` in GitHub Issues when ready. Break it into sub-issues per specialist. Each specialist's work happens on a feature branch off `develop`. When all sub-issues are merged into `develop` and the acceptance criteria check out on Andrew's Quest, merge `develop` into `main` and tag `v0.1.0`.
