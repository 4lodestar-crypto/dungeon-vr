using DungeonVR.Gameplay.Logic;
using DungeonVR.Server;
using DungeonVR.Shared;
using DungeonVR.Shared.Enums;
using DungeonVR.Shared.Requests;
using DungeonVR.Tests.EditMode.Fixtures;
using NUnit.Framework;
using UnityEngine;

namespace DungeonVR.Tests.EditMode.Integration
{
    /// <summary>
    /// End-to-end integration smoke tests for the movement pipeline.
    /// Tests the full flow: request → queue → process → state change, across multiple ticks.
    /// </summary>
    [TestFixture]
    public class MovementIntegrationTests
    {
        /// <summary>
        /// Full pipeline smoke test:
        /// 1. Create default GameState with 5x5 perimeter-walled grid
        /// 2. Create MovementHandler
        /// 3. Create GameServer, wire handler
        /// 4. Queue Forward movement, ProcessTick — champion moves
        /// 5. Queue RotateLeft, ProcessTick — champion turns
        /// </summary>
        [Test]
        public void FullPipeline_ForwardThenRotateLeft_StateMatchesExpected()
        {
            // Arrange — build the full pipeline
            var state = TestGridBuilder.CreateDefaultGameState();
            // Champion at (2,2) facing South(2) by default
            // 5x5 grid: interior (1..3, 1..3) is open, perimeter blocked

            var handler = new MovementHandler(new TestGridBuilder.GridQueryAdapter(state.Grid));
            var server = new GameServer();
            server.State = state;
            server.SetMovementHandler(handler);

            // Verify initial state
            Assert.AreEqual(2, server.State.Champion.GridX, "Initial GridX should be 2");
            Assert.AreEqual(2, server.State.Champion.GridZ, "Initial GridZ should be 2");
            Assert.AreEqual(2, server.State.Champion.FacingIndex, "Initial facing should be South (2)");
            Assert.AreEqual(0, server.State.CurrentTick, "Initial tick should be 0");

            // --- Phase 1: Forward ---
            // Facing South, Forward moves to (2,1)
            server.QueueRequest(new MovementRequest(MovementDirection.Forward, tickNumber: 1));
            server.ProcessTick();

            // Assert Phase 1
            Assert.AreEqual(1, server.State.CurrentTick, "Tick should be 1 after first ProcessTick");
            Assert.AreEqual(2, server.State.Champion.GridX, "GridX unchanged after Forward");
            Assert.AreEqual(1, server.State.Champion.GridZ, "Forward while facing South should move to z=1");
            Assert.AreEqual(2, server.State.Champion.FacingIndex, "Facing should remain South");
            Assert.IsFalse(
                state.Grid.IsWalkable(
                    server.State.Champion.GridX,
                    server.State.Champion.GridZ + 1),
                "Tile ahead of champion should be walkable (interior)");

            // --- Phase 2: RotateLeft ---
            // Facing South(2), RotateLeft → East(1)
            server.QueueRequest(new MovementRequest(MovementDirection.RotateLeft, tickNumber: 2));
            server.ProcessTick();

            // Assert Phase 2
            Assert.AreEqual(2, server.State.CurrentTick, "Tick should be 2 after second ProcessTick");
            Assert.AreEqual(2, server.State.Champion.GridX, "GridX unchanged after rotation");
            Assert.AreEqual(1, server.State.Champion.GridZ, "GridZ unchanged after rotation");
            Assert.AreEqual(1, server.State.Champion.FacingIndex, "RotateLeft from South should face East (1)");

            // --- Phase 3: Validation of total state ---
            // Get snapshot and verify it matches
            var snapshot = server.GetStateSnapshot();
            Assert.IsNotNull(snapshot, "State snapshot should not be null");
            Assert.AreEqual(2, snapshot.CurrentTick, "Snapshot tick should be 2");
            Assert.AreEqual(1, snapshot.Champion.FacingIndex, "Snapshot facing should be East (1)");
            Assert.AreEqual(2, snapshot.Champion.GridX, "Snapshot GridX should be 2");
            Assert.AreEqual(1, snapshot.Champion.GridZ, "Snapshot GridZ should be 1");
        }

        /// <summary>
        /// Multiple ticks with no requests — verifies tick accumulator behavior.
        /// </summary>
        [Test]
        public void FullPipeline_EmptyTicks_TickAccumulates()
        {
            // Arrange
            var state = TestGridBuilder.CreateDefaultGameState();
            var handler = new MovementHandler(new TestGridBuilder.GridQueryAdapter(state.Grid));
            var server = new GameServer();
            server.State = state;
            server.SetMovementHandler(handler);

            // Act — process several empty ticks
            int tickCount = 5;
            for (int i = 0; i < tickCount; i++)
            {
                server.ProcessTick();
            }

            // Assert — all ticks processed
            Assert.AreEqual(tickCount, server.State.CurrentTick,
                $"Tick should be {tickCount} after {tickCount} empty ProcessTick calls");
            Assert.AreEqual(2, server.State.Champion.GridX, "Champion should not move on empty ticks");
            Assert.AreEqual(2, server.State.Champion.GridZ, "Champion should not move on empty ticks");
        }

        /// <summary>
        /// Backward from center, then forward back to center.
        /// Verifies round-trip movement accuracy.
        /// </summary>
        [Test]
        public void FullPipeline_BackwardThenForward_ReturnsToOrigin()
        {
            // Arrange
            var state = TestGridBuilder.CreateDefaultGameState();
            state.Champion.FacingIndex = 2; // South
            state.Champion.GridX = 2;
            state.Champion.GridZ = 2;
            var handler = new MovementHandler(new TestGridBuilder.GridQueryAdapter(state.Grid));
            var server = new GameServer();
            server.State = state;
            server.SetMovementHandler(handler);

            // Act — Backward (while facing South = move North) to (2,3)
            server.QueueRequest(new MovementRequest(MovementDirection.Backward, tickNumber: 1));
            server.ProcessTick();
            Assert.AreEqual(3, server.State.Champion.GridZ, "Backward while facing South should move to z=3");

            // Act — Forward (while facing South = move South) back to (2,2)
            server.QueueRequest(new MovementRequest(MovementDirection.Forward, tickNumber: 2));
            server.ProcessTick();

            // Assert — back to origin with facing unchanged
            Assert.AreEqual(2, server.State.Champion.GridX, "GridX should return to 2");
            Assert.AreEqual(2, server.State.Champion.GridZ, "GridZ should return to 2");
            Assert.AreEqual(2, server.State.Champion.FacingIndex, "Facing should remain South (2) throughout");
        }
    }
}
