using System;
using System.Collections.Generic;
using UnityEngine;
using DungeonVR.Level.Data;
using DungeonVR.Shared.Data;
using DungeonVR.Shared.Enums;
using DungeonVR.Level.Components;
using DungeonVR.Level.Interfaces;
using Random = System.Random;

namespace DungeonVR.Level.Logic
{
    /// <summary>
    /// Procedural dungeon generator producing TileData arrays.
    /// Algorithm: rooms (random size/position) + L-shaped corridors.
    /// Guarantees connectivity via BFS flood fill with automatic retry.
    /// </summary>
    public class DungeonGenerator
    {
        /// <summary>
        /// Maximum attempts to generate a valid, solvable layout before giving up.
        /// </summary>
        private const int MaxRetries = 50;

        /// <summary>
        /// Maximum placement attempts per room.
        /// </summary>
        private const int MaxRoomPlacementAttempts = 200;

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

        /// <summary>
        /// Generate a complete dungeon layout as a TileData array.
        /// Uses the provided parameters for all configuration.
        /// Returns a validated, solvable tile array.
        /// </summary>
        public TileData[] Generate(DungeonParams parameters)
        {
            if (parameters == null)
                throw new ArgumentNullException(nameof(parameters));

            parameters.Clamp();

            for (int attempt = 0; attempt < MaxRetries; attempt++)
            {
                int seed = parameters.Seed + attempt * 31337;
                var rng = new Random(seed);

                TileData[] result = TryGenerate(parameters, rng);
                if (result == null)
                    continue;

                // Check solvability (connectivity via BFS)
                var validator = new LevelValidator();
                if (validator.IsSolvable(result, parameters.Width, parameters.Depth))
                {
                    return result;
                }
            }

            // Fallback: if all retries failed, generate a minimal solvable 3x3 box
            return GenerateFallback(parameters);
        }

        /// <summary>
        /// Single attempt at generating a dungeon layout.
        /// Returns null on failure (unable to place all rooms).
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

            // Step 2: Place perimeter walls
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

            // Step 3: Generate rooms
            int targetRoomCount = rng.Next(parameters.MinRoomCount, parameters.MaxRoomCount + 1);
            List<RoomRect> rooms = new List<RoomRect>(targetRoomCount);

            for (int attempt = 0; attempt < MaxRoomPlacementAttempts && rooms.Count < targetRoomCount; attempt++)
            {
                int rw = rng.Next(parameters.MinRoomSize, parameters.MaxRoomSize + 1);
                int rh = rng.Next(parameters.MinRoomSize, parameters.MaxRoomSize + 1);

                // Position rooms with margin from perimeter walls
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

                // Mark room interior as Floor (already Floor by default, but be explicit)
                for (int x = rx; x < rx + rw; x++)
                    for (int z = rz; z < rz + rh; z++)
                        grid[x, z] = TileType.Floor;
            }

            // If we couldn't place at least 1 room, fail
            if (rooms.Count < 1)
                return null;

            // Step 4: Place interior walls around rooms
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

            // Step 6: Place Spawn at first room's center
            RoomRect firstRoom = rooms[0];
            int spawnTileX = firstRoom.CenterX;
            int spawnTileZ = firstRoom.CenterZ;
            // Clamp to room interior
            spawnTileX = Mathf.Clamp(spawnTileX, firstRoom.X, firstRoom.X + firstRoom.Width - 1);
            spawnTileZ = Mathf.Clamp(spawnTileZ, firstRoom.Z, firstRoom.Z + firstRoom.Height - 1);
            grid[spawnTileX, spawnTileZ] = TileType.Spawn;

            // Step 7: Place Stairs in last room, at edge farthest from spawn
            RoomRect lastRoom = rooms[rooms.Count - 1];
            TileCoord stairsPos = FindFarthestEdgeTile(lastRoom, spawnTileX, spawnTileZ);
            grid[stairsPos.X, stairsPos.Z] = TileType.Stairs;

            // Step 8: Compute WallFace values
            WallFace[,] wallFaces = ComputeWallFaces(grid, width, depth);

            // Step 9: Build TileData array
            List<TileData> tileList = new List<TileData>(width * depth);
            for (int x = 0; x < width; x++)
            {
                for (int z = 0; z < depth; z++)
                {
                    tileList.Add(new TileData(x, z, grid[x, z], wallFaces[x, z]));
                }
            }

            return tileList.ToArray();
        }

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

