using DungeonVR.Shared;
using UnityEngine;

namespace DungeonVR.Server
{
    /// <summary>
    /// Interface for systems that participate in the tick loop.
    /// Each tick, the GameTick calls all registered ITickableSystems.
    /// V0-EXCEPTION: refactor through proper server layer in V1.
    /// </summary>
    public interface ITickableSystem
    {
        /// <summary>
        /// Called once per tick by GameTick.
        /// </summary>
        /// <param name="tickNumber">Current tick number (monotonically increasing).</param>
        /// <param name="state">The current game state — systems may read and modify state.</param>
        void OnTick(int tickNumber, GameState state);
    }
}
