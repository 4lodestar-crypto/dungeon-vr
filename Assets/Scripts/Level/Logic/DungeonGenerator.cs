using System;
using System.Collections.Generic;
using UnityEngine;
using DungeonVR.Level.Components;
using DungeonVR.Level.Data;
using DungeonVR.Level.Interfaces;
using DungeonVR.Shared.Data;
using DungeonVR.Shared.Enums;
using Random = System.Random;

namespace DungeonVR.Level.Logic
{
    /// <summary>
    /// Procedural dungeon generator producing TileData arrays.
    /// Algorithm: rooms (random size/position) + L-shaped corridors.
    /// Guarantees connectivity via BFS flood fill with automatic corridor carving.
    /// </summary>
    public static class DungeonGenerator
    {
        /// <summary>
        /// Maximum attempts to generate a valid layout before giving up.
        /// </summary>
        private const int MaxGenerationAttempts = 50;

        /// <summary>
        /// Maximum placement attempts per room.
        /// </summary>
        private const int MaxRoomPlacementAttempts = 200;

        /// <summary>
        /// Maximum attempts to carve connectivity corridors during BFS fixing.
        /// </summary>
        private const int MaxConnectivityFixes = 100;

        /// <summary>
        /// Represents a placed room rectangle.
        /// </summary>
        private struct RoomRect
        {
            public int X;
            public int Z;
            public int Width;
            public int Height;

            public int Left => X;
            public int Right => X + Width - 1;
            public int Top => Z;
            public int Bottom => Z + Height - 1;

            public int CenterX => X + Width / 2;
            public int CenterZ => Z + Height / 2;

            public RoomRect(int x, int z, int w, int h)
            {
                X = x;
                Z = z;
                Width = w;
                Height = h;
            }

            /// <summary>Returns true if (tx, tz) is inside this room's interior.</summary>
            public bool Contains(int tx, int tz)
            {
                return tx >= X && tx < X + Width && tz >= Z && tz < Z + Height;
            }

            /// <summary>Returns true if this room overlaps another room.</summary>
            public bool Overlaps(RoomRect other)
            {
                return Left < other.Right && Right > other.Left &&
                       Top < other.Bottom && Bottom > other.Top;
            }

            /// <summary>Returns true if (tx, tz) is on the perimeter edge of this room.</summary>
            public bool IsEdge(int tx, int tz)
            {
                return Contains(tx, tz) &&
                       (tx == X || tx == X + Width - 1 || tz == Z || tz == Z + Height - 1);
            }
        }

        // ------------------------------------------------------------------
        // T11: Public API
        // ------------------------------------------------------------------

        /// <summary>
        /// Generate a complete dungeon layout as a TileData array.
        /// Uses the provided parameters for all configuration.
        /// Returns a validated, solvable tile array.
        /// </summary>
        public static TileData[] Generate(DungeonParams p)
        {
            if (p == null)
                throw new ArgumentNullException(nameof(p));

            p.Clamp();

            for (int attempt = 0; attempt < MaxGenerationAttempts; attempt++)
            {
                int seed = p.Seed + attempt * 31337;
                var rng = new Random(seed);

                TileData[] result = TryGenerate(p, rng);
                if (result == null)
                    continue;

                // Ensure connectivity via BFS + corridor carving
                if (EnsureConnectivity(result, p.Width, p.Depth, rng))
                {
                    // Rebuild TileData array with correct WallFaces after connectivity fixes
                    return RebuildWithWallFaces(result, p.Width, p.Depth);
                }
            }

            // Fallback: if all retries failed, generate a minimal solvable 3x3 box
            return GenerateFallback(p);
        }

