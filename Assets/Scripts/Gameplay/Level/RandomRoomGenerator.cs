using System;
using System.Collections.Generic;
using UnityEngine;

namespace DungeonVR.Gameplay.Level
{
    /// <summary>
    /// Procedural dungeon generator using random room placement with corridor stitching.
    /// Places rooms on a wall-filled grid, connects them with L-shaped corridors,
    /// and guarantees full connectivity via flood-fill verification.
    /// V0-EXCEPTION: pure logic — no MonoBehaviour, no Unity scene dependencies.
    /// </summary>
    public class RandomRoomGenerator : IDungeonGenerator
    {
        /// <summary>Minimum room width/height in tiles (inclusive).</summary>
        public int MinRoomSize { get; }

        /// <summary>Maximum room width/height in tiles (inclusive).</summary>
        public int MaxRoomSize { get; }

        /// <summary>Width of corridor passages in tiles (1 = single tile passage).</summary>
        public int CorridorWidth { get; }

        /// <summary>Minimum grid width to generate.</summary>
        public int MinGridWidth { get; }

        /// <summary>Minimum grid height to generate.</summary>
        public int MinGridHeight { get; }

        /// <summary>Number of rooms to attempt placement for (final count may be lower if space is tight).</summary>
        public int RoomCount { get; }

        /// <summary>Maximum number of placement attempts per room before giving up.</summary>
        private const int MaxPlacementAttempts = 100;

        /// <summary>
        /// Create a RandomRoomGenerator with the default parameters:
        /// 15x15 minimum grid, 3 rooms, 5-10 tile rooms, 1-tile corridors.
        /// </summary>
        public RandomRoomGenerator()
            : this(minRoomSize: 5, maxRoomSize: 10, corridorWidth: 1, roomCount: 4, minGridWidth: 15, minGridHeight: 15)
        {
        }

        /// <summary>
        /// Create a RandomRoomGenerator with explicit parameters.
        /// </summary>
        /// <param name="minRoomSize">Minimum room width and height in tiles (inclusive). Default 5.</param>
        /// <param name="maxRoomSize">Maximum room width and height in tiles (inclusive). Default 10.</param>
        /// <param name="corridorWidth">Width of corridor passages. Default 1.</param>
        /// <param name="roomCount">Number of rooms to attempt. Default 4.</param>
        /// <param name="minGridWidth">Minimum grid width. Default 15.</param>
        /// <param name="minGridHeight">Minimum grid height. Default 15.</param>
        public RandomRoomGenerator(int minRoomSize, int maxRoomSize, int corridorWidth,
                                    int roomCount = 4, int minGridWidth = 15, int minGridHeight = 15)
        {
            MinRoomSize = Mathf.Max(3, minRoomSize);
            MaxRoomSize = Mathf.Max(MinRoomSize, maxRoomSize);
            CorridorWidth = Mathf.Max(1, corridorWidth);
            RoomCount = Mathf.Clamp(roomCount, 3, 10);
            MinGridWidth = Mathf.Max(9, minGridWidth);
            MinGridHeight = Mathf.Max(9, minGridHeight);
        }

        /// <summary>
        /// Generate a dungeon layout deterministically from the given seed.
        /// Uses System.Random internally — same seed always produces the same layout
        /// for the same parameter set.
        /// </summary>
        public DungeonData Generate(int seed)
        {
            System.Random rng = new System.Random(seed);

            // Calculate grid dimensions to comfortably fit requested rooms
            int gridWidth = CalculateGridDimension(MinGridWidth, MaxRoomSize, RoomCount);
            int gridHeight = CalculateGridDimension(MinGridHeight, MaxRoomSize, RoomCount);

            // Start with all walls
            bool[,] walls = new bool[gridWidth, gridHeight];
            for (int x = 0; x < gridWidth; x++)
            {
                for (int y = 0; y < gridHeight; y++)
                {
                    walls[x, y] = true;
                }
            }

            // --- Phase 1: Place rooms ---
            List<RectInt> rooms = new List<RectInt>();
            int maxAttempts = MaxPlacementAttempts * RoomCount;

            for (int i = 0; i < RoomCount && maxAttempts > 0; maxAttempts--)
            {
                int roomW = rng.Next(MinRoomSize, MaxRoomSize + 1);
                int roomH = rng.Next(MinRoomSize, MaxRoomSize + 1);

                // Room bounds: leave 1-tile margin for perimeter walls
                int maxX = gridWidth - roomW - 2;
                int maxY = gridHeight - roomH - 2;

                if (maxX < 1 || maxY < 1)
                {
                    continue;
                }

                int rx = rng.Next(1, maxX + 1);
                int ry = rng.Next(1, maxY + 1);
                RectInt candidate = new RectInt(rx, ry, roomW, roomH);

                if (!OverlapsAny(candidate, rooms))
                {
                    // Carve the room
                    for (int x = candidate.x; x < candidate.xMax; x++)
                    {
                        for (int y = candidate.y; y < candidate.yMax; y++)
                        {
                            walls[x, y] = false;
                        }
                    }

                    rooms.Add(candidate);
                    i++; // successfully placed
                }
            }

            // --- Phase 2: Connect rooms with L-shaped corridors ---
            for (int i = 1; i < rooms.Count; i++)
            {
                Vector2Int a = RoomCenter(rooms[i - 1]);
                Vector2Int b = RoomCenter(rooms[i]);
                CarveCorridor(walls, a.x, a.y, b.x, b.y, CorridorWidth, gridWidth, gridHeight);
            }

            // Additionally connect first and last for a loop (improves connectivity)
            if (rooms.Count >= 3)
            {
                Vector2Int first = RoomCenter(rooms[0]);
                Vector2Int last = RoomCenter(rooms[rooms.Count - 1]);
                CarveCorridor(walls, first.x, first.y, last.x, last.y, CorridorWidth, gridWidth, gridHeight);
            }

            // --- Phase 3: Flood-fill connectivity check and stitch ---
            EnsureConnectivity(walls, rooms, gridWidth, gridHeight, rng);

            // --- Phase 4: Ensure perimeter edges are walls ---
            for (int x = 0; x < gridWidth; x++)
            {
                walls[x, 0] = true;
                walls[x, gridHeight - 1] = true;
            }
            for (int y = 0; y < gridHeight; y++)
            {
                walls[0, y] = true;
                walls[gridWidth - 1, y] = true;
            }

            // --- Phase 5: Find start position (first walkable tile in first room center) ---
            Vector2Int startPosition = FindStartPosition(walls, rooms, gridWidth, gridHeight);

            return new DungeonData(gridWidth, gridHeight, walls, startPosition);
        }

