using DungeonVR.AI.Logic;
using DungeonVR.Shared;
using DungeonVR.Shared.Data;
using DungeonVR.Tests.EditMode.Fixtures;
using NUnit.Framework;
using System.Collections.Generic;

namespace DungeonVR.Tests.EditMode.AI
{
    /// <summary>
    /// EditMode unit tests for AStarGridPathfinder.
    /// Verifies pathfinding correctness, cache behavior, and edge cases.
    /// All tests are deterministic — no Physics queries, no UnityEngine.Random.
    /// </summary>
    [TestFixture]
    public class AStarPathfinderTests
    {
        private TestGridBuilder.GridQueryAdapter CreateGrid(int width, int depth, bool[,] walls)
        {
            var gridData = new GridData(width, depth, walls);
            return new TestGridBuilder.GridQueryAdapter(gridData);
        }

        /// <summary>
        /// Creates a 5x5 open grid (no walls) for straight-line path tests.
        /// </summary>
        private TestGridBuilder.GridQueryAdapter CreateOpenGrid()
        {
            int size = 5;
            bool[,] walls = new bool[size, size];
            return CreateGrid(size, size, walls);
        }

        // ─── Straight Path ────────────────────────────────────────────

        [Test]
        public void TryFindPath_StraightLine_ReturnsCorrectPath()
        {
            var grid = CreateOpenGrid();
            var pathfinder = new AStarGridPathfinder(grid);

            var start = new TileCoord(0, 0);
            var target = new TileCoord(0, 3);

            bool found = pathfinder.TryFindPath(start, target, maxSteps: 100, out var path);

            Assert.IsTrue(found, "Should find path in open grid");
            Assert.IsNotNull(path);
            Assert.AreEqual(3, path.Count, "Path from (0,0) to (0,3) should have 3 steps");
            Assert.AreEqual(new TileCoord(0, 1), path[0]);
            Assert.AreEqual(new TileCoord(0, 2), path[1]);
            Assert.AreEqual(new TileCoord(0, 3), path[2]);
        }

        [Test]
        public void TryFindPath_Horizontal_ReturnsCorrectPath()
        {
            var grid = CreateOpenGrid();
            var pathfinder = new AStarGridPathfinder(grid);

            var start = new TileCoord(0, 0);
            var target = new TileCoord(3, 0);

            bool found = pathfinder.TryFindPath(start, target, maxSteps: 100, out var path);

            Assert.IsTrue(found);
            Assert.AreEqual(3, path.Count);
            Assert.AreEqual(new TileCoord(1, 0), path[0]);
            Assert.AreEqual(new TileCoord(2, 0), path[1]);
            Assert.AreEqual(new TileCoord(3, 0), path[2]);
        }

        [Test]
        public void TryFindPath_Diagonal_ReturnsManhattanPath()
        {
            var grid = CreateOpenGrid();
            var pathfinder = new AStarGridPathfinder(grid);

            var start = new TileCoord(0, 0);
            var target = new TileCoord(2, 2);

            bool found = pathfinder.TryFindPath(start, target, maxSteps: 100, out var path);

            Assert.IsTrue(found);
            // Manhattan path: 4 steps (e.g., E-E-N-N or N-N-E-E depending on tie-breaking)
            Assert.AreEqual(4, path.Count, "Diagonal of 2x2 should be 4 steps in 4-directional grid");

            // Verify each step is adjacent to previous
            var prev = start;
            foreach (var step in path)
            {
                int dx = step.X - prev.X;
                int dz = step.Z - prev.Z;
                if (dx < 0) dx = -dx;
                if (dz < 0) dz = -dz;
                Assert.AreEqual(1, dx + dz, $"Step from {prev} to {step} should be adjacent");
                prev = step;
            }

            // Verify final position
            Assert.AreEqual(target, path[path.Count - 1]);
        }

        // ─── Already at Target ────────────────────────────────────────

        [Test]
        public void TryFindPath_AlreadyAtTarget_ReturnsEmptyPath()
        {
            var grid = CreateOpenGrid();
            var pathfinder = new AStarGridPathfinder(grid);

            var start = new TileCoord(2, 2);
            var target = new TileCoord(2, 2);

            bool found = pathfinder.TryFindPath(start, target, maxSteps: 100, out var path);

            Assert.IsTrue(found, "Already at target should return true");
            Assert.IsNotNull(path);
            Assert.AreEqual(0, path.Count, "Path should be empty when already at target");
        }

