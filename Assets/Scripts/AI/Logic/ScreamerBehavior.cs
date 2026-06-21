using DungeonVR.AI.Data;
using DungeonVR.AI.Interfaces;
using DungeonVR.Shared;
using DungeonVR.Shared.Data;
using System.Collections.Generic;

namespace DungeonVR.AI.Logic
{
    /// <summary>
    /// Concrete AI behavior for the Screamer — an Ambush-type monster.
    ///
    /// State machine: Idle (hidden) → Alert (scream on detection) → Attack → Cooldown → repeat.
    /// Signature behavior: emits a loud scream cue upon detecting the player (Alert entry).
    ///
    /// Server-authoritative: mutates internal state only; the owning server entity reads
    /// PendingAttackRequest and PendingMoveTarget after each Tick() to apply effects.
    ///
    /// All logic is deterministic: seeded System.Random, grid-based perception (Manhattan distance),
    /// tick-count timers. No UnityEngine.Random, no Time.deltaTime, no Physics queries.
    /// Zero allocations in Tick() hot path after initialisation.
    /// </summary>
    public class ScreamerBehavior : IMonsterBehavior
    {
        // ── Immutable configuration ─────────────────────────────────────
        private readonly MonsterDefinition _definition;
        private readonly IGridPathfinder _pathfinder;
        private readonly System.Random _rng;
        private readonly int _entityId;

        // ── Mutable state ───────────────────────────────────────────────
        private MonsterStateId _currentState;
        private int _currentHP;
        private TileCoord _position;
        private int _stateEnteredTick;
        private int _lastAttackTick;
        private int _lastTickProcessed;

        // ── Movement accumulation (fractional tiles/tick) ───────────────
        private float _movementAccumulator;

        // ── Pathfinding state ───────────────────────────────────────────
        private List<TileCoord> _currentPath;
        private int _currentPathIndex;
        private TileCoord _lastPathGoal;

        // ── Cue emission (consumed by presentation layer) ───────────────
        private string _lastCueEmitted;
        private bool _alertEnteredThisFrame;

        // ── Public output (read by owning entity after Tick) ────────────

        /// <summary>Non-null if the monster attacked this tick. Reset each Tick().</summary>
        public AttackRequest? PendingAttackRequest { get; private set; }

        /// <summary>Non-null if the monster moved to a new tile this tick.</summary>
        public TileCoord? PendingMoveTarget { get; private set; }

        /// <summary>Last cue key emitted (scream, attack, hurt, death). Read once, cleared next Tick().</summary>
        public string LastCueEmitted => _lastCueEmitted;

        // ── Read-only queries ──────────────────────────────────────────

        public MonsterStateId CurrentState => _currentState;
        public TileCoord Position => _position;
        public int CurrentHP => _currentHP;
        public int MaxHP => _definition.maxHP;
        public bool IsAlive => _currentHP > 0;
        public int EntityId => _entityId;

        // ── Construction ───────────────────────────────────────────────

        /// <summary>
        /// Creates a Screamer behavior bound to a definition, pathfinder, entity ID, spawn position, and seed.
        /// </summary>
        public ScreamerBehavior(
            MonsterDefinition definition,
            IGridPathfinder pathfinder,
            int entityId,
            TileCoord spawnPosition,
            int seed)
        {
            _definition = definition ?? throw new System.ArgumentNullException(nameof(definition));
            _pathfinder = pathfinder ?? throw new System.ArgumentNullException(nameof(pathfinder));
            _entityId = entityId;
            _position = spawnPosition;
            _rng = new System.Random(seed);

            _currentState = definition.initialState;
            _currentHP = definition.maxHP;
            _stateEnteredTick = 0;
            _lastAttackTick = -definition.attackCooldownTicks; // can attack immediately
            _lastTickProcessed = -1;
            _movementAccumulator = 0f;
            _currentPath = null;
            _currentPathIndex = 0;
            _lastPathGoal = new TileCoord(-1, -1);
            _lastCueEmitted = null;
            _alertEnteredThisFrame = false;
        }

        // ── IMonsterBehavior ───────────────────────────────────────────

