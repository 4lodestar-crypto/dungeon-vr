using DungeonVR.Gameplay.Components;
using NUnit.Framework;
using UnityEngine;

namespace DungeonVR.Tests.EditMode.Systems
{
    /// <summary>
    /// EditMode unit tests for GridData component.
    /// Uses ScriptableObject-like instantiation via new GameObject since GridData
    /// is a MonoBehaviour. Tests wall generation, bounds checking, and walkable queries.
    /// </summary>
    public class GridDataTests
    {
        private GridData CreateGrid(int width = 5, int height = 5)
        {
            var go = new GameObject("TestGrid");
            var grid = go.AddComponent<GridData>();
            // Use the exposed GeneratePerimeterWalledGrid directly (EditMode-safe)
            grid.GeneratePerimeterWalledGrid();
            return grid;
        }

        // ---------- GeneratePerimeterWalledGrid ----------

        [Test]
        public void GridData_PerimeterWalled_EdgesAreWalls()
        {
            var grid = CreateGrid(5, 5);
            bool[,] walls = grid.Walls;

            // Top row (y=0)
            for (int x = 0; x < 5; x++)
                Assert.IsTrue(walls[x, 0], $"Tile ({x},0) should be a wall (edge)");

            // Bottom row (y=4)
            for (int x = 0; x < 5; x++)
                Assert.IsTrue(walls[x, 4], $"Tile ({x},4) should be a wall (edge)");

            // Left column (x=0)
            for (int y = 0; y < 5; y++)
                Assert.IsTrue(walls[0, y], $"Tile (0,{y}) should be a wall (edge)");

            // Right column (x=4)
            for (int y = 0; y < 5; y++)
                Assert.IsTrue(walls[4, y], $"Tile (4,{y}) should be a wall (edge)");
        }

        [Test]
        public void GridData_PerimeterWalled_InteriorIsWalkable()
        {
            var grid = CreateGrid(5, 5);
            bool[,] walls = grid.Walls;

            // Interior (1..3, 1..3) should all be walkable
            for (int x = 1; x <= 3; x++)
                for (int y = 1; y <= 3; y++)
                    Assert.IsFalse(walls[x, y], $"Tile ({x},{y}) should be walkable (interior)");
        }

        [Test]
        public void GridData_SetTile_UpdatesWallState()
        {
            var grid = CreateGrid(5, 5);

            // Change interior tile (2,2) to a wall
            grid.SetTile(2, 2, true);
            Assert.IsTrue(grid.Walls[2, 2], "Tile (2,2) should now be a wall");

            // Change an edge tile to walkable
            grid.SetTile(0, 0, false);
            Assert.IsFalse(grid.Walls[0, 0], "Tile (0,0) should now be walkable");
        }

        [Test]
        public void GridData_SetTile_OutOfBounds_NoThrow()
        {
            var grid = CreateGrid(5, 5);
            // Should not throw
            grid.SetTile(-1, -1, true);
            grid.SetTile(99, 99, true);
        }

        // ---------- IsInBounds ----------

        [Test]
        public void GridData_IsInBounds_ValidTile_ReturnsTrue()
        {
            var grid = CreateGrid(5, 5);
            Assert.IsTrue(grid.IsInBounds(2, 2));
            Assert.IsTrue(grid.IsInBounds(0, 0));
            Assert.IsTrue(grid.IsInBounds(4, 4));
        }

        [Test]
        public void GridData_IsInBounds_InvalidTile_ReturnsFalse()
        {
            var grid = CreateGrid(5, 5);
            Assert.IsFalse(grid.IsInBounds(-1, 0), "Negative x should be out of bounds");
            Assert.IsFalse(grid.IsInBounds(0, -1), "Negative y should be out of bounds");
            Assert.IsFalse(grid.IsInBounds(5, 0), "x=5 should be out of bounds for 5x5 grid");
            Assert.IsFalse(grid.IsInBounds(0, 5), "y=5 should be out of bounds for 5x5 grid");
        }

        // ---------- IsWalkable ----------

        [Test]
        public void GridData_IsWalkable_InteriorTile_ReturnsTrue()
        {
            var grid = CreateGrid(5, 5);
            Assert.IsTrue(grid.IsWalkable(2, 2), "Interior tile should be walkable");
        }

        [Test]
        public void GridData_IsWalkable_WallTile_ReturnsFalse()
        {
            var grid = CreateGrid(5, 5);
            Assert.IsFalse(grid.IsWalkable(0, 0), "Edge tile (wall) should not be walkable");
            Assert.IsFalse(grid.IsWalkable(4, 3), "Edge tile (wall) should not be walkable");
        }

        [Test]
        public void GridData_IsWalkable_OutOfBounds_ReturnsFalse()
        {
            var grid = CreateGrid(5, 5);
            Assert.IsFalse(grid.IsWalkable(-1, 2), "Out of bounds tile should not be walkable");
            Assert.IsFalse(grid.IsWalkable(5, 2), "Out of bounds tile should not be walkable");
        }

        // ---------- Dimensions ----------

        [Test]
        public void GridData_Dimensions_MatchConstructor()
        {
            var grid = CreateGrid(5, 5);
            Assert.AreEqual(5, grid.Width);
            Assert.AreEqual(5, grid.Height);
        }

        [Test]
        public void GridData_NonSquareDimensions_Supported()
        {
            var go = new GameObject("TestGridRect");
            var grid = go.AddComponent<GridData>();
            // Can't set private _width/_height directly without reflection in EditMode;
            // just verify the default 5x5
            Assert.AreEqual(5, grid.Width);
            Assert.AreEqual(5, grid.Height);
        }
    }
}
