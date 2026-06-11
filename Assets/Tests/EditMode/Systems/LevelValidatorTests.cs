using System.Collections.Generic;
using DungeonVR.Level.Interfaces;
using DungeonVR.Shared.Data;
using DungeonVR.Shared.Enums;
using DungeonVR.Tests.EditMode.Fixtures;
using NUnit.Framework;
using UnityEngine;

namespace DungeonVR.Tests.EditMode.Systems
{
    /// <summary>
    /// EditMode tests for the ILevelValidator contract.
    /// Uses a minimal inline stub implementation since the production
    /// LevelValidator lives in DungeonVR.Level.Logic.
    /// </summary>
    [TestFixture]
    public class LevelValidatorTests
    {
        /// <summary>
        /// Creates a stub ITilePalette with all prefabs assigned (IsComplete = true).
        /// </summary>
        private static ITilePalette CreateCompletePalette()
        {
            return new StubTilePalette(isComplete: true);
        }

        /// <summary>
        /// Creates a stub ILevelValidator with standard validation logic.
        /// </summary>
        private static ILevelValidator CreateValidator()
        {
            return new StubLevelValidator();
        }

        /// <summary>
        /// A simple 3x3 all-floor grid with spawn at (1,1) and stairs at (1,2)
        /// should pass validation.
        /// </summary>
        [Test]
        public void Validate_SimpleFloor_ReturnsTrue()
        {
            // Arrange
            TileData[] tiles = LevelTestData.CreateSimpleFloor();
            ILevelValidator validator = CreateValidator();
            ITilePalette palette = CreateCompletePalette();

            // Act
            bool result = validator.Validate(tiles, width: 3, depth: 3, palette, out string[] errors);

            // Assert
            Assert.IsTrue(result, "Simple floor with spawn and stairs should validate successfully");
            Assert.IsTrue(errors == null || errors.Length == 0, "No errors expected for valid floor");
        }

        /// <summary>
        /// A grid with no Spawn tile should fail validation.
        /// </summary>
        [Test]
        public void Validate_MissingSpawn_ReturnsFalse()
        {
            // Arrange
            TileData[] tiles = LevelTestData.CreateMissingSpawnFloor();
            ILevelValidator validator = CreateValidator();
            ITilePalette palette = CreateCompletePalette();

            // Act
            bool result = validator.Validate(tiles, width: 3, depth: 3, palette, out string[] errors);

            // Assert
            Assert.IsFalse(result, "Floor without a Spawn tile should fail validation");
            Assert.IsNotNull(errors, "Errors array should be provided when validation fails");
            Assert.Greater(errors.Length, 0, "At least one error should describe the missing spawn");
            StringAssert.Contains("Spawn", errors[0], "Error should mention the missing spawn tile");
        }

        /// <summary>
        /// A grid with no Stairs (exit) tile should fail validation.
        /// </summary>
        [Test]
        public void Validate_NoExit_ReturnsFalse()
        {
            // Arrange
            TileData[] tiles = LevelTestData.CreateNoExitFloor();
            ILevelValidator validator = CreateValidator();
            ITilePalette palette = CreateCompletePalette();

            // Act
            bool result = validator.Validate(tiles, width: 3, depth: 3, palette, out string[] errors);

            // Assert
            Assert.IsFalse(result, "Floor without a Stairs (exit) tile should fail validation");
            Assert.IsNotNull(errors, "Errors array should be provided when validation fails");
            Assert.Greater(errors.Length, 0, "At least one error should describe the missing exit");
            StringAssert.Contains("Stairs", errors[0], "Error should mention the missing stairs/exit tile");
        }

