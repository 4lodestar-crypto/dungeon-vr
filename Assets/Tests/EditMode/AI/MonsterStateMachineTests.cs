using DungeonVR.AI.Data;
using DungeonVR.AI.Interfaces;
using DungeonVR.AI.Logic;
using DungeonVR.Shared;
using DungeonVR.Shared.Data;
using NUnit.Framework;

namespace DungeonVR.Tests.EditMode.AI
{
    /// <summary>
    /// EditMode unit tests for MonsterStateMachine.
    /// Verifies all state transitions are correct and deterministic.
    /// No Unity dependencies — pure logical tests.
    /// </summary>
    [TestFixture]
    public class MonsterStateMachineTests
    {
        private MonsterDefinition CreateDefinition(
            int maxHP = 10,
            float detectionRange = 4f,
            int attackCooldown = 3,
            MonsterStateId initialState = MonsterStateId.Idle)
        {
            MonsterDefinition def = UnityEngine.ScriptableObject.CreateInstance<MonsterDefinition>();
            def.monsterName = "TestMonster";
            def.archetype = MonsterArchetype.Ambush;
            def.maxHP = maxHP;
            def.moveSpeedTilesPerTick = 1f;
            def.detectionRangeTiles = detectionRange;
            def.damagePerHit = 3;
            def.attackCooldownTicks = attackCooldown;
            def.initialState = initialState;
            return def;
        }

        [Test]
        public void InitialState_MatchesDefinition()
        {
            var def = CreateDefinition(initialState: MonsterStateId.Idle);
            var fsm = new MonsterStateMachine(def);

            Assert.AreEqual(MonsterStateId.Idle, fsm.CurrentState);
            Assert.AreEqual(10, fsm.CurrentHP);
            Assert.IsTrue(fsm.IsAlive);
        }

        [Test]
        public void InitialState_Patrol_MatchesDefinition()
        {
            var def = CreateDefinition(initialState: MonsterStateId.Patrol);
            var fsm = new MonsterStateMachine(def);

            Assert.AreEqual(MonsterStateId.Patrol, fsm.CurrentState);
        }

        [Test]
        public void Evaluate_IdleToAlert_WhenPlayerInRange()
        {
            var def = CreateDefinition(detectionRange: 4f);
            var fsm = new MonsterStateMachine(def);
            fsm.SetStateDirect(MonsterStateId.Idle, 0);

            // Player at distance 3 (within range 4)
            var playerPos = new TileCoord(3, 3);
            var monsterPos = new TileCoord(0, 0);

            var result = fsm.Evaluate(playerPos, monsterPos, 0);
            Assert.AreEqual(MonsterStateId.Alert, result);
        }

        [Test]
        public void Evaluate_Idle_StaysIdle_WhenPlayerOutOfRange()
        {
            var def = CreateDefinition(detectionRange: 4f);
            var fsm = new MonsterStateMachine(def);
            fsm.SetStateDirect(MonsterStateId.Idle, 0);

            // Player at distance 10 (outside range 4)
            var playerPos = new TileCoord(10, 0);
            var monsterPos = new TileCoord(0, 0);

            var result = fsm.Evaluate(playerPos, monsterPos, 0);
            Assert.AreEqual(MonsterStateId.Idle, result);
        }

        [Test]
        public void Evaluate_AlertToAttack_WhenAdjacent_AndCooldownReady()
        {
            var def = CreateDefinition(detectionRange: 4f, attackCooldown: 3);
            var fsm = new MonsterStateMachine(def);
            fsm.SetStateDirect(MonsterStateId.Alert, 0);

            // Adjacent tile
            var playerPos = new TileCoord(0, 1);
            var monsterPos = new TileCoord(0, 0);

            // Should transition to Attack (adjacent + cooldown ready)
            var result = fsm.Evaluate(playerPos, monsterPos, 10);
            Assert.AreEqual(MonsterStateId.Attack, result);
        }

        [Test]
        public void Evaluate_Alert_StaysAlert_WhenInRange_ButNotAdjacent()
        {
            var def = CreateDefinition(detectionRange: 4f);
            var fsm = new MonsterStateMachine(def);
            fsm.SetStateDirect(MonsterStateId.Alert, 0);

            // Within range but not adjacent
            var playerPos = new TileCoord(2, 2);
            var monsterPos = new TileCoord(0, 0);

            var result = fsm.Evaluate(playerPos, monsterPos, 5);
            Assert.AreEqual(MonsterStateId.Alert, result);
        }

