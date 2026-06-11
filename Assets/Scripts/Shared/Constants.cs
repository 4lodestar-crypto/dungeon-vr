using UnityEngine;

namespace DungeonVR.Shared
{
    /// <summary>
    /// Shared constants across all Dungeon VR systems.
    /// V0-EXCEPTION: TILE_SIZE, TICK_RATE remain here; server-layer config in V1.
    /// </summary>
    public static class GameConstants
    {
        /// <summary>
        /// Grid tile pitch in world units. Every tile is TILE_SIZE x TILE_SIZE.
        /// </summary>
        public const float TILE_SIZE = 3.0f;

        /// <summary>
        /// Server-authoritative tick rate in Hz. All gameplay logic runs at this rate.
        /// </summary>
        public const int TICK_RATE = 20;

        /// <summary>
        /// Duration of one tick in seconds (1 / TICK_RATE).
        /// </summary>
        public const float TICK_DELTA = 1.0f / TICK_RATE;

        /// <summary>
        /// Champion eye height above the tile surface.
        /// </summary>
        public const float EYE_HEIGHT = 1.7f;

        /// <summary>
        /// Number of cardinal directions.
        /// </summary>
        public const int DIRECTION_COUNT = 4;
    }
}