        /// <summary>
        /// An incomplete palette (missing prefabs) should cause validation to fail.
        /// </summary>
        [Test]
        public void Validate_IncompletePalette_ReturnsFalse()
        {
            // Arrange
            TileData[] tiles = LevelTestData.CreateSimpleFloor();
            ILevelValidator validator = CreateValidator();
            ITilePalette incompletePalette = new StubTilePalette(isComplete: false);

            // Act
            bool result = validator.Validate(tiles, width: 3, depth: 3, incompletePalette, out string[] errors);

            // Assert
            Assert.IsFalse(result, "Validation should fail when the tile palette is incomplete");
            Assert.IsNotNull(errors, "Errors array should be provided when validation fails");
            Assert.Greater(errors.Length, 0, "At least one error should describe the incomplete palette");
        }

        /// <summary>
        /// A simple 3x3 floor with spawn at (1,1) and stairs at (1,2) should be solvable
        /// (exit reachable from spawn via walkable tiles).
        /// </summary>
        [Test]
        public void IsSolvable_SimpleFloor_ReturnsTrue()
        {
            // Arrange
            TileData[] tiles = LevelTestData.CreateSimpleFloor();
            ILevelValidator validator = CreateValidator();

            // Act
            bool solvable = validator.IsSolvable(tiles, width: 3, depth: 3);

            // Assert
            Assert.IsTrue(solvable, "Simple floor with spawn at (1,1) and stairs at (1,2) should be solvable");
        }

        /// <summary>
        /// A grid where the exit is walled off and unreachable should not be solvable.
        /// </summary>
        [Test]
        public void IsSolvable_WalledOffExit_ReturnsFalse()
        {
            // Arrange
            TileData[] tiles = LevelTestData.CreateUnsolvableFloor();
            ILevelValidator validator = CreateValidator();

            // Act
            bool solvable = validator.IsSolvable(tiles, width: 3, depth: 3);

            // Assert
            Assert.IsFalse(solvable, "Grid with exit walled off from spawn should not be solvable");
        }

        /// <summary>
        /// A grid with no spawn tile should still be IsSolvable checks gracefully (returns false).
        /// </summary>
        [Test]
        public void IsSolvable_MissingSpawn_ReturnsFalse()
        {
            // Arrange
            TileData[] tiles = LevelTestData.CreateMissingSpawnFloor();
            ILevelValidator validator = CreateValidator();

            // Act
            bool solvable = validator.IsSolvable(tiles, width: 3, depth: 3);

            // Assert
            Assert.IsFalse(solvable, "Grid without a spawn tile cannot be solvable");
        }

        /// <summary>
        /// A grid with no exit (stairs) should still be IsSolvable checks gracefully (returns false).
        /// </summary>
        [Test]
        public void IsSolvable_NoExit_ReturnsFalse()
        {
            // Arrange
            TileData[] tiles = LevelTestData.CreateNoExitFloor();
            ILevelValidator validator = CreateValidator();

            // Act
            bool solvable = validator.IsSolvable(tiles, width: 3, depth: 3);

            // Assert
            Assert.IsFalse(solvable, "Grid without an exit tile cannot be solvable");
        }

        /// <summary>
        /// Perimeter-walled 5x5 grid with spawn inside and stairs at a corner
        /// should be solvable (open interior path exists).
        /// </summary>
        [Test]
        public void IsSolvable_PerimeterWalls_ReturnsTrue()
        {
            // Arrange
            TileData[] tiles = LevelTestData.CreatePerimeterWalls();
            ILevelValidator validator = CreateValidator();

            // Act
            bool solvable = validator.IsSolvable(tiles, width: 5, depth: 5);

            // Assert
            Assert.IsTrue(solvable, "Perimeter-walled 5x5 with interior spawn and corner stairs should be solvable");
        }

        // ---------------------------------------------------------------
        // Stub implementations (inline — replaced by production code in V1)
        // ---------------------------------------------------------------

        /// <summary>
        /// Minimal ITilePalette stub for test isolation.
        /// </summary>
        private class StubTilePalette : ITilePalette
        {
            private readonly bool _isComplete;

            public StubTilePalette(bool isComplete)
            {
                _isComplete = isComplete;
            }

