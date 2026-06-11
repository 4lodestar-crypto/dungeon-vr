using UnityEngine;

namespace DungeonVR.Shared.Interfaces
{
    /// <summary>
    /// Interface for querying grid tile data.
    /// Implemented by Level/Content's GridService.
    /// V0-EXCEPTION: hardcoded 2D bool array; proper service interface in V1.
    /// </summary>
    public interface IGridQueryService
    {
        /// <summary>Returns true if the tile at (gridX, gridZ) is walkable.</summary>
        bool IsWalkable(int gridX, int gridZ);

        /// <summary>Returns the world-space center of tile (gridX, gridZ).</summary>
        Vector3 GetTileCenter(int gridX, int gridZ);

        /// <summary>Grid width in tiles.</summary>
        int Width { get; }

        /// <summary>Grid depth in tiles.</summary>
        int Depth { get; }
    }
}
