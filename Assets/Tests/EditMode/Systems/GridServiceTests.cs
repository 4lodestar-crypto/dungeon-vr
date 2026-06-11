using System.Collections.Generic;
using DungeonVR.Shared;
using DungeonVR.Shared.Data;
using DungeonVR.Shared.Enums;
using DungeonVR.Shared.Interfaces;
using NUnit.Framework;
using UnityEngine;

namespace DungeonVR.Tests.EditMode.Systems
{
    /// <summary>
    /// EditMode tests for the IGridQueryService contract (implemented by GridService in production).
    /// Uses an inline stub GridService that indexes tiles by (x,z) and determines walkability
    /// from TileType (Wall and Empty are blocked).
    /// 
    /// Production: DungeonVR.Level.Logic.GridService implements IGridQueryService.
    /// </summary>
    [TestFixture]
    public class GridServiceTests
    {
        /// <summary>
        /// Registers a set of tiles and verifies that walkable/blocked queries match expectations.
        /// Creates a 3x3 grid with spawn at (1,1), stairs at (1,2), walls on perimeter,
        /// and verifies IsWalkable for each position.
        /// </summary>
        [Test]
        public void GridService_RegisterTiles_WalkableReturnsCorrect()
        {
            // Arrange
            TileData[] tiles = new TileData[]
            {
                new TileData(0, 0, TileType.Wall),
                new TileData(1, 0, TileType.Wall),
                new TileData(2, 0, TileType.Wall),
                new TileData(0, 1, TileType.Wall),
                new TileData(1, 1, TileType.Spawn),
                new TileData(2, 1, TileType.Wall),
                new TileData(0, 2, TileType.Wall),
                new TileData(1, 2, TileType.Stairs),
                new TileData(2, 2, TileType.Wall),
            };

            StubGridService gridService = new StubGridService(tiles, width: 3, depth: 3);

            // Act & Assert
            // Walls and Empty should not be walkable
            Assert.IsFalse(gridService.IsWalkable(0, 0), "(0,0) Wall should be blocked");
            Assert.IsFalse(gridService.IsWalkable(1, 0), "(1,0) Wall should be blocked");
            Assert.IsFalse(gridService.IsWalkable(0, 1), "(0,1) Wall should be blocked");
            Assert.IsFalse(gridService.IsWalkable(2, 1), "(2,1) Wall should be blocked");
            Assert.IsFalse(gridService.IsWalkable(0, 2), "(0,2) Wall should be blocked");
            Assert.IsFalse(gridService.IsWalkable(2, 2), "(2,2) Wall should be blocked");

            // Floor, Spawn, Stairs, Door, Trap, Altar should be walkable
            Assert.IsTrue(gridService.IsWalkable(1, 1), "(1,1) Spawn should be walkable");
            Assert.IsTrue(gridService.IsWalkable(1, 2), "(1,2) Stairs should be walkable");
        }

        /// <summary>
        /// Verifies GetTileCenter returns correct world-space positions based on
        /// GameConstants.TILE_SIZE (3.0 units). Matches the TileData.WorldCenter formula:
        /// (X * TILE_SIZE + TILE_SIZE * 0.5f, 0, Z * TILE_SIZE + TILE_SIZE * 0.5f)
        /// </summary>
        [Test]
        public void GridService_GetTileCenter_ReturnsCorrectPosition()
        {
            // Arrange
            TileData[] tiles = new TileData[]
            {
                new TileData(0, 0, TileType.Floor),
                new TileData(2, 3, TileType.Floor),
                new TileData(5, 7, TileType.Floor),
            };

            StubGridService gridService = new StubGridService(tiles, width: 10, depth: 10);

            // Act
            Vector3 center00 = gridService.GetTileCenter(0, 0);
            Vector3 center23 = gridService.GetTileCenter(2, 3);
            Vector3 center57 = gridService.GetTileCenter(5, 7);

            // Assert
            // (0,0): (0*3 + 1.5, 0, 0*3 + 1.5) = (1.5, 0, 1.5)
            Assert.AreEqual(1.5f, center00.x, 1e-6f);
            Assert.AreEqual(0f, center00.y, 1e-6f);
            Assert.AreEqual(1.5f, center00.z, 1e-6f);

            // (2,3): (2*3 + 1.5, 0, 3*3 + 1.5) = (7.5, 0, 10.5)
            Assert.AreEqual(7.5f, center23.x, 1e-6f);
            Assert.AreEqual(0f, center23.y, 1e-6f);
            Assert.AreEqual(10.5f, center23.z, 1e-6f);

            // (5,7): (5*3 + 1.5, 0, 7*3 + 1.5) = (16.5, 0, 22.5)
            Assert.AreEqual(16.5f, center57.x, 1e-6f);
            Assert.AreEqual(0f, center57.y, 1e-6f);
            Assert.AreEqual(22.5f, center57.z, 1e-6f);
        }

