using DungeonVR.AI.Data;
using DungeonVR.AI.Interfaces;
using DungeonVR.AI.Logic;
using DungeonVR.Shared;
using DungeonVR.Shared.Data;
using NUnit.Framework;
using System.Collections.Generic;

namespace DungeonVR.Tests.EditMode.AI
{
    /// <summary>
    /// EditMode unit tests for ScreamerBehavior.
    /// Verifies state transitions, cue emission, movement, and attack logic.
    /// All tests are deterministic — no UnityEngine.Random, no Time.time.
    /// </summary>
    [TestFixture]
    public class ScreamerBehaviorTests
    {
        private MonsterDefinition _definition;
        private StubPathfinder _pathfinder;
        private int _nextEntityId;

        [SetUp]
        public void SetUp()
        {
            _definition = new MonsterDefinition
            {
                monsterName = "Screamer",
                archetype = MonsterArchetype.Ambush,
                maxHP = 8,
                moveSpeedTilesPerTick = 0.5f,
                detectionRangeTiles = 4,
                damagePerHit = 3,
                attackCooldownTicks = 6,
                initialState = MonsterStateId.Idle,
                spawnCueKey = "screamer_spawn",
                detectionCueKey = "screamer_scream",
                attackCueKey = "screamer_attack",
                hurtCueKey = "screamer_hurt",
                deathCueKey = "screamer_death"
            };

            _pathfinder = new StubPathfinder();
            _nextEntityId = 1001;
        }

        private ScreamerBehavior CreateScreamer(int x, int z)
        {
            return new ScreamerBehavior(
                _definition,
                _pathfinder,
                _nextEntityId++,
                new TileCoord(x, z),
                seed: 42);
        }

        private MonsterContext CreateContext(int playerX, int playerZ, int tick, float budgetMs = 1.0f)
        {
            var gameState = new GameState
            {
                CurrentTick = tick,
                Champion = new ChampionState(playerX, playerZ, 0)
            };
            return new MonsterContext(gameState, tick, budgetMs, null);
        }

        // ─── Initialization ──────────────────────────────────────────

        [Test]
        public void Initialize_SetsCorrectState()
        {
            var screamer = CreateScreamer(5, 5);

            Assert.AreEqual(MonsterStateId.Idle, screamer.CurrentState);
            Assert.AreEqual(8, screamer.CurrentHP);
            Assert.IsTrue(screamer.IsAlive);
            Assert.AreEqual(new TileCoord(5, 5), screamer.Position);
            Assert.Greater(screamer.EntityId, 0);
        }

        // ─── Idle → Alert Transition ──────────────────────────────────

        [Test]
        public void Tick_IdleToAlert_WhenPlayerInDetectionRange()
        {
            var screamer = CreateScreamer(0, 0);

            // Player at (2,2) — distance 4, within detection range
            var context = CreateContext(playerX: 2, playerZ: 2, tick: 1);
            screamer.Tick(context);

            Assert.AreEqual(MonsterStateId.Alert, screamer.CurrentState);
            Assert.AreEqual("screamer_scream", screamer.LastCueEmitted,
                "Screamer should emit scream cue on Alert entry");
        }

        [Test]
        public void Tick_Idle_StaysIdle_WhenPlayerOutOfRange()
        {
            var screamer = CreateScreamer(0, 0);

            // Player at (10, 0) — distance 10, outside detection range 4
            var context = CreateContext(playerX: 10, playerZ: 0, tick: 1);
            screamer.Tick(context);

            Assert.AreEqual(MonsterStateId.Idle, screamer.CurrentState);
            Assert.IsNull(screamer.LastCueEmitted);
        }

        // ─── Alert → Attack Transition ────────────────────────────────

        [Test]
        public void Tick_AlertToAttack_WhenAdjacentAndCooldownReady()
        {
            var screamer = CreateScreamer(0, 0);

            // Player adjacent at (0,1) — distance 1
            var context = CreateContext(playerX: 0, playerZ: 1, tick: 10);
            screamer.Tick(context);

            Assert.AreEqual(MonsterStateId.Attack, screamer.CurrentState);
        }

        [Test]
        public void Tick_Attack_DealsDamage_WhenAdjacent()
        {
            var screamer = CreateScreamer(0, 0);

            // Player at (0,1) — adjacent
            var context = CreateContext(playerX: 0, playerZ: 1, tick: 8);
            screamer.Tick(context);

            Assert.AreEqual(MonsterStateId.Attack, screamer.CurrentState);
            Assert.IsNotNull(screamer.PendingAttackRequest);
            Assert.AreEqual(3, screamer.PendingAttackRequest.Value.Damage);
            Assert.AreEqual(screamer.EntityId, screamer.PendingAttackRequest.Value.SourceEntityId);
        }

        // ─── Hurt → Death Transition ──────────────────────────────────

        [Test]
        public void TakeDamage_TransitionsToHurt_WhenNotLethal()
        {
            var screamer = CreateScreamer(5, 5);

            screamer.TakeDamage(3, currentTick: 10);

            Assert.AreEqual(5, screamer.CurrentHP);
            Assert.AreEqual(MonsterStateId.Hurt, screamer.CurrentState);
            Assert.IsTrue(screamer.IsAlive);
            Assert.AreEqual("screamer_hurt", screamer.LastCueEmitted);
        }