        /// <summary>
        /// Calculate grid dimension large enough to hold the requested rooms with padding.
        /// </summary>
        private static int CalculateGridDimension(int minimum, int maxRoomSize, int roomCount)
        {
            // Estimate: each room needs maxRoomSize + 2 padding, arranged roughly in sqrt(count) columns
            int roomsPerSide = Mathf.CeilToInt(Mathf.Sqrt(roomCount));
            int estimated = roomsPerSide * (maxRoomSize + 3) + 2;
            return Mathf.Max(minimum, estimated);
        }

        /// <summary>
        /// Check whether a candidate room rectangle overlaps any existing room (with 1-tile padding).
        /// </summary>
        private static bool OverlapsAny(RectInt candidate, List<RectInt> existing)
        {
            // Expand candidate by 1 tile padding on all sides
            RectInt padded = new RectInt(candidate.x - 1, candidate.y - 1,
                                          candidate.width + 2, candidate.height + 2);

            foreach (RectInt room in existing)
            {
                if (padded.Overlaps(room))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Get the centre tile of a room (rounded toward origin for even-sized rooms).
        /// </summary>
        private static Vector2Int RoomCenter(RectInt room)
        {
            return new Vector2Int(room.x + room.width / 2, room.y + room.height / 2);
        }

        /// <summary>
        /// Carve an L-shaped corridor from (x1, y1) to (x2, y2) with the given width.
        /// First moves horizontally then vertically (L-shape).
        /// </summary>
        private static void CarveCorridor(bool[,] walls, int x1, int y1, int x2, int y2,
                                           int corridorWidth, int gridWidth, int gridHeight)
        {
            int halfWidth = corridorWidth / 2;

            // Horizontal segment
            int horStartX = Mathf.Min(x1, x2);
            int horEndX = Mathf.Max(x1, x2);
            int horY = y1;

            for (int x = horStartX; x <= horEndX; x++)
            {
                for (int w = -halfWidth; w <= halfWidth; w++)
                {
                    int ty = horY + w;
                    if (ty >= 0 && ty < gridHeight && x >= 0 && x < gridWidth)
                    {
                        walls[x, ty] = false;
                    }
                }
            }

            // Vertical segment
            int verStartY = Mathf.Min(y1, y2);
            int verEndY = Mathf.Max(y1, y2);
            int verX = x2;

            for (int y = verStartY; y <= verEndY; y++)
            {
                for (int w = -halfWidth; w <= halfWidth; w++)
                {
                    int tx = verX + w;
                    if (tx >= 0 && tx < gridWidth && y >= 0 && y < gridHeight)
                    {
                        walls[tx, y] = false;
                    }
                }
            }
        }

        /// <summary>
        /// Flood-fill from an origin walkable tile and return the set of visited walkable tiles.
        /// </summary>
        private static HashSet<Vector2Int> FloodFill(bool[,] walls, Vector2Int origin,
                                                      int gridWidth, int gridHeight)
        {
            HashSet<Vector2Int> visited = new HashSet<Vector2Int>();
            Queue<Vector2Int> queue = new Queue<Vector2Int>();
            queue.Enqueue(origin);
            visited.Add(origin);

            // Four cardinal directions
            Vector2Int[] dirs =
            {
                new Vector2Int(0, 1),
                new Vector2Int(0, -1),
                new Vector2Int(1, 0),
                new Vector2Int(-1, 0)
            };

            while (queue.Count > 0)
            {
                Vector2Int current = queue.Dequeue();

                foreach (Vector2Int dir in dirs)
                {
                    Vector2Int next = new Vector2Int(current.x + dir.x, current.y + dir.y);

                    if (next.x < 0 || next.x >= gridWidth || next.y < 0 || next.y >= gridHeight)
                    {
                        continue;
                    }

                    if (walls[next.x, next.y])
                    {
                        continue; // wall — skip
                    }

                    if (visited.Add(next))
                    {
                        queue.Enqueue(next);
                    }
                }
            }

            return visited;
        }

        /// <summary>
        /// Verify all room centres are reachable from each other via walkable tiles.
        /// If not, carve additional stitch corridors between disconnected components.
        /// </summary>
        private static void EnsureConnectivity(bool[,] walls, List<RectInt> rooms,
                                                 int gridWidth, int gridHeight, System.Random rng)
        {
            if (rooms.Count < 2)
            {
                return;
            }

            // Flood-fill from the first room's centre
            Vector2Int origin = RoomCenter(rooms[0]);
            if (!IsWalkable(walls, origin, gridWidth, gridHeight))
            {
                // If origin is somehow walled, pick any walkable tile
                origin = FindAnyWalkable(walls, gridWidth, gridHeight);
                if (origin.x < 0) return; // no walkable tiles at all
            }

            HashSet<Vector2Int> connected = FloodFill(walls, origin, gridWidth, gridHeight);

            // Check each room — if its centre isn't in the connected set, carve a corridor
            for (int i = 1; i < rooms.Count; i++)
            {
                Vector2Int centre = RoomCenter(rooms[i]);

                if (!connected.Contains(centre))
                {
                    // Find the closest tile in the connected set
                    Vector2Int closest = FindClosestConnected(centre, connected, walls, gridWidth, gridHeight);

                    // Carve a corridor from this room's centre to the closest connected tile
                    CarveCorridor(walls, centre.x, centre.y, closest.x, closest.y,
                                  Mathf.Max(1, CorridorWidth), gridWidth, gridHeight);

                    // Re-flood to expand the connected set
                    connected = FloodFill(walls, origin, gridWidth, gridHeight);
                }
            }
        }

        /// <summary>
        /// Find the closest walkable tile (within the connected set) to a given target tile.
        /// Used to stitch disconnected rooms into the main component.
        /// </summary>
        private static Vector2Int FindClosestConnected(Vector2Int target, HashSet<Vector2Int> connected,
                                                         bool[,] walls, int gridWidth, int gridHeight)
        {
            Vector2Int best = target;
            int bestDist = int.MaxValue;

            foreach (Vector2Int tile in connected)
            {
                int dx = tile.x - target.x;
                int dy = tile.y - target.y;
                int dist = dx * dx + dy * dy;

                if (dist < bestDist)
                {
                    bestDist = dist;
                    best = tile;
                }
            }

            return best;
        }

        /// <summary>
        /// Find any walkable tile on the grid. Returns (-1, -1) if none found.
        /// </summary>
        private static Vector2Int FindAnyWalkable(bool[,] walls, int gridWidth, int gridHeight)
        {
            for (int x = 0; x < gridWidth; x++)
            {
                for (int y = 0; y < gridHeight; y++)
                {
                    if (!walls[x, y])
                    {
                        return new Vector2Int(x, y);
                    }
                }
            }

            return new Vector2Int(-1, -1);
        }

        /// <summary>
        /// Find the start position: the centre of the first placed room,
        /// guaranteed to be walkable. Falls back to any walkable tile.
        /// </summary>
        private static Vector2Int FindStartPosition(bool[,] walls, List<RectInt> rooms,
                                                     int gridWidth, int gridHeight)
        {
            if (rooms.Count > 0)
            {
                Vector2Int centre = RoomCenter(rooms[0]);
                if (IsWalkable(walls, centre, gridWidth, gridHeight))
                {
                    return centre;
                }

                // Search outward from centre
                for (int r = 1; r < Mathf.Max(gridWidth, gridHeight); r++)
                {
                    for (int dx = -r; dx <= r; dx++)
                    {
                        for (int dy = -r; dy <= r; dy++)
                        {
                            if (Mathf.Abs(dx) != r && Mathf.Abs(dy) != r) continue;

                            Vector2Int tile = new Vector2Int(centre.x + dx, centre.y + dy);
                            if (IsWalkable(walls, tile, gridWidth, gridHeight))
                            {
                                return tile;
                            }
                        }
                    }
                }
            }

            // Fallback: any walkable tile
            Vector2Int fallback = FindAnyWalkable(walls, gridWidth, gridHeight);
            return fallback.x >= 0 ? fallback : new Vector2Int(1, 1);
        }

        /// <summary>
        /// Check if a tile is in bounds and walkable.
        /// </summary>
        private static bool IsWalkable(bool[,] walls, Vector2Int tile, int gridWidth, int gridHeight)
        {
            return tile.x >= 0 && tile.x < gridWidth &&
                   tile.y >= 0 && tile.y < gridHeight &&
                   !walls[tile.x, tile.y];
        }
    }
}