        /// <summary>
        /// Generate a dungeon and load it directly into the scene via LevelLoader.
        /// Integrates with LevelValidator, GridService, and LevelLoader.
        /// Returns true on success with error=null, false with error message on failure.
        /// </summary>
        public static bool GenerateAndLoad(DungeonParams p, Transform tileRoot, ITilePalette palette, out string error)
        {
            error = null;

            if (p == null)
            {
                error = "DungeonParams is null.";
                return false;
            }
            if (tileRoot == null)
            {
                error = "Tile root transform is null.";
                return false;
            }
            if (palette == null)
            {
                error = "Tile palette is null.";
                return false;
            }

            TileData[] tiles;
            try
            {
                tiles = Generate(p);
            }
            catch (Exception ex)
            {
                error = $"Generation failed: {ex.Message}";
                return false;
            }

            // Validate with a LevelValidator instance
            var validator = new LevelValidator();
            if (!validator.Validate(tiles, p.Width, p.Depth, palette, out string[] validationErrors))
            {
                error = "Level validation failed: " + string.Join("; ", validationErrors);
                return false;
            }

            // Check solvability
            if (!validator.IsSolvable(tiles, p.Width, p.Depth))
            {
                error = "Generated level is not solvable (Stairs unreachable from Spawn).";
                return false;
            }

            // Load via LevelLoader (handles GridService registration internally)
            var loaderObj = new GameObject("DungeonGenerator_Loader");
            var loader = loaderObj.AddComponent<LevelLoader>();
            bool loaded = loader.LoadFromData(tiles, p.Width, p.Depth, palette, tileRoot);

            if (!loaded)
            {
                error = "LevelLoader.LoadFromData failed.";
                UnityEngine.Object.DestroyImmediate(loaderObj);
                return false;
            }

            UnityEngine.Object.DestroyImmediate(loaderObj);
            return true;
        }

        /// <summary>
        /// Generate a dungeon and run LevelValidator on the output.
        /// Returns true if validation passes.
        /// </summary>
        public static bool ValidateGenerated(DungeonParams p, out List<string> errors)
        {
            errors = new List<string>();

            if (p == null)
            {
                errors.Add("DungeonParams is null.");
                return false;
            }

            try
            {
                TileData[] tiles = Generate(p);

                var validator = new LevelValidator();
                string[] validationErrors;
                bool valid = validator.Validate(tiles, p.Width, p.Depth, null, out validationErrors);

                errors.AddRange(validationErrors);

                // Also check solvability if basic validation passes
                if (valid)
                {
                    bool solvable = validator.IsSolvable(tiles, p.Width, p.Depth);
                    if (!solvable)
                    {
                        errors.Add("Generated level is not solvable (Stairs unreachable from Spawn).");
                        return false;
                    }
                }

                return valid;
            }
            catch (Exception ex)
            {
                errors.Add($"Generation exception: {ex.Message}");
                return false;
            }
        }

        // ------------------------------------------------------------------
        // T11: Internal generation
        // ------------------------------------------------------------------