        [Test]
        public void TakeDamage_TransitionsToDeath_WhenLethal()
        {
            var screamer = CreateScreamer(5, 5);

            screamer.TakeDamage(10, currentTick: 10);

            Assert.AreEqual(0, screamer.CurrentHP);
            Assert.IsFalse(screamer.IsAlive);
            Assert.AreEqual(MonsterStateId.Death, screamer.CurrentState);
            Assert.AreEqual("screamer_death", screamer.LastCueEmitted);
        }

        // ─── Cooldown Timing ──────────────────────────────────────────

        [Test]
        public void Tick_Cooldown_PreventsAttack_UntilElapsed()
        {
            var screamer = CreateScreamer(0, 0);

            // Tick 1: attack (adjacent, cooldown ready since lastAttackTick = -cooldown)
            var ctx1 = CreateContext(playerX: 0, playerZ: 1, tick: 1);
            screamer.Tick(ctx1);
            Assert.IsNotNull(screamer.PendingAttackRequest, "First attack should occur");

            // Tick 2: still adjacent but on cooldown (6 tick cooldown)
            var ctx2 = CreateContext(playerX: 0, playerZ: 1, tick: 2);
            screamer.Tick(ctx2);
            Assert.AreEqual(MonsterStateId.Cooldown, screamer.CurrentState,
                "Should enter cooldown after attacking");
            Assert.IsNull(screamer.PendingAttackRequest, "Should not attack during cooldown");

            // Tick 7: cooldown elapsed (1 + 6 = 7)
            var ctx7 = CreateContext(playerX: 0, playerZ: 1, tick: 7);
            screamer.Tick(ctx7);
            Assert.AreEqual(MonsterStateId.Attack, screamer.CurrentState);
            Assert.IsNotNull(screamer.PendingAttackRequest, "Should attack after cooldown");
        }

        // ─── Cue Emission ─────────────────────────────────────────────

        [Test]
        public void Tick_Alert_EmitsScreamCue()
        {
            var screamer = CreateScreamer(0, 0);

            var context = CreateContext(playerX: 2, playerZ: 2, tick: 1);
            screamer.Tick(context);

            Assert.AreEqual("screamer_scream", screamer.LastCueEmitted);
        }

        [Test]
        public void Tick_Attack_EmitsAttackCue()
        {
            var screamer = CreateScreamer(0, 0);

            var context = CreateContext(playerX: 0, playerZ: 1, tick: 1);
            screamer.Tick(context);

            Assert.AreEqual("screamer_attack", screamer.LastCueEmitted);
        }

        // ─── Edge Cases ────────────────────────────────────────────────

        [Test]
        public void Tick_DeadMonster_DoesNothing()
        {
            var screamer = CreateScreamer(0, 0);
            screamer.TakeDamage(100, currentTick: 5);

            var context = CreateContext(playerX: 0, playerZ: 0, tick: 10);
            screamer.Tick(context);

            Assert.AreEqual(MonsterStateId.Death, screamer.CurrentState);
            Assert.IsNull(screamer.PendingMoveTarget);
            Assert.IsNull(screamer.PendingAttackRequest);
        }

        [Test]
        public void Tick_NullGameState_DoesNotCrash()
        {
            var screamer = CreateScreamer(0, 0);

            var context = new MonsterContext(null, 0, 1.0f, null);
            Assert.DoesNotThrow(() => screamer.Tick(context));
        }

        [Test]
        public void Tick_NoChampion_DoesNotCrash()
        {
            var screamer = CreateScreamer(0, 0);

            var gameState = new GameState { CurrentTick = 0, Champion = null };
            var context = new MonsterContext(gameState, 0, 1.0f, null);
            Assert.DoesNotThrow(() => screamer.Tick(context));
        }

        [Test]
        public void Tick_DuplicateTick_Skipped()
        {
            var screamer = CreateScreamer(0, 0);

            var context = CreateContext(playerX: 2, playerZ: 2, tick: 1);
            screamer.Tick(context);
            Assert.AreEqual(MonsterStateId.Alert, screamer.CurrentState);

            // Same tick again — should be skipped
            screamer.Tick(context);
            // State should not change further (no double-processing)
            Assert.AreEqual(MonsterStateId.Alert, screamer.CurrentState);
        }

        // ─── Stubs ─────────────────────────────────────────────────────

        /// <summary>
        /// Stub pathfinder that returns no path (monster doesn't move in tests).
        /// </summary>
        private class StubPathfinder : IGridPathfinder
        {
            public bool TryFindPath(TileCoord start, TileCoord target, int maxSteps, out List<TileCoord> path)
            {
                path = new List<TileCoord>();
                return false;
            }

            public float GetHeuristicCost(TileCoord from, TileCoord to)
            {
                int dx = from.X - to.X;
                int dz = from.Z - to.Z;
                if (dx < 0) dx = -dx;
                if (dz < 0) dz = -dz;
                return dx + dz;
            }

            public void InvalidateCache(TileCoord tile) { }
        }
    }
}
