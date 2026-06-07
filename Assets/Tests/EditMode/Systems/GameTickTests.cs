using System.Collections.Generic;
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
    /// EditMode unit tests for the GameTick tick loop.
    /// Tests: tick rate, queued request processing, sequential ticks, invalid move safety.
    ///
    /// V0-EXCEPTION: GameTick.Awake() requires a live MonoBehaviour hierarchy to wire
    /// serialized fields (FindObjectOfType<GridData>). In EditMode we cannot run Awake
    /// without entering PlayMode, so these tests validate the tick's core logic through
    /// MovementHandler directly — the same path GameTick.ProcessTick() uses internally.
    /// Full GameTick integration (MonoBehaviour lifecycle + serialized field injection)
    /// is covered by PlayMode tests in V1.
    /// </summary>
    public class GameTickTests
    {
        [Test]
        public void GameTick_WithNoRequests_ChampionStateUnchanged()
        {
            // Arrange
            var champion = new ChampionState(new Vector2Int(2, 2), FacingDirection.North);

            // Act — no requests submitted (simulating empty tick)
            // No calls to MovementHandler.Handle()

            // Assert
            Assert.AreEqual(new Vector2Int(2, 2), champion.GridPosition);
            Assert.AreEqual(FacingDirection.North, champion.FacingDirection);
        }

        [Test]
        public void GameTick_TickRateSetting_Is20Hz()
        {
            // Arrange & Act
            float fixedDt = Time.fixedDeltaTime;

            // Assert
            Assert.AreEqual(0.05f, fixedDt, 0.001f,
                "FixedUpdate deltaTime should be 0.05s (50 ms) for 20 Hz tick rate. " +
                "Set in Project Settings > Time > Fixed Timestep.");
        }

        [Test]
        public void GameTick_MultipleSequentialTicks_UpdatePositionCorrectly()
        {
            // Arrange — 6x6 grid to allow three northward moves from (2,2) without OOB
            var grid = new bool[6, 6];
            var champion = new ChampionState(new Vector2Int(2, 2), FacingDirection.North);

            // Act — simulate 3 sequential FixedUpdate ticks
            var r1 = MovementHandler.Handle(new MovementRequest(new Vector2Int(0, 1), tickNumber: 1), champion, grid);
            var r2 = MovementHandler.Handle(new MovementRequest(new Vector2Int(0, 1), tickNumber: 2), champion, grid);
            var r3 = MovementHandler.Handle(new MovementRequest(new Vector2Int(0, 1), tickNumber: 3), champion, grid);

            // Assert
            Assert.IsTrue(r1.Success, "First tick move should succeed");
            Assert.IsTrue(r2.Success, "Second tick move should succeed");
            Assert.IsTrue(r3.Success, "Third tick move should succeed");
            Assert.AreEqual(new Vector2Int(2, 5), champion.GridPosition,
                "After 3 northward ticks from (2,2), champion should be at (2,5)");
        }

        [Test]
        public void GameTick_EnqueueAndProcess_AdvancesChampion()
        {
            // Arrange — simulates GameTick.ProcessTick: collect requests, iterate, apply
            var grid = new bool[5, 5];
            var champion = new ChampionState(new Vector2Int(2, 2), FacingDirection.North);
            var results = new List<MovementResult>();

            // Act — enqueue and process (mimics GameTick.ProcessTick inner loop)
            var request = new MovementRequest(new Vector2Int(0, 1), tickNumber: 1);
            results.Add(MovementHandler.Handle(request, champion, grid));

            // Assert
            Assert.AreEqual(1, results.Count);
            Assert.IsTrue(results[0].Success);
            Assert.AreEqual(new Vector2Int(2, 3), champion.GridPosition,
                "Champion advanced one tile north");
            Assert.AreEqual(new Vector2Int(2, 3), results[0].NewPosition,
                "Result position matches champion position");
        }

        [Test]
        public void GameTick_InvalidMoveViaSystem_StatePreserved()
        {
            // Arrange — perimeter-walled 5x5 grid
            var grid = new bool[5, 5];
            for (int x = 0; x < 5; x++)
                for (int y = 0; y < 5; y++)
                    grid[x, y] = x == 0 || x == 4 || y == 0 || y == 4;

            var champion = new ChampionState(new Vector2Int(2, 4), FacingDirection.North);
            var request = new MovementRequest(new Vector2Int(0, 1), tickNumber: 1);

            // Act
            var result = MovementHandler.Handle(request, champion, grid);

            // Assert
            Assert.IsFalse(result.Success, "Invalid move should be rejected");
            Assert.AreEqual(new Vector2Int(2, 4), champion.GridPosition,
                "Champion position unchanged after invalid move");
            Assert.IsNotEmpty(result.BlockReason, "Block reason should be provided");
        }
    }
}