        /// <summary>
        /// Single attempt at generating a dungeon layout.
        /// Returns null on failure (unable to place any rooms).
        /// </summary>
        private static TileData[] TryGenerate(DungeonParams parameters, Random rng)
        {
            int width = parameters.Width;
            int depth = parameters.Depth;

            // Step 1: Initialize grid — all Floor by default
            TileType[,] grid = new TileType[width, depth];
            for (int x = 0; x < width; x++)
                for (int z = 0; z < depth; z++)
                    grid[x, z] = TileType.Floor;

            // Step 2: Place perimeter walls (outer ring)
            for (int x = 0; x < width; x++)
            {
                grid[x, 0] = TileType.Wall;
                grid[x, depth - 1] = TileType.Wall;
            }
            for (int z = 0; z < depth; z++)
            {
                grid[0, z] = TileType.Wall;
                grid[width - 1, z] = TileType.Wall;
            }

            // Step 3: Generate rooms (leave 1-tile border for perimeter wall)
            int targetRoomCount = rng.Next(parameters.MinRoomCount, parameters.MaxRoomCount + 1);
            List<RoomRect> rooms = new List<RoomRect>(targetRoomCount);

            for (int attempt = 0; attempt < MaxRoomPlacementAttempts && rooms.Count < targetRoomCount; attempt++)
            {
                int rw = rng.Next(parameters.MinRoomSize, parameters.MaxRoomSize + 1);
                int rh = rng.Next(parameters.MinRoomSize, parameters.MaxRoomSize + 1);

                // Clamp room dimensions so rng.Next(2, width - rw - 2) and
                // rng.Next(2, depth - rh - 2) have valid ranges (min < max).
                // rw must be <= width - 5 so that width - rw - 2 > 2.
                // Similarly rh must be <= depth - 5.
                int maxRw = width - 5;
                int maxRh = depth - 5;
                if (rw > maxRw) rw = maxRw;
                if (rh > maxRh) rh = maxRh;

                // If clamping made the room smaller than the minimum, skip
                if (rw < parameters.MinRoomSize || rh < parameters.MinRoomSize)
                    continue;

                // Position rooms with margin from perimeter walls (leave 1-tile border)
                int rx = rng.Next(2, width - rw - 2);
                int rz = rng.Next(2, depth - rh - 2);

                var newRoom = new RoomRect(rx, rz, rw, rh);

                // Reject if overlaps any existing room
                bool overlaps = false;
                foreach (var existing in rooms)
                {
                    if (newRoom.Overlaps(existing))
                    {
                        overlaps = true;
                        break;
                    }
                }

                if (overlaps)
                    continue;

                rooms.Add(newRoom);

                // Mark room interior as Floor
                for (int x = rx; x < rx + rw; x++)
                    for (int z = rz; z < rz + rh; z++)
                        grid[x, z] = TileType.Floor;
            }

            // If we couldn't place at least 1 room, fail
            if (rooms.Count < 1)
                return null;

            // Step 4: Place wall rings around rooms if requested
            if (parameters.PlaceWallsAroundRooms)
            {
                foreach (var room in rooms)
                {
                    PlaceRoomWalls(grid, room, rooms, width, depth);
                }
            }

            // Step 5: Connect rooms with L-shaped corridors
            for (int i = 0; i < rooms.Count - 1; i++)
            {
                CarveCorridor(grid, rooms[i], rooms[i + 1], rooms, parameters.CorridorWidth, rng, width, depth);
            }

            // Step 6: Place Spawn at first room's center (tag "Spawn")
            RoomRect firstRoom = rooms[0];
            int spawnTileX = Mathf.Clamp(firstRoom.CenterX, firstRoom.X, firstRoom.X + firstRoom.Width - 1);
            int spawnTileZ = Mathf.Clamp(firstRoom.CenterZ, firstRoom.Z, firstRoom.Z + firstRoom.Height - 1);
            grid[spawnTileX, spawnTileZ] = TileType.Spawn;

            // Step 7: Place Stairs at last room's edge tile farthest from Spawn (Manhattan distance), tag "Stairs"
            RoomRect lastRoom = rooms[rooms.Count - 1];
            TileCoord stairsPos = FindFarthestEdgeTile(lastRoom, spawnTileX, spawnTileZ);
            grid[stairsPos.X, stairsPos.Z] = TileType.Stairs;

            // Step 8: Compute WallFace values (T12)
            WallFace[,] wallFaces = ComputeWallFaces(grid, width, depth);

            // Step 9: Build TileData array with tags
            List<TileData> tileList = new List<TileData>(width * depth);
            for (int x = 0; x < width; x++)
            {
                for (int z = 0; z < depth; z++)
                {
                    TileData tile = new TileData(x, z, grid[x, z], wallFaces[x, z]);

                    // Assign tags
                    if (x == spawnTileX && z == spawnTileZ && grid[x, z] == TileType.Spawn)
                    {
                        tile.Tags = new[] { "Spawn" };
                    }
                    else if (x == stairsPos.X && z == stairsPos.Z && grid[x, z] == TileType.Stairs)
                    {
                        tile.Tags = new[] { "Stairs" };
                    }

                    tileList.Add(tile);
                }
            }

            return tileList.ToArray();
        }

        // ------------------------------------------------------------------
        // T11: Connectivity guarantee via BFS + corridor carving
        // ------------------------------------------------------------------