        [Test]
        public void Evaluate_Attack_WhenAdjacent_AndCooldownReady()
        {
            var def = CreateDefinition(attackCooldown: 3);
            var fsm = new MonsterStateMachine(def);
            fsm.SetStateDirect(MonsterStateId.Attack, 0);

            // Record an attack at tick 5, wait for cooldown
            fsm.RecordAttack(5);

            // Adjacent at tick 9 (cooldown=3, elapsed=4, ready)
            var playerPos = new TileCoord(0, 1);
            var monsterPos = new TileCoord(0, 0);

            var result = fsm.Evaluate(playerPos, monsterPos, 9);
            Assert.AreEqual(MonsterStateId.Attack, result);
        }

        [Test]
        public void Evaluate_AttackToCooldown_WhenAdjacent_ButCooldownNotReady()
        {
            var def = CreateDefinition(attackCooldown: 6);
            var fsm = new MonsterStateMachine(def);
            fsm.SetStateDirect(MonsterStateId.Attack, 0);

            // Record attack at tick 5
            fsm.RecordAttack(5);

            // Adjacent at tick 7 (cooldown=6, elapsed=2, not ready)
            var playerPos = new TileCoord(0, 1);
            var monsterPos = new TileCoord(0, 0);

            var result = fsm.Evaluate(playerPos, monsterPos, 7);
            Assert.AreEqual(MonsterStateId.Cooldown, result);
        }

        [Test]
        public void Evaluate_AttackToAlert_WhenNotAdjacent()
        {
            var def = CreateDefinition(detectionRange: 4f);
            var fsm = new MonsterStateMachine(def);
            fsm.SetStateDirect(MonsterStateId.Attack, 0);

            // Player moved away — not adjacent but still in range
            var playerPos = new TileCoord(2, 0);
            var monsterPos = new TileCoord(0, 0);

            var result = fsm.Evaluate(playerPos, monsterPos, 5);
            Assert.AreEqual(MonsterStateId.Alert, result);
        }

        [Test]
        public void Evaluate_Death_WhenHPZero()
        {
            var def = CreateDefinition(maxHP: 5);
            var fsm = new MonsterStateMachine(def);
            fsm.SetStateDirect(MonsterStateId.Idle, 0);

            // Deal lethal damage
            fsm.ApplyDamage(10);

            var playerPos = new TileCoord(0, 0);
            var monsterPos = new TileCoord(1, 0);

            var result = fsm.Evaluate(playerPos, monsterPos, 0);
            Assert.AreEqual(MonsterStateId.Death, result);
            Assert.IsFalse(fsm.IsAlive);
        }

        [Test]
        public void Evaluate_Hurt_SurvivesDamage()
        {
            var def = CreateDefinition(maxHP: 10);
            var fsm = new MonsterStateMachine(def);

            bool died = fsm.ApplyDamage(3);
            Assert.IsFalse(died);
            Assert.AreEqual(7, fsm.CurrentHP);
            Assert.IsTrue(fsm.IsAlive);
        }

        [Test]
        public void Evaluate_Hurt_TransitionsBasedOnDistance()
        {
            var def = CreateDefinition(maxHP: 10, detectionRange: 4f);
            var fsm = new MonsterStateMachine(def);
            fsm.SetStateDirect(MonsterStateId.Hurt, 0);
            fsm.ApplyDamage(3); // HP now 7, still alive

            // Player adjacent — should go to Attack
            var playerPos = new TileCoord(0, 1);
            var monsterPos = new TileCoord(0, 0);

            var result = fsm.Evaluate(playerPos, monsterPos, 10);
            Assert.AreEqual(MonsterStateId.Attack, result);
        }

        [Test]
        public void Evaluate_HurtToAlert_WhenPlayerInRange_NotAdjacent()
        {
            var def = CreateDefinition(maxHP: 10, detectionRange: 4f);
            var fsm = new MonsterStateMachine(def);
            fsm.SetStateDirect(MonsterStateId.Hurt, 0);
            fsm.ApplyDamage(3);

            var playerPos = new TileCoord(3, 0);
            var monsterPos = new TileCoord(0, 0);

            var result = fsm.Evaluate(playerPos, monsterPos, 10);
            Assert.AreEqual(MonsterStateId.Alert, result);
        }

