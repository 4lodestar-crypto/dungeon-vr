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
    /// Validates that generated levels pass validation and solvability checks.
    /// </summary>
    [TestFixture]
    public class DungeonGeneratorTests
    {
        /// <summary>
        /// Creates a stub ITilePalette that returns dummy prefabs for all non-Empty types.
        /// </summary>
        private static ITilePalette CreateMockPalette()
        {
            return new StubTilePalette();
        }

        /// <summary>
        /// Creates a DungeonParams with specified seed and default values.
        /// </summary>
        private static DungeonParams CreateParams(int seed = 42, int width = 32, int depth = 32)
        {
            var p = ScriptableObject.CreateInstance<DungeonParams>();
            p.Seed = seed;
            p.Width = width;
            p.Depth = depth;
            p.MinRoomCount = 3;
            p.MaxRoomCount = 6;
            p.MinRoomSize = 4;
            p.MaxRoomSize = 7;
            p.CorridorWidth = 1;
            p.PlaceWallsAroundRooms = true;
            return p;
        }

        // ---------------------------------------------------------------
        // Test 1: Default params produce a valid level
        // ---------------------------------------------------------------

        [Test]
        public void Generate_DefaultParams_ReturnsValidLevel()
        {
            // Arrange
            var generator = new DungeonGenerator();
            var parameters = CreateParams(seed: 42);
            var validator = new LevelValidator();
            ITilePalette palette = CreateMockPalette();

            // Act
            TileData[] tiles = generator.Generate(parameters);

            // Assert
            Assert.IsNotNull(tiles, "Generate() should return a non-null array");
            Assert.Greater(tiles.Length, 0, "Generated tile array should not be empty");

            int expectedTileCount = parameters.Width * parameters.Depth;
            Assert.AreEqual(expectedTileCount, tiles.Length,
                $"Expected {expectedTileCount} tiles for {parameters.Width}x{parameters.Depth} grid");

            bool valid = validator.Validate(tiles, parameters.Width, parameters.Depth,
                palette, out string[] errors);
            Assert.IsTrue(valid, "Generated level should pass validation. Errors: " +
                string.Join("; ", errors ?? new string[0]));
        }

        // ---------------------------------------------------------------
        // Test 2: 10 different seeds all produce solvable levels
        // ---------------------------------------------------------------

        [Test]
        public void Generate_10Seeds_AllSolvable()
        {
            // Arrange
            var generator = new DungeonGenerator();
            var validator = new LevelValidator();
            ITilePalette palette = CreateMockPalette();
            int[] seeds = { 1, 42, 100, 256, 999, 2024, 7777, 12345, 54321, 99999 };

            foreach (int seed in seeds)
            {
                // Arrange per seed
                var parameters = CreateParams(seed: seed);

                // Act
                TileData[] tiles = generator.Generate(parameters);

                // Assert
                Assert.IsNotNull(tiles, $"Seed {seed}: Generate() returned null");
                bool valid = validator.Validate(tiles, parameters.Width, parameters.Depth,
                    palette, out string[] errors);
                Assert.IsTrue(valid, $"Seed {seed}: Level failed validation. Errors: " +
                    string.Join("; ", errors ?? new string[0]));

                bool solvable = validator.IsSolvable(tiles, parameters.Width, parameters.Depth);
                Assert.IsTrue(solvable, $"Seed {seed}: Level is not solvable");
            }
        }

        // ---------------------------------------------------------------
        // Test 3: Different seeds produce different layouts
        // ---------------------------------------------------------------

        [Test]
        public void Generate_DifferentSeeds_DifferentLayouts()
        {
            // Arrange
            var generator = new DungeonGenerator();
            var parametersA = CreateParams(seed: 1);
            var parametersB = CreateParams(seed: 9999);

            // Act
            TileData[] tilesA = generator.Generate(parametersA);
            TileData[] tilesB = generator.Generate(parametersB);

            // Assert
            Assert.IsNotNull(tilesA);
            Assert.IsNotNull(tilesB);
            Assert.AreEqual(tilesA.Length, tilesB.Length,
                "Both generations should have same dimensions");

            // Count differences in tile types
            int differences = 0;
            for (int i = 0; i < tilesA.Length; i++)
            {
                if (tilesA[i].X != tilesB[i].X || tilesA[i].Z != tilesB[i].Z)
                    continue; // Skip out-of-order tiles; we match by position

                // Find matching tile in B by position
                var tileB = FindTileByCoord(tilesB, tilesA[i].X, tilesA[i].Z);
                if (tileB == null || tilesA[i].Type != tileB.Value.Type)
                    differences++;
            }

            // Different seeds should produce different layouts.
            // It's theoretically possible but astronomically unlikely they'd match.
            Assert.Greater(differences, 0,
                "Different seeds should produce different tile arrangements");
        }

        // ---------------------------------------------------------------
        // Test 4: Minimal size (10x10) still produces valid level
        // ---------------------------------------------------------------

        [Test]
        public void Generate_MinimalSize_Works()
        {
            // Arrange
            var generator = new DungeonGenerator();
            var parameters = CreateParams(seed: 42, width: 10, depth: 10);
            // For a 10x10 grid, use smaller rooms
            parameters.MinRoomSize = 3;
            parameters.MaxRoomSize = 4;
            parameters.MinRoomCount = 2;
            parameters.MaxRoomCount = 3;
            var validator = new LevelValidator();
            ITilePalette palette = CreateMockPalette();

            // Act
            TileData[] tiles = generator.Generate(parameters);

            // Assert
            Assert.IsNotNull(tiles, "Generate() should return a non-null array for minimal grid");
            Assert.Greater(tiles.Length, 0, "Generated tile array should not be empty");

            bool valid = validator.Validate(tiles, parameters.Width, parameters.Depth,
                palette, out string[] errors);
            Assert.IsTrue(valid, "Minimal grid should pass validation. Errors: " +
                string.Join("; ", errors ?? new string[0]));

            bool solvable = validator.IsSolvable(tiles, parameters.Width, parameters.Depth);
            Assert.IsTrue(solvable, "Minimal grid should be solvable");
        }

        // ---------------------------------------------------------------
        // Test 5: Different param values produce expected grid sizes
        // ---------------------------------------------------------------

        [Test]
        public void Generate_ParamsOverride_Respected()
        {
            // Arrange
            var generator = new DungeonGenerator();
            var validator = new LevelValidator();
            ITilePalette palette = CreateMockPalette();

            // Test with custom dimensions
            int customWidth = 20;
            int customDepth = 15;
            var parameters = CreateParams(seed: 777, width: customWidth, depth: customDepth);
            parameters.MinRoomCount = 2;
            parameters.MaxRoomCount = 4;
            parameters.MinRoomSize = 3;
            parameters.MaxRoomSize = 5;

            // Act
            TileData[] tiles = generator.Generate(parameters);

            // Assert
            Assert.IsNotNull(tiles);

            int expectedCount = customWidth * customDepth;
            Assert.AreEqual(expectedCount, tiles.Length,
                $"Expected {expectedCount} tiles for {customWidth}x{customDepth} grid, got {tiles.Length}");

            bool valid = validator.Validate(tiles, customWidth, customDepth,
                palette, out string[] errors);
            Assert.IsTrue(valid, "Custom-sized level should pass validation. Errors: " +
                string.Join("; ", errors ?? new string[0]));

            bool solvable = validator.IsSolvable(tiles, customWidth, customDepth);
            Assert.IsTrue(solvable, "Custom-sized level should be solvable");

            // Verify tile count matches grid dimensions
            int maxX = tiles.Max(t => t.X);
            int maxZ = tiles.Max(t => t.Z);
            Assert.AreEqual(customWidth - 1, maxX,
                $"Max X should be {customWidth - 1}, got {maxX}");
            Assert.AreEqual(customDepth - 1, maxZ,
                $"Max Z should be {customDepth - 1}, got {maxZ}");
        }

        // ---------------------------------------------------------------
        // Test 6: Generated level has exactly one spawn and at least one stairs
        // ---------------------------------------------------------------

        [Test]
        public void Generate_HasCorrectSpecialTiles()
        {
            // Arrange
            var generator = new DungeonGenerator();
            var parameters = CreateParams(seed: 42);

            // Act
            TileData[] tiles = generator.Generate(parameters);

            // Assert
            Assert.IsNotNull(tiles);

            int spawnCount = tiles.Count(t => t.Type == TileType.Spawn);
            int stairsCount = tiles.Count(t => t.Type == TileType.Stairs);
            int wallCount = tiles.Count(t => t.Type == TileType.Wall);

            Assert.AreEqual(1, spawnCount, "Should have exactly 1 Spawn tile");
            Assert.GreaterOrEqual(stairsCount, 1, "Should have at least 1 Stairs tile");
            Assert.Greater(wallCount, 0, "Should have at least 1 Wall tile");
        }

        // ---------------------------------------------------------------
        // Test 7: Different settings produce expected variations
        // ---------------------------------------------------------------

        [Test]
        public void Generate_NoWallsAroundRooms_StillValid()
        {
            // Arrange
            var generator = new DungeonGenerator();
            var parameters = CreateParams(seed: 42);
            parameters.PlaceWallsAroundRooms = false;
            var validator = new LevelValidator();
            ITilePalette palette = CreateMockPalette();

            // Act
            TileData[] tiles = generator.Generate(parameters);

            // Assert
            Assert.IsNotNull(tiles);

            bool valid = validator.Validate(tiles, parameters.Width, parameters.Depth,
                palette, out string[] errors);
            Assert.IsTrue(valid, "Level without room wall rings should pass validation. Errors: " +
                string.Join("; ", errors ?? new string[0]));

            bool solvable = validator.IsSolvable(tiles, parameters.Width, parameters.Depth);
            Assert.IsTrue(solvable, "Level without room wall rings should be solvable");
        }

        // ---------------------------------------------------------------
        // Test 8: Generate throws on null params
        // ---------------------------------------------------------------

        [Test]
        public void Generate_NullParams_Throws()
        {
            // Arrange
            var generator = new DungeonGenerator();

            // Act & Assert
            Assert.That(() => generator.Generate(null),
                Throws.ArgumentNullException);
        }

        // ---------------------------------------------------------------
        // Helper methods
        // ---------------------------------------------------------------

        /// <summary>Finds a TileData by (x,z) coordinates.</summary>
        private static TileData? FindTileByCoord(TileData[] tiles, int x, int z)
        {
            foreach (var t in tiles)
            {
                if (t.X == x && t.Z == z)
                    return t;
            }
            return null;
        }

        // ---------------------------------------------------------------
        // Stub implementations
        // ---------------------------------------------------------------

        /// <summary>
        /// Minimal ITilePalette stub for test isolation.
        /// Returns dummy GameObjects for all non-Empty tile types.
        /// </summary>
        private class StubTilePalette : ITilePalette
        {
            private readonly Dictionary<TileType, GameObject> _prefabs;

            public StubTilePalette()
            {
                _prefabs = new Dictionary<TileType, GameObject>();
                // Assign a dummy GameObject for each non-Empty type
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