        /// <summary>
        /// Ensures a valid path from Spawn to Stairs by performing BFS flood fill
        /// and carving additional L-shaped corridors between disconnected regions.
        /// </summary>
        private static bool EnsureConnectivity(TileData[] tiles, int width, int depth, Random rng)
        {
            for (int fixAttempt = 0; fixAttempt < MaxConnectivityFixes; fixAttempt++)
            {
                // Build a lookup map and find Spawn and Stairs positions
                Dictionary<(int, int), TileData> tileMap = new Dictionary<(int, int), TileData>();
                TileCoord? spawnPos = null;
                TileCoord? stairsPos = null;

                foreach (TileData tile in tiles)
                {
                    var key = (tile.X, tile.Z);
                    tileMap[key] = tile;

                    if (tile.Type == TileType.Spawn)
                        spawnPos = new TileCoord(tile.X, tile.Z);

                    if (tile.Type == TileType.Stairs)
                        stairsPos = new TileCoord(tile.X, tile.Z);
                }

                if (spawnPos == null || stairsPos == null)
                    return false;

                // BFS flood fill from Spawn
                HashSet<(int, int)> visited = new HashSet<(int, int)>();
                Queue<(int, int)> queue = new Queue<(int, int)>();
                queue.Enqueue((spawnPos.Value.X, spawnPos.Value.Z));
                visited.Add((spawnPos.Value.X, spawnPos.Value.Z));

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

                        if (visited.Contains(neighborKey))
                            continue;

                        if (nx < 0 || nx >= width || nz < 0 || nz >= depth)
                            continue;

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

                // Check if Stairs is reachable
                if (visited.Contains((stairsPos.Value.X, stairsPos.Value.Z)))
                    return true;

                // Stairs unreachable — find closest unvisited walkable tile to Spawn
                // and carve a corridor toward it
                TileCoord? closestUnvisited = null;
                int closestDist = int.MaxValue;

                foreach (TileData tile in tiles)
                {
                    var key = (tile.X, tile.Z);
                    if (visited.Contains(key))
                        continue;

                    if (!IsWalkableForBFS(tile))
                        continue;

                    int dist = Mathf.Abs(tile.X - spawnPos.Value.X) + Mathf.Abs(tile.Z - spawnPos.Value.Z);
                    if (dist < closestDist)
                    {
                        closestDist = dist;
                        closestUnvisited = new TileCoord(tile.X, tile.Z);
                    }
                }

                if (closestUnvisited == null)
                    return false;

                // Carve a corridor from a random visited tile near the frontier to the unvisited tile
                CarveConnectivityCorridor(tiles, spawnPos.Value, closestUnvisited.Value, width, depth, rng);
            }

            return false;
        }

        /// <summary>
        /// Carves an L-shaped corridor between two positions during connectivity fixing.
        /// Modifies the tile array in-place.
        /// </summary>
        private static void CarveConnectivityCorridor(TileData[] tiles, TileCoord from, TileCoord to,
            int width, int depth, Random rng)
        {
            // Determine the corridor path (L-shaped)
            HashSet<(int, int)> corridorTiles = new HashSet<(int, int)>();
            bool horizontalFirst = rng.Next(2) == 0;

            if (horizontalFirst)
            {
                int minX = Mathf.Min(from.X, to.X);
                int maxX = Mathf.Max(from.X, to.X);
                for (int x = minX; x <= maxX; x++)
                    corridorTiles.Add((x, from.Z));
                int minZ = Mathf.Min(from.Z, to.Z);
                int maxZ = Mathf.Max(from.Z, to.Z);
                for (int z = minZ; z <= maxZ; z++)
                    corridorTiles.Add((to.X, z));
            }
            else
            {
                int minZ = Mathf.Min(from.Z, to.Z);
                int maxZ = Mathf.Max(from.Z, to.Z);
                for (int z = minZ; z <= maxZ; z++)
                    corridorTiles.Add((from.X, z));
                int minX = Mathf.Min(from.X, to.X);
                int maxX = Mathf.Max(from.X, to.X);
                for (int x = minX; x <= maxX; x++)
                    corridorTiles.Add((x, to.Z));
            }

            // Apply corridor tiles to the array
            for (int i = 0; i < tiles.Length; i++)
            {
                var key = (tiles[i].X, tiles[i].Z);
                if (corridorTiles.Contains(key))
                {
                    // Don't overwrite special tiles
                    if (tiles[i].Type != TileType.Spawn && tiles[i].Type != TileType.Stairs)
                    {
                        var oldTile = tiles[i];
                        tiles[i] = new TileData(oldTile.X, oldTile.Z, TileType.Floor, WallFace.None, oldTile.FloorHeight)
                        {
                            Tags = oldTile.Tags,
                            Metadata = oldTile.Metadata
                        };
                    }
                }
            }
        }

        /// <summary>
        /// Determines if a tile is walkable for BFS flood fill.
        /// </summary>
        private static bool IsWalkableForBFS(TileData tile)
        {
            return tile.Type != TileType.Wall &&
                   tile.Type != TileType.Empty;
        }

        // ------------------------------------------------------------------
        // T11: Room walls
        // ------------------------------------------------------------------

