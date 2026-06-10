using DungeonVR.Shared;
using UnityEngine;

namespace DungeonVR.Server
{
    /// <summary>
    /// Holds the full authoritative game state for a single tick cycle.
    /// In V0, this is a simple container for champion state + seeded RNG.
    /// V0-EXCEPTION: refactor through proper server layer in V1.
    /// </summary>
    public class GameState
    {
        /// <summary>The champion's current state (position, facing).</summary>
        public ChampionState Champion { get; set; }

        /// <summary>
        /// Seeded RNG for deterministic debug and replay.
        /// Seed = 42 for V0.
        /// </summary>
        public System.Random Rng { get; }

        public GameState(ChampionState champion, int seed = 42)
        {
            Champion = champion;
            Rng = new System.Random(seed);
        }
    }
}
