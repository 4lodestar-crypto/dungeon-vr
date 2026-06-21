using DungeonVR.Shared.Data;
using System.Collections.Generic;

namespace DungeonVR.AI.Interfaces
{
    /// <summary>
    /// Contract for grid-based pathfinding used by monster AI.
    /// All implementations must be deterministic, allocation-aware, and budget-interruptible.
    /// </summary>
    public interface IGridPathfinder
    {
        /// <summary>
        /// Attempts to find a path from start to target on the walkable grid.
        /// </summary>
        /// <param name="start">Starting tile.</param>
        /// <param name="target">Destination tile.</param>
        /// <param name="maxSteps">Maximum path length before giving up. Prevents infinite searches.</param>
        /// <param name="path">Resulting path (excluding start, including target) or null if no path found.</param>
        /// <returns>True if a valid path was found.</returns>
        bool TryFindPath(TileCoord start, TileCoord target, int maxSteps, out List<TileCoord> path);

        /// <summary>
        /// Returns the heuristic cost estimate between two tiles (Manhattan distance).
        /// Must be admissible (never overestimate) for A* correctness.
        /// </summary>
        float GetHeuristicCost(TileCoord from, TileCoord to);

        /// <summary>
        /// Invalidates all cached paths that traverse through or end at the given tile.
        /// Called when a door closes, a tile becomes blocked, or terrain changes.
        /// </summary>
        void InvalidateCache(TileCoord tile);
    }
}