        /// <summary>
        /// Places a 1-tile-thick ring of Wall tiles around a room.
        /// Skips tiles that are inside another room's interior.
        /// </summary>
        private static void PlaceRoomWalls(TileType[,] grid, RoomRect room, List<RoomRect> allRooms,
            int width, int depth)
        {
            // Ring extends 1 tile outside the room rectangle
            int left = room.X - 1;
            int right = room.X + room.Width;
            int top = room.Z - 1;
            int bottom = room.Z + room.Height;

            for (int x = left; x <= right; x++)
            {
                for (int z = top; z <= bottom; z++)
                {
                    // Skip interior of the room itself
                    if (room.Contains(x, z))
                        continue;

                    // Skip out of bounds
                    if (x < 0 || x >= width || z < 0 || z >= depth)
                        continue;

                    // Skip if this tile is inside another room's interior
                    bool insideOtherRoom = false;
                    foreach (var other in allRooms)
                    {
                        if (other.Equals(room))
                            continue;
                        if (other.Contains(x, z))
                        {
                            insideOtherRoom = true;
                            break;
                        }
                    }

                    if (insideOtherRoom)
                        continue;

                    grid[x, z] = TileType.Wall;
                }
            }
        }

        // ------------------------------------------------------------------
        // T11: Corridors
        // ------------------------------------------------------------------

        /// <summary>
        /// Carves an L-shaped corridor from room A's center toward room B's center.
        /// Corridor tiles override walls but do not overlap room interiors.
        /// </summary>
        private static void CarveCorridor(TileType[,] grid, RoomRect fromRoom, RoomRect toRoom,
            List<RoomRect> allRooms, int corridorWidth, Random rng, int width, int depth)
        {
            int ax = Mathf.Clamp(fromRoom.CenterX, fromRoom.X, fromRoom.X + fromRoom.Width - 1);
            int az = Mathf.Clamp(fromRoom.CenterZ, fromRoom.Z, fromRoom.Z + fromRoom.Height - 1);
            int bx = Mathf.Clamp(toRoom.CenterX, toRoom.X, toRoom.X + toRoom.Width - 1);
            int bz = Mathf.Clamp(toRoom.CenterZ, toRoom.Z, toRoom.Z + toRoom.Height - 1);

            // Randomly choose L-shape orientation
            bool horizontalFirst = rng.Next(2) == 0;

            // Collect all corridor tiles
            HashSet<(int, int)> corridorTiles = new HashSet<(int, int)>();

            if (horizontalFirst)
            {
                // Horizontal from (ax, az) to (bx, az), then vertical to (bx, bz)
                int minX = Mathf.Min(ax, bx);
                int maxX = Mathf.Max(ax, bx);
                for (int x = minX; x <= maxX; x++)
                    AddCorridorTile(corridorTiles, x, az, corridorWidth);
                int minZ = Mathf.Min(az, bz);
                int maxZ = Mathf.Max(az, bz);
                for (int z = minZ; z <= maxZ; z++)
                    AddCorridorTile(corridorTiles, bx, z, corridorWidth);
            }
            else
            {
                // Vertical from (ax, az) to (ax, bz), then horizontal to (bx, bz)
                int minZ = Mathf.Min(az, bz);
                int maxZ = Mathf.Max(az, bz);
                for (int z = minZ; z <= maxZ; z++)
                    AddCorridorTile(corridorTiles, ax, z, corridorWidth);
                int minX = Mathf.Min(ax, bx);
                int maxX = Mathf.Max(ax, bx);
                for (int x = minX; x <= maxX; x++)
                    AddCorridorTile(corridorTiles, x, bz, corridorWidth);
            }

            // Apply corridor tiles — skip if inside any room interior
            foreach (var (cx, cz) in corridorTiles)
            {
                if (cx < 0 || cx >= width || cz < 0 || cz >= depth)
                    continue;

                // Skip if inside any room interior
                bool insideRoom = false;
                foreach (var room in allRooms)
                {
                    if (room.Contains(cx, cz))
                    {
                        insideRoom = true;
                        break;
                    }
                }

                if (insideRoom)
                    continue;

                // Only carve through walls (don't overwrite special tiles like Spawn/Stairs)
                if (grid[cx, cz] != TileType.Spawn && grid[cx, cz] != TileType.Stairs)
                {
                    grid[cx, cz] = TileType.Floor;
                }
            }
        }