        [Test]
        public void Evaluate_HurtToIdle_WhenPlayerOutOfRange()
        {
            var def = CreateDefinition(maxHP: 10, detectionRange: 4f);
            var fsm = new MonsterStateMachine(def);
            fsm.SetStateDirect(MonsterStateId.Hurt, 0);
            fsm.ApplyDamage(3);

            var playerPos = new TileCoord(10, 0);
            var monsterPos = new TileCoord(0, 0);

            var result = fsm.Evaluate(playerPos, monsterPos, 10);
            Assert.AreEqual(MonsterStateId.Idle, result);
        }

        [Test]
        public void CooldownRemaining_ReturnsCorrectValue()
        {
            var def = CreateDefinition(attackCooldown: 6);
            var fsm = new MonsterStateMachine(def);

            fsm.RecordAttack(10);

            // At tick 13: elapsed = 3, remaining = 3
            Assert.AreEqual(3, fsm.CooldownRemaining(13));

            // At tick 16: elapsed = 6, remaining = 0
            Assert.AreEqual(0, fsm.CooldownRemaining(16));

            // At tick 20: elapsed = 10, remaining = 0
            Assert.AreEqual(0, fsm.CooldownRemaining(20));
        }

        [Test]
        public void CanAttack_WhenCooldownElapsed()
        {
            var def = CreateDefinition(attackCooldown: 3);
            var fsm = new MonsterStateMachine(def);

            // Initial state: can attack (last attack tick = -cooldown)
            Assert.IsTrue(fsm.CanAttack(0));

            fsm.RecordAttack(5);
            Assert.IsFalse(fsm.CanAttack(6));
            Assert.IsFalse(fsm.CanAttack(7));
            Assert.IsTrue(fsm.CanAttack(8)); // 5 + 3 = 8
        }

        [Test]
        public void TransitionTo_DetectsStateChange()
        {
            var def = CreateDefinition();
            var fsm = new MonsterStateMachine(def);

            bool changed = fsm.TransitionTo(MonsterStateId.Alert, 5);
            Assert.IsTrue(changed);
            Assert.AreEqual(MonsterStateId.Alert, fsm.CurrentState);
            Assert.AreEqual(5, fsm.StateEnteredTick);
        }

        [Test]
        public void TransitionTo_SameState_NoChange()
        {
            var def = CreateDefinition(initialState: MonsterStateId.Idle);
            var fsm = new MonsterStateMachine(def);

            bool changed = fsm.TransitionTo(MonsterStateId.Idle, 5);
            Assert.IsFalse(changed);
            Assert.AreEqual(0, fsm.StateEnteredTick); // unchanged from construction
        }

        [Test]
        public void ManhattanDistance_CalculatesCorrectly()
        {
            Assert.AreEqual(0, MonsterStateMachine.ManhattanDistance(
                new TileCoord(0, 0), new TileCoord(0, 0)));

            Assert.AreEqual(4, MonsterStateMachine.ManhattanDistance(
                new TileCoord(0, 0), new TileCoord(2, 2)));

            Assert.AreEqual(5, MonsterStateMachine.ManhattanDistance(
                new TileCoord(1, 2), new TileCoord(4, 4)));

            Assert.AreEqual(3, MonsterStateMachine.ManhattanDistance(
                new TileCoord(5, 5), new TileCoord(2, 5)));
        }

        [Test]
        public void IsAdjacent_DetectsCardinalNeighbors()
        {
            // Adjacent (North)
            Assert.IsTrue(MonsterStateMachine.IsAdjacent(
                new TileCoord(0, 0), new TileCoord(0, 1)));

            // Adjacent (East)
            Assert.IsTrue(MonsterStateMachine.IsAdjacent(
                new TileCoord(0, 0), new TileCoord(1, 0)));

            // Not adjacent (diagonal)
            Assert.IsFalse(MonsterStateMachine.IsAdjacent(
                new TileCoord(0, 0), new TileCoord(1, 1)));

            // Not adjacent (same tile)
            Assert.IsFalse(MonsterStateMachine.IsAdjacent(
                new TileCoord(0, 0), new TileCoord(0, 0)));

            // Not adjacent (2 tiles away)
            Assert.IsFalse(MonsterStateMachine.IsAdjacent(
                new TileCoord(0, 0), new TileCoord(0, 2)));
        }
    }
}
