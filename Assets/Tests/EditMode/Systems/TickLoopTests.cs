using DungeonVR.Gameplay.Logic;
using DungeonVR.Server;
using DungeonVR.Shared;
using DungeonVR.Shared.Enums;
using DungeonVR.Shared.Requests;
using DungeonVR.Tests.EditMode.Fixtures;
using NUnit.Framework;

namespace DungeonVR.Tests.EditMode.Systems
{
    /// <summary>
    /// EditMode unit tests for GameServer tick loop.
    /// Tests request queuing, tick processing, and state management.
    /// </summary>
    [TestFixture]
    public class TickLoopTests
    {
        /// <summary>
        /// Calling ProcessTick() with no queued requests should still increment the tick.
        /// </summary>
        [Test]
        public void ProcessTick_NoRequests_TickIncrements()
        {
            // Arrange
            var server = new GameServer();
            server.State.CurrentTick = 0;

            // Act
            server.ProcessTick();

            // Assert
            Assert.AreEqual(1, server.State.CurrentTick, "Tick should increment even with no requests");
        }

        /// <summary>
        /// Queue a valid Forward movement request, call ProcessTick, and verify the champion moves.
        /// </summary>
        [Test]
        public void ProcessTick_ValidMovement_StateChanges()
        {
            // Arrange
            var state = TestGridBuilder.CreateDefaultGameState();
            state.Champion.FacingIndex = 0; // North
            state.Champion.GridX = 2;
            state.Champion.GridZ = 2;

            var server = new GameServer();
            server.State = state;
            server.SetMovementHandler(new MovementHandler(new TestGridBuilder.GridQueryAdapter(state.Grid)));
            server.QueueRequest(new MovementRequest(MovementDirection.Forward, tickNumber: 1));

            // Act
            server.ProcessTick();

            // Assert
            Assert.AreEqual(1, server.State.CurrentTick, "Tick should be 1 after one ProcessTick");
            Assert.AreEqual(2, server.State.Champion.GridX, "GridX unchanged");
            Assert.AreEqual(3, server.State.Champion.GridZ, "Champion should have advanced 1 tile north");
        }

        /// <summary>
        /// Queue two valid moves and call ProcessTick twice — both should be processed in order.
        /// First: Forward (2,2→2,3). Second: Forward (2,3→2,4 hits wall, blocked).
        /// </summary>
        [Test]
        public void ProcessTick_TwoValidMoves_ProcessedInOrder()
        {
            // Arrange
            var state = TestGridBuilder.CreateDefaultGameState();
            state.Champion.FacingIndex = 0; // North
            state.Champion.GridX = 2;
            state.Champion.GridZ = 1; // Start at (2,1) so second move is to (2,2) then (2,3)

            var server = new GameServer();
            server.State = state;
            server.SetMovementHandler(new MovementHandler(new TestGridBuilder.GridQueryAdapter(state.Grid)));
            server.QueueRequest(new MovementRequest(MovementDirection.Forward, tickNumber: 1));

            // Act - first tick
            server.ProcessTick();

            // Assert - first move processed
            Assert.AreEqual(1, server.State.CurrentTick, "Tick should be 1");
            Assert.AreEqual(2, server.State.Champion.GridZ, "First move: should be at z=2");

            // Arrange - second request
            server.QueueRequest(new MovementRequest(MovementDirection.Forward, tickNumber: 2));

            // Act - second tick
            server.ProcessTick();

            // Assert - second move processed
            Assert.AreEqual(2, server.State.CurrentTick, "Tick should be 2");
            Assert.AreEqual(3, server.State.Champion.GridZ, "Second move: should be at z=3");
            Assert.AreEqual(2, server.State.Champion.GridX, "GridX unchanged");
        }

        /// <summary>
        /// Queue an invalid move (into a wall). State should remain unchanged,
        /// but tick should still increment.
        /// </summary>
        [Test]
        public void ProcessTick_InvalidMoveIntoWall_StateUnchangedTickIncrements()
        {
            // Arrange
            var state = TestGridBuilder.CreateDefaultGameState();
            state.Champion.FacingIndex = 0; // North
            state.Champion.GridX = 2;
            state.Champion.GridZ = 3; // One tile before north wall — moving forward hits wall

            var server = new GameServer();
            server.State = state;
            server.SetMovementHandler(new MovementHandler(new TestGridBuilder.GridQueryAdapter(state.Grid)));
            server.QueueRequest(new MovementRequest(MovementDirection.Forward, tickNumber: 1));

            // Act
            server.ProcessTick();

            // Assert
            Assert.AreEqual(1, server.State.CurrentTick, "Tick should still increment");
            Assert.AreEqual(2, server.State.Champion.GridX, "GridX should be unchanged");
            Assert.AreEqual(3, server.State.Champion.GridZ, "GridZ should be unchanged (blocked by wall)");
        }

        /// <summary>
        /// Verify that GameConstants.TICK_RATE is exactly 20 (constant correctness check).
        /// </summary>
        [Test]
        public void TickRate_Constant_IsTwenty()
        {
            // Arrange & Act & Assert
            Assert.AreEqual(20, GameConstants.TICK_RATE,
                "TICK_RATE must be 20 Hz as specified in the design contract");

            // Derived values should also be consistent
            Assert.AreEqual(1f / 20f, GameConstants.TICK_DELTA, 1e-6f,
                "TICK_DELTA should be 1/TICK_RATE");
        }
    }
}