        /// <summary>
        /// Adds a corridor tile with the specified width.
        /// For width=1, adds just (x,z). For wider corridors, adds a band.
        /// </summary>
        private static void AddCorridorTile(HashSet<(int, int)> tiles, int x, int z, int width)
        {
            int half = width / 2;
            for (int dx = -half; dx < width - half; dx++)
            {
                for (int dz = -half; dz < width - half; dz++)
                {
                    tiles.Add((x + dx, z + dz));
                }
            }
        }

        // ------------------------------------------------------------------
        // T11: Stairs placement
        // ------------------------------------------------------------------

        /// <summary>
        /// Finds the edge tile in a room that is farthest (Manhattan distance) from (fromX, fromZ).
        /// </summary>
        private static TileCoord FindFarthestEdgeTile(RoomRect room, int fromX, int fromZ)
        {
            int bestX = room.CenterX;
            int bestZ = room.CenterZ;
            int bestDist = -1;

            // Top and bottom edges
            for (int x = room.X; x < room.X + room.Width; x++)
            {
                int dist = Mathf.Abs(x - fromX) + Mathf.Abs(room.Z - fromZ);
                if (dist > bestDist)
                {
                    bestDist = dist;
                    bestX = x;
                    bestZ = room.Z;
                }

                dist = Mathf.Abs(x - fromX) + Mathf.Abs(room.Z + room.Height - 1 - fromZ);
                if (dist > bestDist)
                {
                    bestDist = dist;
                    bestX = x;
                    bestZ = room.Z + room.Height - 1;
                }
            }

            // Left and right edges (excluding corners already checked)
            for (int z = room.Z + 1; z < room.Z + room.Height - 1; z++)
            {
                int dist = Mathf.Abs(room.X - fromX) + Mathf.Abs(z - fromZ);
                if (dist > bestDist)
                {
                    bestDist = dist;
                    bestX = room.X;
                    bestZ = z;
                }

                dist = Mathf.Abs(room.X + room.Width - 1 - fromX) + Mathf.Abs(z - fromZ);
                if (dist > bestDist)
                {
                    bestDist = dist;
                    bestX = room.X + room.Width - 1;
                    bestZ = z;
                }
            }

            return new TileCoord(bestX, bestZ);
        }

        // ------------------------------------------------------------------
        // T12: WallFace computation
        // ------------------------------------------------------------------

        /// <summary>
        /// Computes WallFace flags for all tiles based on adjacency.
        /// 
        /// Rules:
        /// - Floor tiles adjacent to a Wall tile get WallFace pointing toward that wall.
        /// - Wall tiles adjacent to a Floor tile get WallFace pointing toward that floor (bidirectional).
        /// - Wall tiles on the perimeter get WallFace pointing inward (toward floor).
        /// - No wall faces between two walls or two floors.
        /// </summary>
        private static WallFace[,] ComputeWallFaces(TileType[,] grid, int width, int depth)
        {
            WallFace[,] faces = new WallFace[width, depth];

            // Cardinal direction offsets: North(+Z), South(-Z), East(+X), West(-X)
            int[] dx = { 0, 0, 1, -1 };
            int[] dz = { 1, -1, 0, 0 };
            WallFace[] dirFlags = { WallFace.North, WallFace.South, WallFace.East, WallFace.West };

            for (int x = 0; x < width; x++)
            {
                for (int z = 0; z < depth; z++)
                {
                    TileType currentType = grid[x, z];

                    // Skip Empty tiles
                    if (currentType == TileType.Empty)
                        continue;

                    WallFace face = WallFace.None;

                    for (int d = 0; d < 4; d++)
                    {
                        int nx = x + dx[d];
                        int nz = z + dz[d];

                        // Out of bounds → add wall face (for perimeter walls, face inward)
                        if (nx < 0 || nx >= width || nz < 0 || nz >= depth)
                        {
                            // Only add wall face if current tile is walkable (faces outward at boundary)
                            // or if it's a wall (faces inward, away from boundary)
                            if (IsWalkableType(currentType))
                            {
                                // Floor at boundary → face outward (toward missing/world edge)
                                // This should rarely happen with perimeter walls, but handle it
                                face |= dirFlags[d];
                            }
                            continue;
                        }

                        TileType neighborType = grid[nx, nz];

                        bool currentIsWalkable = IsWalkableType(currentType);
                        bool neighborIsWalkable = IsWalkableType(neighborType);

                        if (currentIsWalkable && !neighborIsWalkable)
                        {
                            // Floor adjacent to Wall → add face toward wall
                            face |= dirFlags[d];
                        }
                        else if (!currentIsWalkable && neighborIsWalkable)
                        {
                            // Wall adjacent to Floor → add face toward floor (bidirectional)
                            face |= dirFlags[d];
                        }
                        // Both walkable or both blocked → no face needed
                    }

                    faces[x, z] = face;
                }
            }

            return faces;
        }

