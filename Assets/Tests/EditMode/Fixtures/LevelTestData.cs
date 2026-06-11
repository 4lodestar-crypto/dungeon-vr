using DungeonVR.Shared.Data;
using DungeonVR.Shared.Enums;
using UnityEngine;

namespace DungeonVR.Tests.EditMode.Fixtures
{
    /// <summary>
    /// Static factory methods for creating level test data (TileData arrays and JSON strings).
    /// All grids use the same coordinate system: (x,z) where x is column, z is row.
    /// TILE_SIZE = 3.0 units per tile.
    /// </summary>
    public static class LevelTestData
    {
        /// <summary>
        /// Creates a 3x3 all-floor grid with:
        /// - Spawn at (1,1)
        /// - Stairs at (1,2)
        /// - All other tiles are Floor
        /// - No wall faces
        /// </summary>
        public static TileData[] CreateSimpleFloor()
        {
            TileData[] tiles = new TileData[9];
            int index = 0;

            for (int z = 0; z < 3; z++)
            {
                for (int x = 0; x < 3; x++)
                {
                    TileType type = TileType.Floor;
                    if (x == 1 && z == 1)
                        type = TileType.Spawn;
                    else if (x == 1 && z == 2)
                        type = TileType.Stairs;

                    tiles[index++] = new TileData(x, z, type);
                }
            }

            return tiles;
        }

        /// <summary>
        /// Creates a 5x5 grid with walls on the perimeter (TileType.Wall at edges)
        /// and an open 3x3 interior.
        /// - Spawn at center (2,2)
        /// - Stairs at corner (0,4) — inside the wall boundary
        /// - Perimeter is all Wall tiles
        /// </summary>
        public static TileData[] CreatePerimeterWalls()
        {
            const int size = 5;
            TileData[] tiles = new TileData[size * size];
            int index = 0;

            for (int z = 0; z < size; z++)
            {
                for (int x = 0; x < size; x++)
                {
                    TileType type;
                    if (x == 0 || x == size - 1 || z == 0 || z == size - 1)
                        type = TileType.Wall;
                    else if (x == 2 && z == 2)
                        type = TileType.Spawn;
                    else if (x == 0 && z == 4)
                        type = TileType.Stairs;
                    else
                        type = TileType.Floor;

                    tiles[index++] = new TileData(x, z, type);
                }
            }

            return tiles;
        }

        /// <summary>
        /// Creates a 3x3 grid where the exit (Stairs) is completely walled off
        /// and unreachable from the spawn.
        /// Layout (W=Wall, F=Floor, S=Spawn, E=Stairs/Exit):
        ///   F W F   (z=0)
        ///   W S W   (z=1)
        ///   E W F   (z=2)
        /// Spawn at (1,1) is surrounded on all 4 sides by walls.
        /// Exit at (0,2) is blocked from spawn by wall at (0,1) and (1,2).
        /// Expect IsSolvable to return false.
        /// </summary>
        public static TileData[] CreateUnsolvableFloor()
        {
            TileData[] tiles = new TileData[9];

            // Row z=0: Floor, Wall, Floor
            tiles[0] = new TileData(0, 0, TileType.Floor);
            tiles[1] = new TileData(1, 0, TileType.Wall);
            tiles[2] = new TileData(2, 0, TileType.Floor);

            // Row z=1: Wall, Spawn, Wall
            tiles[3] = new TileData(0, 1, TileType.Wall);
            tiles[4] = new TileData(1, 1, TileType.Spawn);
            tiles[5] = new TileData(2, 1, TileType.Wall);

            // Row z=2: Stairs, Wall, Floor
            tiles[6] = new TileData(0, 2, TileType.Stairs);
            tiles[7] = new TileData(1, 2, TileType.Wall);
            tiles[8] = new TileData(2, 2, TileType.Floor);

            // Spawn(1,1) isolated by walls in all 4 cardinal directions:
            //   North(1,2)=Wall, South(1,0)=Wall, East(2,1)=Wall, West(0,1)=Wall
            // Exit(0,2) reachable only from (0,1)=Wall or (1,2)=Wall — both blocked
            return tiles;
        }

        /// <summary>
        /// Creates a 3x3 grid with no spawn tile present.
        /// Has Floor and Stairs but no Spawn.
        /// Expect Validate to return false.
        /// </summary>
        public static TileData[] CreateMissingSpawnFloor()
        {
            TileData[] tiles = new TileData[9];
            int index = 0;

            for (int z = 0; z < 3; z++)
            {
                for (int x = 0; x < 3; x++)
                {
                    TileType type = TileType.Floor;
                    if (x == 1 && z == 2)
                        type = TileType.Stairs;

                    tiles[index++] = new TileData(x, z, type);
                }
            }

            return tiles;
        }

        /// <summary>
        /// Creates a 3x3 grid with no exit (Stairs) tile present.
        /// Has Floor and Spawn but no Stairs.
        /// Expect Validate to return false.
        /// </summary>
        public static TileData[] CreateNoExitFloor()
        {
            TileData[] tiles = new TileData[9];
            int index = 0;

            for (int z = 0; z < 3; z++)
            {
                for (int x = 0; x < 3; x++)
                {
                    TileType type = TileType.Floor;
                    if (x == 1 && z == 1)
                        type = TileType.Spawn;

                    tiles[index++] = new TileData(x, z, type);
                }
            }

            return tiles;
        }

        /// <summary>
        /// Returns a JSON string representing a simple 3x3 floor layout.
        /// Format: { "tiles": [...], "width": 3, "depth": 3 }
        /// Each tile: { "x": N, "z": N, "type": "Floor|Wall|Spawn|Stairs", "wallFaces": "None", "floorHeight": 0 }
        /// Used for LevelLoader deserialization testing.
        /// </summary>
        public static string CreateFloorJson()
        {
            return @"{
    ""tiles"": [
        { ""x"": 0, ""z"": 0, ""type"": ""Floor"", ""wallFaces"": ""None"", ""floorHeight"": 0 },
        { ""x"": 1, ""z"": 0, ""type"": ""Floor"", ""wallFaces"": ""None"", ""floorHeight"": 0 },
        { ""x"": 2, ""z"": 0, ""type"": ""Floor"", ""wallFaces"": ""None"", ""floorHeight"": 0 },
        { ""x"": 0, ""z"": 1, ""type"": ""Floor"", ""wallFaces"": ""None"", ""floorHeight"": 0 },
        { ""x"": 1, ""z"": 1, ""type"": ""Spawn"", ""wallFaces"": ""None"", ""floorHeight"": 0 },
        { ""x"": 2, ""z"": 1, ""type"": ""Floor"", ""wallFaces"": ""None"", ""floorHeight"": 0 },
        { ""x"": 0, ""z"": 2, ""type"": ""Floor"", ""wallFaces"": ""None"", ""floorHeight"": 0 },
        { ""x"": 1, ""z"": 2, ""type"": ""Stairs"", ""wallFaces"": ""None"", ""floorHeight"": 0 },
        { ""x"": 2, ""z"": 2, ""type"": ""Floor"", ""wallFaces"": ""None"", ""floorHeight"": 0 }
    ],
    ""width"": 3,
    ""depth"": 3
}";
        }
    }
}
