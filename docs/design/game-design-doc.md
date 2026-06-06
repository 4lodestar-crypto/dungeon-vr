# Game Design Document (v0.1)

**Working title:** Dungeon VR
**Platform:** Windows Desktop (V0–V1 development target), Meta Quest 3 / 3S / Pro (V2+)
**Engine:** Unity 6 LTS (desktop) + Meta XR SDK (V2+)
**Players:** 1 (V1–V3), up to 4 co-op (V4+)
**Inspiration:** Classic grid-based first-person dungeon crawlers of the late 80s and early 90s
**Status:** Living document. Updated when design changes. Andrew is the design authority.

---

## The one-sentence pitch

A VR dungeon crawler where your hands physically reach to your hip for your sword, trace runes in the air to cast spells, and grab torches off the wall — exploring a tile-by-tile dungeon alone or with up to three friends.

## Why VR makes this game

Old grid-based crawlers had clunky inventory and abstract spell systems because the keyboard and mouse demanded abstraction. In VR, that abstraction disappears:

- **Inventory is physical.** Your weapon hangs at your hip. Your scrolls are in a chest pouch. Your backpack is reached over your shoulder. No menus.
- **Spells are gestural.** Trace a rune in the air. Combine runes for compound effects. Bad handwriting fails the spell.
- **Combat is embodied.** Swing your arm to swing your sword. Block with a shield held in your off hand. Distance and reach matter.
- **Exploration is presence.** A torch on the wall actually lights your face. A door creaks open as you push it. A monster's growl comes from the correct direction.

## Core loop (V1)

1. Enter a room. Hold up a torch to see.
2. Search the room — open chests, check alcoves, read inscriptions.
3. Encounter a monster. Combat it with weapon, spell, or evasion.
4. Solve the room's puzzle (pressure plate, hidden lever, key in another room).
5. Open the door to the next room.
6. Reach the floor's exit. Descend to the next floor (V2+).

## The champion (V1)

V1 has **one champion**: a pre-defined adventurer with balanced stats. The character-selection / Hall of Champions mechanic comes in V1's later milestones — you walk into a chamber of mirrors and physically touch a portrait to choose your character.

Champion stats:
- **Health** — physical damage absorption
- **Stamina** — for sustained swings, blocks, sprinting
- **Mana** — spell casting resource
- **Skill levels** — Fighter, Ninja, Priest, Wizard. Each levels independently based on what the player does.

The skill-by-doing system is core to the original genre and we keep it. Swing a sword, gain Fighter. Cast a spell, gain Wizard. This makes player choices visible in their character.

## Combat (V1)

- **Melee:** swing the controller, the in-game weapon swings. Speed and arc matter for damage. Hitting a wall costs stamina and may stagger you.
- **Ranged:** throw daggers, draw a bow. Physical aim, physical release.
- **Magic:** trace one or more runes in the air. Each rune is a syllable. Compound combinations are spells. Bad tracing fizzles the spell and costs mana.
- **Defense:** block with a shield, dodge by physically stepping aside, parry with timing. Some monsters require specific defenses.

## Movement

- **Tile-by-tile.** Thumbstick forward = move one tile forward. Snap turn = rotate 90°.
- **Smooth turn** available as a comfort option.
- **No free movement.** This is a design commitment, not a limitation — it makes spatial puzzles work and netcode tractable.

## What makes a floor

Each floor is a hand-crafted (V1–V5) or procedurally generated (V6+) set of tiles. A floor has:

- A start (where the player arrives)
- An exit (stairs to the next floor or end-of-game portal)
- Rooms separated by walls and doors
- Items, monsters, traps, puzzles, lore
- An overall theme (Floor 1 = Hall of Champions / introductory crypt)

## What this game is NOT

To prevent scope creep and keep the project shippable, here is what we explicitly do not do:

- Not an action RPG. No real-time twitch combat outside the tick system.
- Not open-world. The dungeon is the world.
- Not a graphics showcase. Stylized, readable, mobile-VR-appropriate art.
- Not multiplayer in V1–V3. We architect for it, we do not build it yet.
- Not a copy of any specific existing game. We are inspired by a genre, not deriving from a single title.

## What separates us from the inspiration

By V6 (procgen) and V8 (live), the following must be original:

- Monster designs, names, sounds, lore
- Spell system runes and naming
- Champion archetypes and visual designs
- World setting and lore
- UI and HUD elements
- Music and ambience

Spiritually inspired by the grid-based crawler tradition. Visually and narratively our own. This is a legal requirement before V7 beta, not just an aesthetic preference.

## Open design questions (need Andrew's call)

1. **Party composition for multiplayer (V4+):** does each player control one champion and they all occupy the same tile as a party? Or does each player occupy their own tile and the party moves as a formation? This is the single biggest unresolved design question and affects nearly every system.
2. **Permadeath?** Original inspiration had resurrection mechanics in the Hall. Do we keep that, do a softer "down but not dead," or full permadeath?
3. **Save model:** save anywhere, save at altars, or auto-save only?
4. **Difficulty:** one fixed difficulty (like the original) or selectable?
5. **Length of full game:** how many floors total in V3? The genre originally shipped 14.

These do not need to be answered in V0. They need to be answered before they block work.