        /// <summary>
        /// Returns true if the tile type is considered walkable for wall face computation.
        /// </summary>
        private static bool IsWalkableType(TileType type)
        {
            return type != TileType.Wall && type != TileType.Empty;
        }

        /// <summary>
        /// Rebuilds a TileData array from existing tiles, recomputing all WallFaces
        /// from a reconstructed grid. Used after connectivity-fixing corridor carving
        /// to ensure correct WallFace values on newly carved tiles.
        /// </summary>
        private static TileData[] RebuildWithWallFaces(TileData[] source, int width, int depth)
        {
            // Reconstruct TileType grid from source
            TileType[,] grid = new TileType[width, depth];
            for (int i = 0; i < source.Length; i++)
            {
                int x = source[i].X;
                int z = source[i].Z;
                if (x >= 0 && x < width && z >= 0 && z < depth)
                    grid[x, z] = source[i].Type;
            }

            // Recompute WallFaces from clean grid
            WallFace[,] wallFaces = ComputeWallFaces(grid, width, depth);

            // Rebuild TileData array preserving tags and metadata
            TileData[] result = new TileData[source.Length];
            for (int i = 0; i < source.Length; i++)
            {
                int x = source[i].X;
                int z = source[i].Z;
                TileData tile = new TileData(x, z, source[i].Type, wallFaces[x, z], source[i].FloorHeight);
                tile.Tags = source[i].Tags;
                tile.Metadata = source[i].Metadata;
                result[i] = tile;
            }

            return result;
        }

        // ------------------------------------------------------------------
        // T11: Fallback generation
        // ------------------------------------------------------------------

        /// <summary>
        /// Minimal fallback level when all generation attempts fail.
        /// Creates a tiny 3-5 tile solvable layout: perimeter walls, spawn center, stairs at edge.
        /// </summary>
        private static TileData[] GenerateFallback(DungeonParams parameters)
        {
            int fw = Mathf.Min(5, Mathf.Max(3, parameters.Width));
            int fd = Mathf.Min(5, Mathf.Max(3, parameters.Depth));

            TileType[,] grid = new TileType[fw, fd];
            for (int x = 0; x < fw; x++)
                for (int z = 0; z < fd; z++)
                    grid[x, z] = TileType.Floor;

            // Perimeter walls
            for (int x = 0; x < fw; x++)
            {
                grid[x, 0] = TileType.Wall;
                grid[x, fd - 1] = TileType.Wall;
            }
            for (int z = 0; z < fd; z++)
            {
                grid[0, z] = TileType.Wall;
                grid[fw - 1, z] = TileType.Wall;
            }

            // Spawn in center, stairs at opposite edge
            int spawnX = fw / 2;
            int spawnZ = fd / 2;
            grid[spawnX, spawnZ] = TileType.Spawn;
            grid[fw - 2, fd - 2] = TileType.Stairs;

            WallFace[,] faces = ComputeWallFaces(grid, fw, fd);

            TileData[] result = new TileData[fw * fd];
            int idx = 0;
            for (int x = 0; x < fw; x++)
            {
                for (int z = 0; z < fd; z++)
                {
                    TileData tile = new TileData(x, z, grid[x, z], faces[x, z]);
                    if (x == spawnX && z == spawnZ)
                        tile.Tags = new[] { "Spawn" };
                    else if (x == fw - 2 && z == fd - 2 && grid[x, z] == TileType.Stairs)
                        tile.Tags = new[] { "Stairs" };
                    result[idx++] = tile;
                }
            }

            return result;
        }
    }
}
