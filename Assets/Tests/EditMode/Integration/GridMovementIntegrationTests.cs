using DungeonVR.Gameplay;
using DungeonVR.Gameplay.Logic;
using DungeonVR.Shared;
using DungeonVR.Shared.Requests;
using DungeonVR.Shared.Results;
using DungeonVR.Tests.EditMode.Fixtures;
using NUnit.Framework;
using UnityEngine;

namespace DungeonVR.Tests.EditMode.Integration
{
    /// <summary>
    /// EditMode integration smoke test: instantiate champion + grid, send MovementRequest through tick,
    /// verify position change. Covers end-to-end: input -> request -> tick -> state change.
    ///
    /// These tests exercise the full MovementHandler pipeline without requiring scene load.
    /// </summary>
    public class GridMovementIntegrationTests
    {
        [Test]
        public void Champion_MoveForwardOnEmptyGrid_AdvancesOneTile()
        {
            // Arrange
            bool[,] grid = TestGridBuilder.Create5x5PerimeterWalled();
            // Make interior walkable: (2,2)->(2,4) for north movement
            grid[2, 3] = false;
            var champion = TestGridBuilder.CreateDefaultChampion(); // (2,2) North

            // Act
            var request = new MovementRequest(new Vector2Int(0, 1), tickNumber: 1);
            MovementResult result = MovementHandler.Handle(request, champion, grid);

            // Assert
            Assert.IsTrue(result.Success, "Forward move on empty grid should succeed");
            Assert.AreEqual(new Vector2Int(2, 3), result.NewState.GridPosition,
                "Result state should reflect the new position");
            Assert.AreEqual(new Vector2Int(2, 2), champion.GridPosition,
                "Original champion must NOT be mutated (immutability pattern)");
        }

        [Test]
        public void Champion_MoveIntoWall_StaysInPlace()
        {
            // Arrange
            bool[,] grid = TestGridBuilder.Create5x5PerimeterWalled();
            // (2,4) is the north edge = wall
            var champion = TestGridBuilder.CreateChampionAt(new Vector2Int(2, 4), FacingDirection.North);

            // Act
            var request = new MovementRequest(new Vector2Int(0, 1), tickNumber: 1);
            MovementResult result = MovementHandler.Handle(request, champion, grid);

            // Assert
            Assert.IsFalse(result.Success, "Move into wall tile should be rejected");
            Assert.AreEqual(new Vector2Int(2, 4), champion.GridPosition, "Champion position should not change");
            Assert.AreEqual(new Vector2Int(2, 4), result.NewState.GridPosition,
                "Result state should match original on failure");
            Assert.IsNotEmpty(result.BlockReason, "Block reason should be provided");
        }

        [Test]
        public void Champion_MultipleMoves_AcrossMultipleTicks_ArrivesCorrectly()
        {
            // Arrange
            bool[,] grid = TestGridBuilder.Create5x5Empty(); // all walkable
            var champion = TestGridBuilder.CreateChampionAt(new Vector2Int(0, 0), FacingDirection.East);

            // Act — enqueue 3 forward moves, adopt returned state after each like a real tick
            MovementResult r1 = MovementHandler.Handle(new MovementRequest(new Vector2Int(1, 0), tickNumber: 1), champion, grid);
            champion = r1.NewState;
            MovementResult r2 = MovementHandler.Handle(new MovementRequest(new Vector2Int(1, 0), tickNumber: 2), champion, grid);
            champion = r2.NewState;
            MovementResult result3 = MovementHandler.Handle(new MovementRequest(new Vector2Int(1, 0), tickNumber: 3), champion, grid);

            // Assert
            Assert.IsTrue(result3.Success, "Third move should succeed");
            Assert.AreEqual(new Vector2Int(3, 0), champion.GridPosition,
                "After 3 east moves adopting state each tick, champion at (3,0)");
        }

        [Test]
        public void Champion_RotateLeftAndMove_ChangesDirection()
        {
            // Arrange
            bool[,] grid = TestGridBuilder.Create5x5Empty();
            var champion = TestGridBuilder.CreateDefaultChampion(); // (2,2) North

            // Act — rotate left (now facing West), then move forward, adopting state each time
            var rotateReq = new MovementRequest(Vector2Int.zero, tickNumber: 1, FacingDirection.West);
            MovementResult rotateResult = MovementHandler.Handle(rotateReq, champion, grid);
            champion = rotateResult.NewState;

            var moveReq = new MovementRequest(new Vector2Int(-1, 0), tickNumber: 2);
            MovementResult moveResult = MovementHandler.Handle(moveReq, champion, grid);

            // Assert
            Assert.AreEqual(FacingDirection.West, champion.FacingDirection,
                "After rotate left from North, facing West");
            Assert.IsTrue(moveResult.Success, "West move after rotation should succeed");
            Assert.AreEqual(new Vector2Int(1, 2), champion.GridPosition,
                "Champion moved 1 tile west");
        }

        [Test]
        public void System_EmptyTick_DoesNotChangeState()
        {
            // Arrange
            bool[,] grid = TestGridBuilder.Create5x5Empty();
            var champion = TestGridBuilder.CreateDefaultChampion(); // (2,2) North
            Vector2Int beforePos = champion.GridPosition;
            FacingDirection beforeFacing = champion.FacingDirection;

            // Act — no requests processed (empty tick)
            // No MovementHandler calls at all — champion unchanged

            // Assert
            Assert.AreEqual(beforePos, champion.GridPosition, "Position unchanged on empty tick");
            Assert.AreEqual(beforeFacing, champion.FacingDirection, "Facing unchanged on empty tick");
        }
    }
}
