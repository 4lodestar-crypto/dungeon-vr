using System.Collections.Generic;
using System.Linq;
using DungeonVR.Shared.Data;
using DungeonVR.Shared.Enums;
using DungeonVR.Level.Interfaces;

namespace DungeonVR.Level.Logic
{
    /// <summary>
    /// Validates level data for correctness and solvability.
    /// Implements ILevelValidator.
    /// 
    /// Validation checks:
    /// - Exactly 1 Spawn tile exists
    /// - At least 1 Stairs tile exists
    /// - No duplicate/overlapping coordinates
    /// - All tiles within grid bounds
    /// - Palette is complete (all non-Empty types have prefabs)
    /// - All tile types in tiles are in the palette
    /// 
    /// Solvability check:
    /// - BFS flood fill from Spawn tile
    /// - All Stairs tiles must be reachable
    /// - Counts walkable tiles reached
    /// </summary>
    public class LevelValidator : ILevelValidator
    {
        /// <summary>
        /// Run all validation checks on the given tile data.
        /// Returns true if the level is valid, false otherwise.
        /// </summary>
        public bool Validate(TileData[] tiles, int width, int depth, ITilePalette palette, out string[] errors)
        {
            List<string> errorList = new List<string>();

            if (tiles == null || tiles.Length == 0)
            {
                errorList.Add("TileData array is null or empty.");
                errors = errorList.ToArray();
                return false;
            }

            if (width <= 0 || depth <= 0)
            {
                errorList.Add($"Invalid grid dimensions: {width}x{depth}. Both must be positive.");
            }

            // Check palette completeness (skip if no palette provided — tiles-only validation)
            if (palette != null && !palette.IsComplete)
            {
                errorList.Add("Tile palette is incomplete — some TileType values have no prefab assigned.");
            }

            // Track tile counts and coordinate uniqueness
            Dictionary<(int, int), TileData> tileMap = new Dictionary<(int, int), TileData>();
            int spawnCount = 0;
            int stairsCount = 0;
            int wallCount = 0;
            int emptyCount = 0;

            foreach (TileData tile in tiles)
            {
                // Bounds check
                if (tile.X < 0 || tile.X >= width || tile.Z < 0 || tile.Z >= depth)
                {
                    errorList.Add($"Tile at ({tile.X},{tile.Z}) is out of bounds ({width}x{depth}).");
                    continue;
                }

                // Duplicate coordinate check
                var key = (tile.X, tile.Z);
                if (tileMap.ContainsKey(key))
                {
                    errorList.Add($"Duplicate tile at ({tile.X},{tile.Z}): type '{tileMap[key].Type}' and '{tile.Type}'.");
                }
                else
                {
                    tileMap[key] = tile;
                }

                // Count special tiles
                switch (tile.Type)
                {
                    case TileType.Spawn:
                        spawnCount++;
                        break;
                    case TileType.Stairs:
                        stairsCount++;
                        break;
                    case TileType.Wall:
                        wallCount++;
                        break;
                    case TileType.Empty:
                        emptyCount++;
                        break;
                }

                // Check that tile type exists in the palette
                if (palette != null && tile.Type != TileType.Empty)
                {
                    var prefab = palette.GetPrefab(tile.Type);
                    if (prefab == null)
                    {
                        errorList.Add($"Tile type '{tile.Type}' at ({tile.X},{tile.Z}) has no prefab in the palette.");
                    }
                }
            }

            // Check spawn and stairs counts
            if (spawnCount == 0)
                errorList.Add("No Spawn tile found — exactly 1 is required.");
            else if (spawnCount > 1)
                errorList.Add($"Found {spawnCount} Spawn tiles — exactly 1 is required.");

            if (stairsCount == 0)
                errorList.Add("No Stairs tile found — at least 1 is required.");

            if (wallCount == 0)
                errorList.Add("No Wall tiles found — level may be too open.");

            if (emptyCount > 0)
                errorList.Add($"Found {emptyCount} Empty tile(s) inside the grid. Empty tiles should only exist outside bounds.");

            // Check for out-of-bounds tiles (we already counted them above)
            // Check vertical fill: at least some walkable space
            int walkableCount = tiles.Count(t =>
                t.Type != TileType.Wall &&
                t.Type != TileType.Empty &&
                t.X >= 0 && t.X < width &&
                t.Z >= 0 && t.Z < depth);

            if (walkableCount == 0)
                errorList.Add("No walkable tiles found in the grid.");

            errors = errorList.ToArray();
            return errors.Length == 0;
        }

        /// <summary>
        /// Run solvability check (exit reachable from start).
        /// BFS flood fill from the Spawn tile through walkable tiles.
        /// Returns true if at least one Stairs tile is reachable.
        /// </summary>
        public bool IsSolvable(TileData[] tiles, int width, int depth)
        {
            if (tiles == null || tiles.Length == 0 || width <= 0 || depth <= 0)
                return false;

            // Build a lookup map and find spawn
            Dictionary<(int, int), TileData> tileMap = new Dictionary<(int, int), TileData>();
            TileCoord? spawnPos = null;
            List<TileCoord> stairsPositions = new List<TileCoord>();

            foreach (TileData tile in tiles)
            {
                var key = (tile.X, tile.Z);
                tileMap[key] = tile;

                if (tile.Type == TileType.Spawn)
                    spawnPos = new TileCoord(tile.X, tile.Z);

                if (tile.Type == TileType.Stairs)
                    stairsPositions.Add(new TileCoord(tile.X, tile.Z));
            }

            if (spawnPos == null || stairsPositions.Count == 0)
                return false;

            // BFS flood fill from spawn
            HashSet<(int, int)> visited = new HashSet<(int, int)>();
            Queue<(int, int)> queue = new Queue<(int, int)>();

            TileCoord start = spawnPos.Value;
            queue.Enqueue((start.X, start.Z));
            visited.Add((start.X, start.Z));

            // Cardinal directions: North, South, East, West
            int[] dx = { 0, 0, 1, -1 };
            int[] dz = { 1, -1, 0, 0 };

            while (queue.Count > 0)
            {
                var (cx, cz) = queue.Dequeue();

                for (int i = 0; i < 4; i++)
                {
                    int nx = cx + dx[i];
                    int nz = cz + dz[i];

                    var neighborKey = (nx, nz);

                    // Skip if already visited
                    if (visited.Contains(neighborKey))
                        continue;

                    // Skip if out of bounds
                    if (nx < 0 || nx >= width || nz < 0 || nz >= depth)
                        continue;

                    // Check if the neighbor tile exists and is walkable
                    if (tileMap.TryGetValue(neighborKey, out TileData neighbor))
                    {
                        if (IsWalkableForBFS(neighbor))
                        {
                            visited.Add(neighborKey);
                            queue.Enqueue((nx, nz));
                        }
                    }
                }
            }

            // Check if all stairs positions are reachable
            foreach (TileCoord stairs in stairsPositions)
            {
                if (!visited.Contains((stairs.X, stairs.Z)))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Determines if a tile is walkable for BFS flood fill.
        /// Stairs are walkable (exit condition). Walls and Empty tiles block.
        /// </summary>
        private static bool IsWalkableForBFS(TileData tile)
        {
            return tile.Type != TileType.Wall &&
                   tile.Type != TileType.Empty;
        }
    }
}
