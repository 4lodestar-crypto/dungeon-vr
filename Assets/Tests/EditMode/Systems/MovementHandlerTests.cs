using DungeonVR.Gameplay;
using DungeonVR.Gameplay.Logic;
using DungeonVR.Shared;
using DungeonVR.Shared.Requests;
using DungeonVR.Shared.Results;
using NUnit.Framework;
using UnityEngine;

namespace DungeonVR.Tests.EditMode
{
    /// <summary>
    /// EditMode unit tests for MovementHandler.
    /// Tests: forward move, wall block, rotate left/right, queue ordering.
    /// </summary>
    public class MovementHandlerTests
    {
        /// <summary>5x5 empty grid (no perimeter walls) — all tiles walkable.</summary>
        private static bool[,] CreateEmpty5x5()
        {
            return new bool[5, 5];
        }

        /// <summary>5x5 grid with walls on edges (index 0 and 4).</summary>
        private static bool[,] CreatePerimeterWalled5x5()
        {
            var grid = new bool[5, 5];
            for (int x = 0; x < 5; x++)
                for (int y = 0; y < 5; y++)
                    grid[x, y] = x == 0 || x == 4 || y == 0 || y == 4;
            return grid;
        }

        [Test]
        public void MovementHandler_ForwardFromOrigin_AdvancesOneTile()
        {
            // Arrange
            var champion = new ChampionState(new Vector2Int(2, 2), FacingDirection.North);
            var grid = CreateEmpty5x5();
            var request = new MovementRequest(new Vector2Int(0, 1), tickNumber: 1);

            // Act
            MovementResult result = MovementHandler.Handle(request, champion, grid);

            // Assert
            Assert.IsTrue(result.Success, "Move forward from open tile should succeed");
            Assert.AreEqual(new Vector2Int(2, 3), result.NewState.GridPosition);
            Assert.AreEqual(new Vector2Int(2, 2), champion.GridPosition,
                "Original champion state must NOT be mutated (immutability pattern)");
        }

        [Test]
        public void MovementHandler_MoveIntoWall_StaysInPlace()
        {
            // Arrange
            var champion = new ChampionState(new Vector2Int(2, 4), FacingDirection.North);
            var grid = CreatePerimeterWalled5x5();
            var request = new MovementRequest(new Vector2Int(0, 1), tickNumber: 1);

            // Act
            MovementResult result = MovementHandler.Handle(request, champion, grid);

            // Assert
            Assert.IsFalse(result.Success, "Move into wall should be blocked");
            Assert.AreEqual(new Vector2Int(2, 4), champion.GridPosition, "Champion state should remain unchanged");
            Assert.AreEqual(new Vector2Int(2, 4), result.NewState.GridPosition, "Result state should match original");
            Assert.IsNotEmpty(result.BlockReason, "Block reason should be provided");
        }

        [Test]
        public void MovementHandler_RotateLeft_ChangesFacing90()
        {
            // Arrange
            var champion = new ChampionState(new Vector2Int(2, 2), FacingDirection.North);
            var grid = CreateEmpty5x5();
            var request = new MovementRequest(Vector2Int.zero, tickNumber: 1, FacingDirection.West);

            // Act
            MovementResult result = MovementHandler.Handle(request, champion, grid);

            // Assert
            Assert.IsTrue(result.Success, "Rotate should succeed");
            Assert.AreEqual(FacingDirection.West, result.NewState.FacingDirection);
            Assert.AreEqual(FacingDirection.North, champion.FacingDirection,
                "Original champion facing must NOT be mutated (immutability pattern)");
            Assert.AreEqual(new Vector2Int(2, 2), result.NewState.GridPosition, "Position unchanged on rotate");
            Assert.AreEqual(new Vector2Int(2, 2), champion.GridPosition, "Original position unchanged");
        }

        [Test]
        public void MovementHandler_RotateRight_ChangesFacing90()
        {
            // Arrange
            var champion = new ChampionState(new Vector2Int(2, 2), FacingDirection.North);
            var grid = CreateEmpty5x5();
            var request = new MovementRequest(Vector2Int.zero, tickNumber: 1, FacingDirection.East);

            // Act
            MovementResult result = MovementHandler.Handle(request, champion, grid);

            // Assert
            Assert.IsTrue(result.Success, "Rotate should succeed");
            Assert.AreEqual(FacingDirection.East, result.NewState.FacingDirection);
            Assert.AreEqual(FacingDirection.North, champion.FacingDirection,
                "Original champion facing must NOT be mutated (immutability pattern)");
            Assert.AreEqual(new Vector2Int(2, 2), result.NewState.GridPosition, "Position unchanged on rotate");
            Assert.AreEqual(new Vector2Int(2, 2), champion.GridPosition, "Original position unchanged");
        }

        [Test]
        public void MovementHandler_MultipleMoves_QueueProcessesInOrder()
        {
            // Arrange
            var champion = new ChampionState(new Vector2Int(2, 2), FacingDirection.North);
            var grid = CreateEmpty5x5();

            // Act — process two forward moves, adopting returned state each time
            var req1 = new MovementRequest(new Vector2Int(0, 1), tickNumber: 1);
            MovementResult result1 = MovementHandler.Handle(req1, champion, grid);
            champion = result1.NewState; // adopt cloned state

            var req2 = new MovementRequest(new Vector2Int(0, 1), tickNumber: 2);
            MovementResult result2 = MovementHandler.Handle(req2, champion, grid);

            // Assert
            Assert.IsTrue(result2.Success);
            Assert.AreEqual(new Vector2Int(2, 4), champion.GridPosition,
                "After adopting result1, champion should be at (2,3); after result2 at (2,4)");
        }

        [Test]
        public void MovementHandler_MoveOffGridEdge_StaysInPlace()
        {
            // Arrange
            var champion = new ChampionState(new Vector2Int(0, 2), FacingDirection.West);
            var grid = CreateEmpty5x5();

            // Act
            var request = new MovementRequest(new Vector2Int(-1, 0), tickNumber: 1);
            MovementResult result = MovementHandler.Handle(request, champion, grid);

            // Assert
            Assert.IsFalse(result.Success, "Move off-grid edge should be blocked");
            Assert.AreEqual(new Vector2Int(0, 2), champion.GridPosition, "Position unchanged");
            Assert.AreEqual(new Vector2Int(0, 2), result.NewState.GridPosition,
                "Result state should match original on failure");
            Assert.IsNotEmpty(result.BlockReason, "Block reason should mention bounds");
        }
    }
}