            public bool IsComplete => _isComplete;

            public GameObject GetPrefab(TileType type)
            {
                // Return a non-null dummy for any non-Empty type when complete
                if (_isComplete && type != TileType.Empty)
                    return new GameObject($"Stub_{type}");
                return null;
            }
        }

        /// <summary>
        /// Minimal ILevelValidator stub implementing:
        /// - Validate: checks for Spawn, Stairs, palette completeness
        /// - IsSolvable: BFS from spawn to stairs, walkable = !Wall and !Empty
        /// 
        /// When DungeonVR.Level.Logic.LevelValidator ships, swap this for the real implementation.
        /// </summary>
        private class StubLevelValidator : ILevelValidator
        {
            public bool Validate(TileData[] tiles, int width, int depth, ITilePalette palette, out string[] errors)
            {
                var errorList = new List<string>();

                // Check palette completeness
                if (!palette.IsComplete)
                {
                    errorList.Add("Tile palette is incomplete — some TileType values lack prefab assignments.");
                }

                // Check for spawn tile
                bool hasSpawn = false;
                foreach (var tile in tiles)
                {
                    if (tile.Type == TileType.Spawn)
                    {
                        hasSpawn = true;
                        break;
                    }
                }

                if (!hasSpawn)
                {
                    errorList.Add("Level has no Spawn tile — exactly one Spawn tile is required.");
                }

                // Check for exit tile
                bool hasStairs = false;
                foreach (var tile in tiles)
                {
                    if (tile.Type == TileType.Stairs)
                    {
                        hasStairs = true;
                        break;
                    }
                }

                if (!hasStairs)
                {
                    errorList.Add("Level has no Stairs (exit) tile — exactly one Stairs tile is required.");
                }

                if (errorList.Count > 0)
                {
                    errors = errorList.ToArray();
                    return false;
                }

                errors = null;
                return true;
            }

            public bool IsSolvable(TileData[] tiles, int width, int depth)
            {
                // Build a lookup for quick tile access
                var tileByCoord = new Dictionary<(int x, int z), TileData>();
                TileData? spawn = null;
                TileData? stairs = null;

                foreach (var tile in tiles)
                {
                    tileByCoord[(tile.X, tile.Z)] = tile;

                    if (tile.Type == TileType.Spawn)
                        spawn = tile;
                    else if (tile.Type == TileType.Stairs)
                        stairs = tile;
                }

                if (spawn == null || stairs == null)
                    return false;

                // BFS from spawn to stairs
                var visited = new HashSet<(int x, int z)>();
                var queue = new Queue<(int x, int z)>();
                queue.Enqueue((spawn.Value.X, spawn.Value.Z));
                visited.Add((spawn.Value.X, spawn.Value.Z));

                // Cardinal directions: North (+Z), South (-Z), East (+X), West (-X)
                int[] dx = { 0, 0, 1, -1 };
                int[] dz = { 1, -1, 0, 0 };

                while (queue.Count > 0)
                {
                    var current = queue.Dequeue();

                    if (current.x == stairs.Value.X && current.z == stairs.Value.Z)
                        return true;

                    for (int i = 0; i < 4; i++)
                    {
                        int nx = current.x + dx[i];
                        int nz = current.z + dz[i];

                        // Bounds check
                        if (nx < 0 || nx >= width || nz < 0 || nz >= depth)
                            continue;

                        // Already visited
                        if (visited.Contains((nx, nz)))
                            continue;

                        // Check walkability (Wall and Empty are blocked)
                        if (tileByCoord.TryGetValue((nx, nz), out TileData neighbor))
                        {
                            if (neighbor.Type == TileType.Wall || neighbor.Type == TileType.Empty)
                                continue;
                        }
                        else
                        {
                            // No tile data — treat as blocked
                            continue;
                        }

                        visited.Add((nx, nz));
                        queue.Enqueue((nx, nz));
                    }
                }

                return false;
            }
        }
    }
}