        // ─── Blocked Path ─────────────────────────────────────────────

        [Test]
        public void TryFindPath_BlockedPath_ReturnsFalse()
        {
            // Grid: 3x3 with a wall separating start from target
            int width = 3, depth = 3;
            bool[,] walls = new bool[width, depth];
            walls[1, 0] = true; // block the middle of the bottom row
            walls[1, 1] = true; // block the center
            walls[1, 2] = true; // block the middle of the top row
            // This creates two separate halves: left column (0,*) and right column (2,*)

            var grid = CreateGrid(width, depth, walls);
            var pathfinder = new AStarGridPathfinder(grid);

            var start = new TileCoord(0, 1);
            var target = new TileCoord(2, 1);

            bool found = pathfinder.TryFindPath(start, target, maxSteps: 100, out var path);

            Assert.IsFalse(found, "Should not find path through wall");
            Assert.IsNull(path);
        }

        [Test]
        public void TryFindPath_AroundObstacle_FindsDetour()
        {
            // 5x5 grid with a wall pillar in the center
            // Start at (1,0), target at (3,0), wall at (2,1) — must go around
            int size = 5;
            bool[,] walls = new bool[size, size];
            // Create a vertical wall: (2,1), (2,2), (2,3)
            walls[2, 1] = true;
            walls[2, 2] = true;
            walls[2, 3] = true;

            var grid = CreateGrid(size, size, walls);
            var pathfinder = new AStarGridPathfinder(grid);

            var start = new TileCoord(1, 0);
            var target = new TileCoord(3, 0);

            bool found = pathfinder.TryFindPath(start, target, maxSteps: 100, out var path);

            Assert.IsTrue(found, "Should find path around obstacle");
            Assert.IsNotNull(path);
            Assert.Greater(path.Count, 0);

            // Path should go around the wall pillar — verify no step is on a wall
            foreach (var step in path)
            {
                Assert.IsTrue(grid.IsWalkable(step.X, step.Z),
                    $"Step {step} should not be on a wall tile");
            }

            // Verify final position
            Assert.AreEqual(target, path[path.Count - 1]);
        }

        [Test]
        public void TryFindPath_StartBlocked_ReturnsFalse()
        {
            bool[,] walls = new bool[3, 3];
            walls[0, 0] = true; // start is blocked

            var grid = CreateGrid(3, 3, walls);
            var pathfinder = new AStarGridPathfinder(grid);

            var start = new TileCoord(0, 0); // blocked
            var target = new TileCoord(2, 2);

            bool found = pathfinder.TryFindPath(start, target, maxSteps: 100, out var path);

            Assert.IsFalse(found, "Should fail when start tile is blocked");
            Assert.IsNull(path);
        }

        [Test]
        public void TryFindPath_TargetBlocked_ReturnsFalse()
        {
            bool[,] walls = new bool[3, 3];
            walls[2, 2] = true; // target is blocked

            var grid = CreateGrid(3, 3, walls);
            var pathfinder = new AStarGridPathfinder(grid);

            var start = new TileCoord(0, 0);
            var target = new TileCoord(2, 2); // blocked

            bool found = pathfinder.TryFindPath(start, target, maxSteps: 100, out var path);

            Assert.IsFalse(found, "Should fail when target tile is blocked");
            Assert.IsNull(path);
        }

        // ─── Cache Invalidation ───────────────────────────────────────

        [Test]
        public void Cache_ReturnsSamePath_OnRepeatedQuery()
        {
            var grid = CreateOpenGrid();
            var pathfinder = new AStarGridPathfinder(grid);

            var start = new TileCoord(0, 0);
            var target = new TileCoord(0, 2);

            bool found1 = pathfinder.TryFindPath(start, target, 100, out var path1);
            bool found2 = pathfinder.TryFindPath(start, target, 100, out var path2);

            Assert.IsTrue(found1);
            Assert.IsTrue(found2);
            Assert.AreEqual(path1.Count, path2.Count, "Cached path should match");
            for (int i = 0; i < path1.Count; i++)
            {
                Assert.AreEqual(path1[i], path2[i]);
            }
        }

