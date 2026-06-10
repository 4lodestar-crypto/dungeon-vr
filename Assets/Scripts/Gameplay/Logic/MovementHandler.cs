using DungeonVR.Shared;
using DungeonVR.Shared.Requests;
using DungeonVR.Shared.Results;
using UnityEngine;

namespace DungeonVR.Gameplay.Logic
{
    /// <summary>
    /// Pure-logic handler for tile-based champion movement.
    /// Validates bounds and walls, then applies translation or rotation.
    /// V0-EXCEPTION: refactor through proper server layer in V1.
    /// </summary>
    public static class MovementHandler
    {
        /// <summary>Tile pitch constant: 3 metres between adjacent tile centres.</summary>
        public const float TILE_SIZE = 3.0f;

        /// <summary>
        /// Process a single movement request against the given grid and champion state.
        /// Returns a MovementResult indicating success/failure and the new (cloned) state.
        /// The input champion is never mutated — a clone is created on success paths
        /// (immutability pattern for server-authoritative architecture).
        /// </summary>
        /// <param name="request">The movement request to validate and apply.</param>
        /// <param name="champion">The current champion state (not mutated — cloned on success).</param>
        /// <param name="walls">A 2D bool array where true = blocked/wall.</param>
        public static MovementResult Handle(MovementRequest request, ChampionState champion, bool[,] walls)
        {
            int gridWidth = walls.GetLength(0);
            int gridHeight = walls.GetLength(1);

            if (request.IsRotation)
            {
                // A/D — rotate in place
                ChampionState newState = champion.Clone();
                newState.FacingDirection = request.DesiredFacing.Value;
                return MovementResult.Succeeded(newState);
            }

            // W/S — calculate target tile
            Vector2Int target = champion.GridPosition + request.Direction;

            // Bounds check
            if (target.x < 0 || target.x >= gridWidth || target.y < 0 || target.y >= gridHeight)
            {
                return MovementResult.Blocked(champion,
                    $"Move to ({target.x},{target.y}) is out of bounds (grid {gridWidth}x{gridHeight})");
            }

            // Wall check
            if (walls[target.x, target.y])
            {
                return MovementResult.Blocked(champion,
                    $"Move to ({target.x},{target.y}) is blocked by a wall");
            }

            // Apply movement — clone, modify, return new state
            ChampionState newState = champion.Clone();
            newState.GridPosition = target;
            return MovementResult.Succeeded(newState);
        }

        /// <summary>
        /// Convert world-space position to tile-space coordinate.
        /// Origin tile (0,0) centre is at world (0, 0, 0).
        /// </summary>
        public static Vector2Int WorldToTile(Vector3 worldPos)
        {
            int x = Mathf.RoundToInt(worldPos.x / TILE_SIZE);
            int z = Mathf.RoundToInt(worldPos.z / TILE_SIZE);
            return new Vector2Int(x, z);
        }

        /// <summary>
        /// Convert tile-space coordinate to world-space position (centre of tile).
        /// </summary>
        public static Vector3 TileToWorld(Vector2Int tile)
        {
            return new Vector3(tile.x * TILE_SIZE, 0f, tile.y * TILE_SIZE);
        }
    }
}