        /// <summary>
        /// Out-of-bounds coordinates must return blocked (IsWalkable = false).
        /// Tests negative coordinates and coordinates beyond the grid dimensions.
        /// </summary>
        [Test]
        public void GridService_OutOfBounds_ReturnsBlocked()
        {
            // Arrange
            TileData[] tiles = LevelTestData.CreateSimpleFloor(); // 3x3 grid
            StubGridService gridService = new StubGridService(tiles, width: 3, depth: 3);

            // Act & Assert — negative coordinates
            Assert.IsFalse(gridService.IsWalkable(-1, 0), "(-1,0) out of bounds — should be blocked");
            Assert.IsFalse(gridService.IsWalkable(0, -1), "(0,-1) out of bounds — should be blocked");
            Assert.IsFalse(gridService.IsWalkable(-1, -1), "(-1,-1) out of bounds — should be blocked");

            // Act & Assert — beyond grid dimensions
            Assert.IsFalse(gridService.IsWalkable(3, 0), "(3,0) beyond 3x3 width — should be blocked");
            Assert.IsFalse(gridService.IsWalkable(0, 3), "(0,3) beyond 3x3 depth — should be blocked");
            Assert.IsFalse(gridService.IsWalkable(99, 99), "(99,99) far out of bounds — should be blocked");

            // Verify a valid coordinate still works
            Assert.IsTrue(gridService.IsWalkable(1, 1), "(1,1) Spawn should still be walkable");
        }

        /// <summary>
        /// GridService must report correct Width and Depth dimensions.
        /// </summary>
        [Test]
        public void GridService_WidthDepth_MatchesRegisteredDimensions()
        {
            // Arrange
            TileData[] tiles = LevelTestData.CreatePerimeterWalls(); // 5x5
            StubGridService gridService = new StubGridService(tiles, width: 5, depth: 5);

            // Act & Assert
            Assert.AreEqual(5, gridService.Width, "Width should match registered dimension");
            Assert.AreEqual(5, gridService.Depth, "Depth should match registered dimension");
        }

        /// <summary>
        /// Empty tiles (TileType.Empty) must be treated as blocked / not walkable.
        /// </summary>
        [Test]
        public void GridService_EmptyTiles_AreBlocked()
        {
            // Arrange
            TileData[] tiles = new TileData[]
            {
                new TileData(0, 0, TileType.Empty),
                new TileData(1, 0, TileType.Floor),
                new TileData(0, 1, TileType.Empty),
                new TileData(1, 1, TileType.Empty),
            };

            StubGridService gridService = new StubGridService(tiles, width: 2, depth: 2);

            // Act & Assert
            Assert.IsFalse(gridService.IsWalkable(0, 0), "Empty tile should be blocked");
            Assert.IsFalse(gridService.IsWalkable(0, 1), "Empty tile should be blocked");
            Assert.IsFalse(gridService.IsWalkable(1, 1), "Empty tile should be blocked");
            Assert.IsTrue(gridService.IsWalkable(1, 0), "Floor tile should be walkable");
        }

        // ---------------------------------------------------------------
        // Stub implementation — swap for DungeonVR.Level.Logic.GridService
        // when the production class ships.
        // ---------------------------------------------------------------

        /// <summary>
        /// Stub IGridQueryService backed by a TileData[] array.
        /// Walkable if TileType is not Wall and not Empty.
        /// GetTileCenter uses the standard formula matching TileData.WorldCenter.
        /// </summary>
        private class StubGridService : IGridQueryService
        {
            private readonly Dictionary<(int x, int z), TileData> _tiles = new Dictionary<(int x, int z), TileData>();
            private readonly HashSet<(int x, int z)> _walkableCache = new HashSet<(int x, int z)>();

            public int Width { get; }
            public int Depth { get; }

            public StubGridService(TileData[] tiles, int width, int depth)
            {
                Width = width;
                Depth = depth;

                foreach (TileData tile in tiles)
                {
                    _tiles[(tile.X, tile.Z)] = tile;

                    // Walkable = not Wall and not Empty
                    if (tile.Type != TileType.Wall && tile.Type != TileType.Empty)
                    {
                        _walkableCache.Add((tile.X, tile.Z));
                    }
                }
            }

            public bool IsWalkable(int gridX, int gridZ)
            {
                // Out of bounds = blocked
                if (gridX < 0 || gridX >= Width || gridZ < 0 || gridZ >= Depth)
                    return false;

                return _walkableCache.Contains((gridX, gridZ));
            }

            public Vector3 GetTileCenter(int gridX, int gridZ)
            {
                float halfTile = GameConstants.TILE_SIZE * 0.5f;
                float worldX = gridX * GameConstants.TILE_SIZE + halfTile;
                float worldZ = gridZ * GameConstants.TILE_SIZE + halfTile;

                // Check for FloorHeight on registered tiles
                if (_tiles.TryGetValue((gridX, gridZ), out TileData tileData))
                {
                    return new Vector3(worldX, tileData.FloorHeight, worldZ);
                }

                return new Vector3(worldX, 0f, worldZ);
            }
        }
    }
}