        [Test]
        public void InvalidateCache_TileOnPath_ForcesRecalculation()
        {
            var grid = CreateOpenGrid();
            var pathfinder = new AStarGridPathfinder(grid);

            var start = new TileCoord(0, 0);
            var target = new TileCoord(0, 3);

            // First query — caches path [(0,1), (0,2), (0,3)]
            pathfinder.TryFindPath(start, target, 100, out var path1);

            // Invalidate a tile on the cached path
            pathfinder.InvalidateCache(new TileCoord(0, 1));

            // Second query — should recalculate
            bool found2 = pathfinder.TryFindPath(start, target, 100, out var path2);

            Assert.IsTrue(found2);
            Assert.IsNotNull(path2);
            Assert.AreEqual(target, path2[path2.Count - 1]);
        }

        [Test]
        public void InvalidateCache_TargetTile_ForcesRecalculation()
        {
            var grid = CreateOpenGrid();
            var pathfinder = new AStarGridPathfinder(grid);

            var start = new TileCoord(0, 0);
            var target = new TileCoord(2, 2);

            pathfinder.TryFindPath(start, target, 100, out _);
            pathfinder.InvalidateCache(target);

            bool found = pathfinder.TryFindPath(start, target, 100, out var path);
            Assert.IsTrue(found);
        }

        // ─── Heuristic ────────────────────────────────────────────────

        [Test]
        public void GetHeuristicCost_ReturnsManhattanDistance()
        {
            var grid = CreateOpenGrid();
            var pathfinder = new AStarGridPathfinder(grid);

            Assert.AreEqual(0f, pathfinder.GetHeuristicCost(
                new TileCoord(0, 0), new TileCoord(0, 0)));

            Assert.AreEqual(4f, pathfinder.GetHeuristicCost(
                new TileCoord(0, 0), new TileCoord(2, 2)));

            Assert.AreEqual(7f, pathfinder.GetHeuristicCost(
                new TileCoord(1, 3), new TileCoord(5, 0)));
        }

        // ─── Out of Bounds ────────────────────────────────────────────

        [Test]
        public void TryFindPath_OutOfBounds_ReturnsFalse()
        {
            var grid = CreateOpenGrid(); // 5x5
            var pathfinder = new AStarGridPathfinder(grid);

            var start = new TileCoord(0, 0);
            var target = new TileCoord(10, 10); // out of bounds

            bool found = pathfinder.TryFindPath(start, target, 100, out var path);
            Assert.IsFalse(found);
            Assert.IsNull(path);
        }

        [Test]
        public void TryFindPath_NegativeCoord_ReturnsFalse()
        {
            var grid = CreateOpenGrid();
            var pathfinder = new AStarGridPathfinder(grid);

            var start = new TileCoord(-1, 0);
            var target = new TileCoord(2, 2);

            bool found = pathfinder.TryFindPath(start, target, 100, out var path);
            Assert.IsFalse(found);
            Assert.IsNull(path);
        }

        // ─── Max Steps ────────────────────────────────────────────────

        [Test]
        public void TryFindPath_MaxStepsExceeded_ReturnsFalse()
        {
            var grid = CreateOpenGrid();
            var pathfinder = new AStarGridPathfinder(grid);

            var start = new TileCoord(0, 0);
            var target = new TileCoord(4, 4); // distance 8, but maxSteps=3

            bool found = pathfinder.TryFindPath(start, target, maxSteps: 3, out var path);

            // Depending on tie-breaking, this might or might not find within 3 steps
            // With 3 maxSteps exploring, it won't reach distance-8 target
            // This test verifies the maxSteps guard works
            Assert.IsFalse(found || (path != null && path.Count > 3),
                "Should not find path exceeding maxSteps");
        }

        // ─── Grid with Perimeter Walls (from TestGridBuilder pattern) ──

        [Test]
        public void TryFindPath_PerimeterWalledGrid_FindsPathInInterior()
        {
            var grid = new TestGridBuilder.GridQueryAdapter(TestGridBuilder.CreateFiveByFiveGrid());
            var pathfinder = new AStarGridPathfinder(grid);

            // Interior tiles: (1,1) to (3,3) are walkable
            var start = new TileCoord(1, 1);
            var target = new TileCoord(3, 3);

            bool found = pathfinder.TryFindPath(start, target, maxSteps: 100, out var path);

            Assert.IsTrue(found);
            Assert.IsNotNull(path);
            Assert.Greater(path.Count, 0);
            Assert.AreEqual(target, path[path.Count - 1]);
        }
    }
}