        public void Tick(MonsterContext context)
        {
            // ── Death check ────────────────────────────────────────────
            if (_currentHP <= 0)
            {
                _currentState = MonsterStateId.Death;
                ClearPendingOutputs();
                return;
            }

            // ── Skip duplicate ticks ───────────────────────────────────
            if (context.CurrentTick == _lastTickProcessed)
                return;

            // ── Clear outputs from previous tick ───────────────────────
            ClearPendingOutputs();

            // ── Extract champion position from GameState ───────────────
            GameState gameState = context.GameState as GameState;
            if (gameState?.Champion == null)
            {
                _lastTickProcessed = context.CurrentTick;
                return;
            }

            TileCoord championPos = new TileCoord(
                gameState.Champion.GridX,
                gameState.Champion.GridZ);

            // ── Evaluate next state (pure query) ───────────────────────
            MonsterStateId nextState = EvaluateNextState(championPos, context.CurrentTick);

            // ── Detect transition and fire entry actions ───────────────
            if (nextState != _currentState)
            {
                OnStateExited(_currentState);
                _currentState = nextState;
                _stateEnteredTick = context.CurrentTick;
                OnStateEntered(nextState);
            }

            // ── Execute per-tick behavior for current state ────────────
            switch (_currentState)
            {
                case MonsterStateId.Idle:
                    TickIdle(championPos);
                    break;
                case MonsterStateId.Alert:
                    TickAlert(context, championPos);
                    break;
                case MonsterStateId.Attack:
                    TickAttack(context, championPos);
                    break;
                case MonsterStateId.Cooldown:
                    TickCooldown(championPos);
                    break;
                case MonsterStateId.Hurt:
                    TickHurt(championPos, context.CurrentTick);
                    break;
                case MonsterStateId.Death:
                    break; // Terminal
                default:
                    _currentState = MonsterStateId.Idle;
                    break;
            }

            _lastTickProcessed = context.CurrentTick;
        }

        public MonsterStateId Evaluate()
        {
            return _currentState;
        }

        // ── External mutation ──────────────────────────────────────────

        /// <summary>
        /// Apply damage from an external source. Transitions to Death if HP ≤ 0, else Hurt.
        /// </summary>
        public void TakeDamage(int damage, int currentTick)
        {
            if (_currentHP <= 0) return;
            _currentHP = System.Math.Max(0, _currentHP - damage);

            if (_currentHP <= 0)
            {
                _currentState = MonsterStateId.Death;
                _stateEnteredTick = currentTick;
                _lastCueEmitted = _definition.deathCueKey;
            }
            else
            {
                _currentState = MonsterStateId.Hurt;
                _stateEnteredTick = currentTick;
                _lastCueEmitted = _definition.hurtCueKey;
            }
        }

        /// <summary>
        /// Force-set the monster's position (used by teleport / spawn-in effects).
        /// </summary>
        public void SetPosition(TileCoord pos)
        {
            _position = pos;
            _currentPath = null;
            _currentPathIndex = 0;
            _movementAccumulator = 0f;
        }

        // ── State Evaluation ───────────────────────────────────────────

        private MonsterStateId EvaluateNextState(TileCoord championPos, int currentTick)
        {
            if (_currentHP <= 0)
                return MonsterStateId.Death;

            int dist = ManhattanDistance(_position, championPos);
            bool adjacent = (dist == 1);
            bool inRange = (dist <= _definition.detectionRangeTiles);
            bool cooldownReady = (currentTick - _lastAttackTick) >= _definition.attackCooldownTicks;

            // Hurt overrides: evaluate based on distance after taking damage
            if (_currentState == MonsterStateId.Hurt)
            {
                if (adjacent && cooldownReady)
                    return MonsterStateId.Attack;
                if (inRange)
                    return MonsterStateId.Alert;
                return MonsterStateId.Idle;
            }

            // Adjacent + cooldown ready → Attack
            if (adjacent && cooldownReady)
                return MonsterStateId.Attack;

            // Adjacent but cooldown not ready → Cooldown
            if (adjacent && !cooldownReady)
                return MonsterStateId.Cooldown;

            // In detection range but not adjacent → Alert
            if (inRange)
                return MonsterStateId.Alert;

            // In Cooldown but player out of range → Idle after cooldown
            if (_currentState == MonsterStateId.Cooldown && cooldownReady)
                return MonsterStateId.Idle;

            // Attack state but no longer adjacent → Alert (pursuit)
            if (_currentState == MonsterStateId.Attack && !adjacent)
                return MonsterStateId.Alert;

            // Stay in current state
            return _currentState;
        }

        // ── State Entry/Exit ───────────────────────────────────────────

