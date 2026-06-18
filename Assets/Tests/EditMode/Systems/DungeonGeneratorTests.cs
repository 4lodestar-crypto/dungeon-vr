using System;
using System.Collections.Generic;
using System.Linq;
using DungeonVR.Level.Data;
using DungeonVR.Level.Interfaces;
using DungeonVR.Level.Logic;
using DungeonVR.Shared.Data;
using DungeonVR.Shared.Enums;
using NUnit.Framework;
using UnityEngine;

namespace DungeonVR.Tests.EditMode.Systems
{
    /// <summary>
    /// EditMode tests for the procedural DungeonGenerator.
    /// Validates default generation, solvability across seeds,
    /// layout uniqueness, and edge-case parameter configurations.
    /// </summary>
    [TestFixture]
    public class DungeonGeneratorTests
    {
        /// <summary>
        /// Creates a stub ITilePalette that returns dummy prefabs for all non-Empty types.
        /// Required because LevelValidator.Validate() will reject a null palette.
        /// </summary>
        private static ITilePalette CreateMockPalette()
        {
            return new StubTilePalette();
        }

        /// <summary>
        /// Creates a DungeonParams ScriptableObject with the given overrides
        /// and sensible defaults for other fields.
        /// </summary>
        private static DungeonParams CreateParams(int seed = 42, int width = 32, int depth = 32)
        {
            var p = ScriptableObject.CreateInstance<DungeonParams>();
            p.Seed = seed;
            p.Width = width;
            p.Depth = depth;
            p.MinRoomCount = 3;
            p.MaxRoomCount = 8;
            p.MinRoomSize = 4;
            p.MaxRoomSize = 8;
            p.CorridorWidth = 1;
            p.PlaceWallsAroundRooms = true;
            return p;
        }

        // ---------------------------------------------------------------
        // Test 1 — Default params produce a valid, structurally-sound dungeon
        // ---------------------------------------------------------------

        [Test]
        public void DefaultParams_ProducesValidDungeon()
        {
            // Arrange
            var parameters = CreateParams(); // all defaults: 32x32, seed 42
            var validator = new LevelValidator();
            ITilePalette palette = CreateMockPalette();

            // Act
            TileData[] tiles = DungeonGenerator.Generate(parameters);

            // Assert
            Assert.IsNotNull(tiles, "Generate() should return a non-null array");
            Assert.Greater(tiles.Length, 0, "Generated tile array should not be empty");

            int expectedTileCount = parameters.Width * parameters.Depth;
            Assert.AreEqual(expectedTileCount, tiles.Length,
                $"Expected {expectedTileCount} tiles for {parameters.Width}x{parameters.Depth} grid");

            bool valid = validator.Validate(tiles, parameters.Width, parameters.Depth,
                palette, out string[] errors);
            Assert.IsTrue(valid, "Default params level should pass validation. Errors: " +
                string.Join("; ", errors ?? new string[0]));
        }

        // ---------------------------------------------------------------
        // Test 2 — 10 seeds (0-9) all produce solvable dungeons
        // ---------------------------------------------------------------

        [Test]
        public void TenSeeds_AllSolvable(
            [Values(0, 1, 2, 3, 4, 5, 6, 7, 8, 9)] int seed)
        {
            // Arrange
            var validator = new LevelValidator();
            var parameters = CreateParams(seed: seed);

            // Act
            TileData[] tiles = DungeonGenerator.Generate(parameters);

            // Assert
            Assert.IsNotNull(tiles, $"Seed {seed}: Generate() returned null");
            Assert.AreEqual(parameters.Width * parameters.Depth, tiles.Length,
                $"Seed {seed}: Unexpected tile count");

            bool solvable = validator.IsSolvable(tiles, parameters.Width, parameters.Depth);
            Assert.IsTrue(solvable, $"Seed {seed}: Level is not solvable");
        }

        // ---------------------------------------------------------------
        // Test 3 — Different seeds produce structurally different layouts
        // ---------------------------------------------------------------

        [Test]
        public void DifferentSeeds_DifferentLayouts()
        {
            // Arrange
            var paramsA = CreateParams(seed: 1);
            var paramsB = CreateParams(seed: 2);

            // Act
            TileData[] tilesA = DungeonGenerator.Generate(paramsA);
            TileData[] tilesB = DungeonGenerator.Generate(paramsB);

            // Assert
            Assert.IsNotNull(tilesA);
            Assert.IsNotNull(tilesB);
            Assert.AreEqual(tilesA.Length, tilesB.Length,
                "Both generations should have the same array length for equal dimensions");

            // Compare tile types at each position; different seeds should differ somewhere
            bool identical = tilesA.SequenceEqual(tilesB, new TileTypeComparer());
            Assert.IsFalse(identical,
                "Seed 1 and seed 2 should produce different tile arrangements");
        }

        // ---------------------------------------------------------------
        // Test 4 — Minimal 10x10 grid still generates a valid solvable dungeon
        // ---------------------------------------------------------------

