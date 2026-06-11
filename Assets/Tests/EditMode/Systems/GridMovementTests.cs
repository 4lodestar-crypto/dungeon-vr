using DungeonVR.Gameplay.Logic;
using DungeonVR.Shared;
using DungeonVR.Shared.Enums;
using DungeonVR.Shared.Requests;
using DungeonVR.Tests.EditMode.Fixtures;
using DungeonVR.Shared.Results;
using NUnit.Framework;
using UnityEngine;

namespace DungeonVR.Tests.EditMode.Systems
{
    /// <summary>
    /// EditMode unit tests for MovementHandler.
    /// AAA pattern: Arrange-Act-Assert. Deterministic — no Random, timing, or frame-rate.
    /// </summary>
    [TestFixture]
    public class GridMovementTests
    {
        /// <summary>
        /// Starting at (2,2) facing North (0), moving Forward should advance to (2,3).
        /// </summary>
        [Test]
        public void Movement_Forward_AdvancesGridZByOne()
        {
            // Arrange
            var state = TestGridBuilder.CreateDefaultGameState();
            state.Champion.FacingIndex = 0; // North
            state.Champion.GridX = 2;
            state.Champion.GridZ = 2;
            var handler = new MovementHandler(new TestGridBuilder.GridQueryAdapter(state.Grid));
            var request = new MovementRequest(MovementDirection.Forward, tickNumber: 1);

            // Act
            MovementResult result = handler.Handle(request, state);

            // Assert
            Assert.IsTrue(result.Success, "Forward movement should succeed on open tile");
            Assert.AreEqual(2, state.Champion.GridX, "GridX should remain unchanged");
            Assert.AreEqual(3, state.Champion.GridZ, "GridZ should advance by 1 when facing North");
            Assert.AreEqual(0, state.Champion.FacingIndex, "FacingIndex should remain North");
        }

        /// <summary>
        /// Starting at the north edge facing North — forward move should be blocked.
        /// 5x5 grid: perimeter walls at z=4, so moving from (2,3)->(2,4) hits wall.
        /// </summary>
        [Test]
        public void Movement_WallBlocksForward_ReturnsBlocked()
        {
            // Arrange
            var state = TestGridBuilder.CreateDefaultGameState();
            state.Champion.FacingIndex = 0; // North
            state.Champion.GridX = 2;
            state.Champion.GridZ = 3; // One tile before north wall
            var handler = new MovementHandler(new TestGridBuilder.GridQueryAdapter(state.Grid));
            var request = new MovementRequest(MovementDirection.Forward, tickNumber: 1);

            // Act
            MovementResult result = handler.Handle(request, state);

            // Assert
            Assert.IsFalse(result.Success, "Movement into a wall should be blocked");
            Assert.IsNotNull(result.BlockReason, "Blocked result should provide a reason");
            Assert.AreEqual(2, state.Champion.GridX, "Position should remain unchanged after blocked move");
            Assert.AreEqual(3, state.Champion.GridZ, "Position should remain unchanged after blocked move");
        }

        /// <summary>
        /// Starting at (2,2) facing North (0), RotateLeft should set FacingIndex to 3 (West).
        /// </summary>
        [Test]
        public void Movement_RotateLeft_ChangesFacing()
        {
            // Arrange
            var state = TestGridBuilder.CreateDefaultGameState();
            state.Champion.FacingIndex = 0; // North
            state.Champion.GridX = 2;
            state.Champion.GridZ = 2;
            var handler = new MovementHandler(new TestGridBuilder.GridQueryAdapter(state.Grid));
            var request = new MovementRequest(MovementDirection.RotateLeft, tickNumber: 1);

            // Act
            MovementResult result = handler.Handle(request, state);

            // Assert
            Assert.IsTrue(result.Success, "Rotation should always succeed");
            Assert.AreEqual(3, state.Champion.FacingIndex, "RotateLeft from North should face West");
            Assert.AreEqual(2, state.Champion.GridX, "GridX should not change on rotation");
            Assert.AreEqual(2, state.Champion.GridZ, "GridZ should not change on rotation");
        }

        /// <summary>
        /// Starting at (2,2) facing North (0), RotateRight should set FacingIndex to 1 (East).
        /// </summary>
        [Test]
        public void Movement_RotateRight_ChangesFacing()
        {
            // Arrange
            var state = TestGridBuilder.CreateDefaultGameState();
            state.Champion.FacingIndex = 0; // North
            state.Champion.GridX = 2;
            state.Champion.GridZ = 2;
            var handler = new MovementHandler(new TestGridBuilder.GridQueryAdapter(state.Grid));
            var request = new MovementRequest(MovementDirection.RotateRight, tickNumber: 1);

            // Act
            MovementResult result = handler.Handle(request, state);

            // Assert
            Assert.IsTrue(result.Success, "Rotation should always succeed");
            Assert.AreEqual(1, state.Champion.FacingIndex, "RotateRight from North should face East");
            Assert.AreEqual(2, state.Champion.GridX, "GridX should not change on rotation");
            Assert.AreEqual(2, state.Champion.GridZ, "GridZ should not change on rotation");
        }

        /// <summary>
        /// Starting at (2,2) facing South (2), Backward moves the champion to (2,1).
        /// Backward while facing South means moving North (opposite direction).
        /// </summary>
        [Test]
        public void Movement_Backward_WorksFromOpenTile()
        {
            // Arrange
            var state = TestGridBuilder.CreateDefaultGameState();
            state.Champion.FacingIndex = 2; // South
            state.Champion.GridX = 2;
            state.Champion.GridZ = 2;
            var handler = new MovementHandler(new TestGridBuilder.GridQueryAdapter(state.Grid));
            var request = new MovementRequest(MovementDirection.Backward, tickNumber: 1);

            // Act
            MovementResult result = handler.Handle(request, state);

            // Assert
            Assert.IsTrue(result.Success, "Backward movement on open tile should succeed");
            Assert.AreEqual(2, state.Champion.GridX, "GridX should remain unchanged");
            Assert.AreEqual(1, state.Champion.GridZ, "Backward while facing South should move North (z=1)");
            Assert.AreEqual(2, state.Champion.FacingIndex, "FacingIndex should remain South");
        }

        /// <summary>
        /// Moving forward off the grid (out of bounds) should be blocked.
        /// </summary>
        [Test]
        public void Movement_OutOfBounds_ReturnsBlocked()
        {
            // Arrange: use a 3x3 open grid, champion at edge
            var grid = TestGridBuilder.CreateThreeByThreeGrid();
            var state = new GameState
            {
                CurrentTick = 0,
                Champion = new ChampionState(gridX: 2, gridZ: 2, facingIndex: 1), // East, at far-right column
                Grid = grid
            };
            var handler = new MovementHandler(new TestGridBuilder.GridQueryAdapter(grid));
            var request = new MovementRequest(MovementDirection.Forward, tickNumber: 1);

            // Act: moving East from x=2 on a 3-wide grid (valid indices: 0,1,2) goes to x=3 = out of bounds
            MovementResult result = handler.Handle(request, state);

            // Assert
            Assert.IsFalse(result.Success, "Movement out of bounds should be blocked");
            Assert.AreEqual(2, state.Champion.GridX, "GridX should remain unchanged");
            Assert.AreEqual(2, state.Champion.GridZ, "GridZ should remain unchanged");
        }
    }
}