        private void OnStateEntered(MonsterStateId state)
        {
            switch (state)
            {
                case MonsterStateId.Alert:
                    // SCREAM — the Screamer's signature behavior
                    _lastCueEmitted = _definition.detectionCueKey;
                    _alertEnteredThisFrame = true;
                    _currentPath = null;
                    _currentPathIndex = 0;
                    _movementAccumulator = 0f;
                    break;
                case MonsterStateId.Attack:
                    _lastCueEmitted = _definition.attackCueKey;
                    _movementAccumulator = 0f;
                    break;
                case MonsterStateId.Cooldown:
                    _currentPath = null;
                    _currentPathIndex = 0;
                    break;
            }
        }

        private void OnStateExited(MonsterStateId state)
        {
            _alertEnteredThisFrame = false;
        }

        // ── Per-Tick State Behaviors ───────────────────────────────────

        private void TickIdle(TileCoord championPos)
        {
            // Ambush: hidden, waiting. No movement or actions.
            // Detection is handled by EvaluateNextState — if player enters range,
            // transition to Alert happens automatically.
        }

        private void TickAlert(MonsterContext context, TileCoord championPos)
        {
            int dist = ManhattanDistance(_position, championPos);

            // Lost track — fall back to Idle
            if (dist > _definition.detectionRangeTiles)
            {
                _currentState = MonsterStateId.Idle;
                _currentPath = null;
                _currentPathIndex = 0;
                _movementAccumulator = 0f;
                return;
            }

            // Recompute path if champion moved or no path exists
            if (_currentPath == null || _currentPathIndex >= _currentPath.Count ||
                championPos != _lastPathGoal)
            {
                if (_pathfinder.TryFindPath(_position, championPos, maxSteps: 64, out var path))
                {
                    _currentPath = path;
                    _currentPathIndex = 0;
                    _lastPathGoal = championPos;
                }
                else
                {
                    _currentPath = null;
                    _currentPathIndex = 0;
                    return;
                }
            }

            // Move along path (fractional movement for speed < 1.0 tiles/tick)
            if (_currentPath != null && _currentPath.Count > 0 && _currentPathIndex < _currentPath.Count)
            {
                _movementAccumulator += _definition.moveSpeedTilesPerTick;
                int tilesToMove = (int)_movementAccumulator;

                for (int i = 0; i < tilesToMove && _currentPathIndex < _currentPath.Count; i++)
                {
                    _position = _currentPath[_currentPathIndex];
                    _currentPathIndex++;
                    _movementAccumulator -= 1f;
                }

                if (_currentPathIndex >= _currentPath.Count)
                {
                    _currentPath = null;
                    _currentPathIndex = 0;
                }

                PendingMoveTarget = _position;
            }
        }

        private void TickAttack(MonsterContext context, TileCoord championPos)
        {
            int dist = ManhattanDistance(_position, championPos);

            // Lost adjacency — return to pursuit
            if (dist > 1)
            {
                _currentState = MonsterStateId.Alert;
                return;
            }

            // Attack on cooldown ready
            if ((context.CurrentTick - _lastAttackTick) >= _definition.attackCooldownTicks)
            {
                PendingAttackRequest = new AttackRequest(
                    sourceEntityId: _entityId,
                    targetEntityId: 0, // V0: champion entity ID is always 0
                    damage: _definition.damagePerHit,
                    tickStamp: context.CurrentTick);

                _lastAttackTick = context.CurrentTick;
            }
        }

        private void TickCooldown(TileCoord championPos)
        {
            // Wait for cooldown. If player moves away, EvaluateNextState
            // will transition to Idle or Alert automatically.
        }

        private void TickHurt(TileCoord championPos, int currentTick)
        {
            // Hurt is transient — EvaluateNextState decides the follow-up state.
            // No additional per-tick behavior needed.
        }

        // ── Helpers ────────────────────────────────────────────────────

        private static int ManhattanDistance(TileCoord a, TileCoord b)
        {
            int dx = a.X - b.X;
            int dz = a.Z - b.Z;
            if (dx < 0) dx = -dx;
            if (dz < 0) dz = -dz;
            return dx + dz;
        }

        private void ClearPendingOutputs()
        {
            PendingAttackRequest = null;
            PendingMoveTarget = null;
            _lastCueEmitted = null;
            _alertEnteredThisFrame = false;
        }

        public override string ToString()
            => $"{_definition.monsterName}[{_entityId}] @ {_position} | {_currentState} | HP:{_currentHP}/{_definition.maxHP}";
    }
}