        [Test]
        public void MinimalGrid_10x10_Works()
        {
            // Arrange
            var parameters = CreateParams(seed: 42, width: 10, depth: 10);
            parameters.MinRoomCount = 1;
            parameters.MaxRoomCount = 3;
            parameters.MinRoomSize = 3;
            parameters.MaxRoomSize = 4;
            var validator = new LevelValidator();
            ITilePalette palette = CreateMockPalette();

            // Act
            TileData[] tiles = DungeonGenerator.Generate(parameters);

            // Assert
            Assert.IsNotNull(tiles, "Generate() should return non-null for 10x10 grid");
            Assert.Greater(tiles.Length, 0, "Generated tile array should not be empty");

            bool valid = validator.Validate(tiles, parameters.Width, parameters.Depth,
                palette, out string[] errors);
            Assert.IsTrue(valid, "Minimal 10x10 grid should pass validation. Errors: " +
                string.Join("; ", errors ?? new string[0]));

            bool solvable = validator.IsSolvable(tiles, parameters.Width, parameters.Depth);
            Assert.IsTrue(solvable, "Minimal 10x10 grid should be solvable");
        }

        // ---------------------------------------------------------------
        // Test 5 — Width/Depth/Seed overrides produce matching grid size
        // ---------------------------------------------------------------

        [Test]
        public void ParamOverrides_Respected()
        {
            // Arrange
            int customWidth = 20;
            int customDepth = 20;
            int customSeed = 12345;
            var parameters = CreateParams(seed: customSeed, width: customWidth, depth: customDepth);

            // Act
            TileData[] tiles = DungeonGenerator.Generate(parameters);

            // Assert — tile count matches width * depth
            Assert.IsNotNull(tiles);
            int expectedCount = customWidth * customDepth;
            Assert.AreEqual(expectedCount, tiles.Length,
                $"Expected {expectedCount} tiles for {customWidth}x{customDepth} grid, got {tiles.Length}");

            // Verify coordinate bounds match
            int maxX = tiles.Max(t => t.X);
            int maxZ = tiles.Max(t => t.Z);
            Assert.AreEqual(customWidth - 1, maxX,
                $"Max X should be {customWidth - 1}, got {maxX}");
            Assert.AreEqual(customDepth - 1, maxZ,
                $"Max Z should be {customDepth - 1}, got {maxZ}");
        }

        // ---------------------------------------------------------------
        // Test 6 — Generated level has exactly 1 Spawn and at least 1 Stairs
        // ---------------------------------------------------------------

        [Test]
        public void SpecialTileCounts_Correct()
        {
            // Arrange
            var parameters = CreateParams(seed: 42);

            // Act
            TileData[] tiles = DungeonGenerator.Generate(parameters);

            // Assert
            Assert.IsNotNull(tiles);

            int spawnCount = tiles.Count(t => t.Type == TileType.Spawn);
            int stairsCount = tiles.Count(t => t.Type == TileType.Stairs);

            Assert.AreEqual(1, spawnCount, "Exactly 1 Spawn tile is required");
            Assert.GreaterOrEqual(stairsCount, 1, "At least 1 Stairs tile is required");
        }

        // ---------------------------------------------------------------
        // Test 7 — PlaceWallsAroundRooms=false still produces valid level
        // ---------------------------------------------------------------

        [Test]
        public void NoWallsAroundRooms_StillValid()
        {
            // Arrange
            var parameters = CreateParams(seed: 42);
            parameters.PlaceWallsAroundRooms = false;
            var validator = new LevelValidator();
            ITilePalette palette = CreateMockPalette();

            // Act
            TileData[] tiles = DungeonGenerator.Generate(parameters);

            // Assert
            Assert.IsNotNull(tiles);

            bool valid = validator.Validate(tiles, parameters.Width, parameters.Depth,
                palette, out string[] errors);
            Assert.IsTrue(valid,
                "Level without room-wall rings should pass validation. Errors: " +
                string.Join("; ", errors ?? new string[0]));

            bool solvable = validator.IsSolvable(tiles, parameters.Width, parameters.Depth);
            Assert.IsTrue(solvable,
                "Level without room-wall rings should be solvable");
        }

        // ---------------------------------------------------------------
        // Test 8 — Generate(null) throws ArgumentNullException
        // ---------------------------------------------------------------

        [Test]
        public void NullParams_Throws()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => DungeonGenerator.Generate(null));
        }

        // ---------------------------------------------------------------
        // Helper: IEqualityComparer<TileData> that compares by position and type
        // ---------------------------------------------------------------

        private class TileTypeComparer : IEqualityComparer<TileData>
        {
            public bool Equals(TileData a, TileData b)
            {
                // Compare by position and type — same coordinate must have same type
                return a.X == b.X && a.Z == b.Z && a.Type == b.Type;
            }

            public int GetHashCode(TileData obj)
            {
                return HashCode.Combine(obj.X, obj.Z, obj.Type);
            }
        }

        // ---------------------------------------------------------------
        // Stub ITilePalette implementation for test isolation
        // ---------------------------------------------------------------

        private class StubTilePalette : ITilePalette
        {
            private readonly Dictionary<TileType, GameObject> _prefabs;

            public StubTilePalette()
            {
                _prefabs = new Dictionary<TileType, GameObject>();
                foreach (TileType type in System.Enum.GetValues(typeof(TileType)))
                {
                    if (type != TileType.Empty)
                    {
                        _prefabs[type] = new GameObject($"Stub_{type}");
                    }
                }
            }

            public bool IsComplete => true;

            public GameObject GetPrefab(TileType type)
            {
                _prefabs.TryGetValue(type, out GameObject prefab);
                return prefab;
            }
        }
    }
}