        /// <summary>
        /// Carves an L-shaped corridor from room A's center toward room B's center.
        /// Corridor tiles override walls but do not overlap room interiors.
        /// </summary>
        private static void CarveCorridor(TileType[,] grid, RoomRect fromRoom, RoomRect toRoom,
            List<RoomRect> allRooms, int corridorWidth, Random rng, int width, int depth)
        {
            int ax = fromRoom.CenterX;
            int az = fromRoom.CenterZ;
            int bx = toRoom.CenterX;
            int bz = toRoom.CenterZ;

            // Clamp centers to room interiors
            ax = Mathf.Clamp(ax, fromRoom.X, fromRoom.X + fromRoom.Width - 1);
            az = Mathf.Clamp(az, fromRoom.Z, fromRoom.Z + fromRoom.Height - 1);
            bx = Mathf.Clamp(bx, toRoom.X, toRoom.X + toRoom.Width - 1);
            bz = Mathf.Clamp(bz, toRoom.Z, toRoom.Z + toRoom.Height - 1);

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
                    // For corridorWidth=1, half=0, adds just (x,z)
                    // For corridorWidth=2, adds a 2x2 block
                    // For corridorWidth=3, adds a 3x3 block
                    tiles.Add((x + dx, z + dz));
                }
            }
        }

        /// <summary>
        /// Finds the edge tile in a room that is farthest (Manhattan distance) from (fromX, fromZ).
        /// </summary>
        private static TileCoord FindFarthestEdgeTile(RoomRect room, int fromX, int fromZ)
        {
            int bestX = room.CenterX;
            int bestZ = room.CenterZ;
            int bestDist = -1;

            // Consider all edge tiles of the room
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

        /// <summary>
        /// Computes WallFace flags for all walkable tiles adjacent to
        /// non-walkable tiles (Wall, Empty, or out-of-bounds).
        /// Also computes faces for Wall tiles adjacent to walkable tiles.
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

                    // Skip Empty tiles (shouldn't exist in our grid, but safety check)
                    if (currentType == TileType.Empty)
                        continue;

                    WallFace face = WallFace.None;

                    for (int d = 0; d < 4; d++)
                    {
                        int nx = x + dx[d];
                        int nz = z + dz[d];

                        // Out of bounds → add wall face
                        if (nx < 0 || nx >= width || nz < 0 || nz >= depth)
                        {
                            face |= dirFlags[d];
                            continue;
                        }

                        TileType neighborType = grid[nx, nz];

                        // If current tile is walkable and neighbor blocks → face
                        // If current tile is Wall and neighbor is walkable → face (wall surface)
                        bool currentIsWalkable = IsWalkableType(currentType);
                        bool neighborIsWalkable = IsWalkableType(neighborType);

                        if (currentIsWalkable && !neighborIsWalkable)
                        {
                            face |= dirFlags[d];
                        }
                        else if (!currentIsWalkable && neighborIsWalkable)
                        {
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
        /// Minimal fallback level when all generation attempts fail.
        /// Creates a tiny 3x3 solvable layout: perimeter walls, spawn center, stairs at edge.
        /// </summary>
        private static TileData[] GenerateFallback(DungeonParams parameters)
        {
            int w = Mathf.Max(3, parameters.Width);
            int d = Mathf.Max(3, parameters.Depth);

            // Fallback to a small guaranteed-solvable level
            int fw = Mathf.Min(5, w);
            int fd = Mathf.Min(5, d);

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

            // Spawn in center, stairs at bottom-right interior
            grid[fw / 2, fd / 2] = TileType.Spawn;
            grid[fw - 2, fd - 2] = TileType.Stairs;

            WallFace[,] faces = ComputeWallFaces(grid, fw, fd);

            TileData[] result = new TileData[fw * fd];
            int idx = 0;
            for (int x = 0; x < fw; x++)
                for (int z = 0; z < fd; z++)
                    result[idx++] = new TileData(x, z, grid[x, z], faces[x, z]);

            return result;
        }

        // ------------------------------------------------------------------
        // T14: Integration convenience method
        // ------------------------------------------------------------------

        /// <summary>
        /// Generates a dungeon and loads it via LevelLoader in a single coroutine-compatible step.
        /// Returns true if loading succeeded.
        /// </summary>
        public bool GenerateAndLoad(LevelLoader loader, Transform tileRoot, DungeonParams parameters,
            ITilePalette palette)
        {
            if (loader == null)
                throw new ArgumentNullException(nameof(loader));
            if (tileRoot == null)
                throw new ArgumentNullException(nameof(tileRoot));
            if (parameters == null)
                throw new ArgumentNullException(nameof(parameters));
            if (palette == null)
                throw new ArgumentNullException(nameof(palette));

            TileData[] tiles = Generate(parameters);
            return loader.LoadFromData(tiles, parameters.Width, parameters.Depth, palette, tileRoot);
        }
    }
}
