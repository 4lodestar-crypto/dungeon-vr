using DungeonVR.AI.Data;
using DungeonVR.AI.Interfaces;
using DungeonVR.Shared.Data;

namespace DungeonVR.AI.Logic
{
    /// <summary>
    /// Reusable finite state machine for monster AI.
    /// Transitions are data-driven via MonsterDefinition and MonsterContext perception.
    /// Thread-safe for deterministic server execution; no Unity dependencies.
    /// </summary>
    public class MonsterStateMachine
    {
        private MonsterStateId _currentState;
        private int _stateEnteredTick;
        private int _lastAttackTick;
        private int _currentHP;
        private readonly MonsterDefinition _definition;

        /// <summary>Current high-level state.</summary>
        public MonsterStateId CurrentState => _currentState;

        /// <summary>Server tick when the current state was entered.</summary>
        public int StateEnteredTick => _stateEnteredTick;

        /// <summary>Server tick of the last attack execution.</summary>
        public int LastAttackTick => _lastAttackTick;

        /// <summary>Current hit points.</summary>
        public int CurrentHP => _currentHP;

        /// <summary>True while HP > 0.</summary>
        public bool IsAlive => _currentHP > 0;

        /// <summary>True if attack cooldown has elapsed.</summary>
        public bool CanAttack(int currentTick)
        {
            return (currentTick - _lastAttackTick) >= _definition.attackCooldownTicks;
        }

        public MonsterStateMachine(MonsterDefinition definition)
        {
            _definition = definition;
            _currentState = definition.initialState;
            _currentHP = definition.maxHP;
            _stateEnteredTick = 0;
            _lastAttackTick = -definition.attackCooldownTicks; // can attack immediately on spawn
        }

        /// <summary>
        /// Sets internal state directly (used for test setup or forced transitions).
        /// Does NOT fire transition side-effects.
        /// </summary>
        public void SetStateDirect(MonsterStateId state, int currentTick)
        {
            _currentState = state;
            _stateEnteredTick = currentTick;
        }

        /// <summary>
        /// Records that an attack was executed this tick.
        /// </summary>
        public void RecordAttack(int currentTick)
        {
            _lastAttackTick = currentTick;
        }

        /// <summary>
        /// Applies damage and returns true if the monster died from this hit.
        /// </summary>
        public bool ApplyDamage(int amount)
        {
            _currentHP -= amount;
            if (_currentHP < 0)
                _currentHP = 0;
            return _currentHP <= 0;
        }

        /// <summary>
        /// Evaluates the next state based on perception and current state.
        /// Pure query — does not mutate internal state or fire transitions.
        /// </summary>
        /// <param name="playerPosition">Current player/champion tile position.</param>
        /// <param name="monsterPosition">Current monster tile position.</param>
        /// <param name="currentTick">Server tick number.</param>
        /// <returns>The state the monster should transition to.</returns>
        public MonsterStateId Evaluate(
            TileCoord playerPosition,
            TileCoord monsterPosition,
            int currentTick)
        {
            // Death is terminal
            if (!IsAlive)
                return MonsterStateId.Death;

            // Hurt overrides everything except Death
            // (Damage was just applied — external caller sets HP then calls Evaluate)
            if (_currentState == MonsterStateId.Hurt && IsAlive)
            {
                // After Hurt, transition based on distance
                float dist = ManhattanDistance(monsterPosition, playerPosition);
                if (dist <= 1f)
                    return MonsterStateId.Attack;
                if (dist <= _definition.detectionRangeTiles)
                    return MonsterStateId.Alert;
                return MonsterStateId.Idle;
            }

            // Attack: if adjacent and cooldown ready, stay in Attack
            if (IsAdjacent(monsterPosition, playerPosition))
            {
                if (CanAttack(currentTick))
                    return MonsterStateId.Attack;
                // Adjacent but on cooldown — wait in Cooldown
                return MonsterStateId.Cooldown;
            }

            // Alert/Detection: player within detection range
            float detectionDist = ManhattanDistance(monsterPosition, playerPosition);
            if (detectionDist <= _definition.detectionRangeTiles)
            {
                return MonsterStateId.Alert;
            }

            // Cooldown timeout: return to Idle if cooldown elapsed and player not nearby
            if (_currentState == MonsterStateId.Cooldown)
            {
                if (CanAttack(currentTick) && detectionDist > _definition.detectionRangeTiles)
                    return MonsterStateId.Idle;
                return MonsterStateId.Cooldown;
            }

            // Idle: patrol not implemented in V0 — remain Idle
            if (_currentState == MonsterStateId.Patrol)
            {
                return detectionDist <= _definition.detectionRangeTiles
                    ? MonsterStateId.Alert
                    : MonsterStateId.Patrol;
            }

            // Attack when not adjacent: fall back to Alert (chase)
            if (_currentState == MonsterStateId.Attack && !IsAdjacent(monsterPosition, playerPosition))
            {
                return MonsterStateId.Alert;
            }

            // Default: stay in current state
            return _currentState;
        }

        /// <summary>
        /// Commits a state transition. Returns true if the state changed.
        /// Call this after Evaluate() to apply the transition.
        /// </summary>
        public bool TransitionTo(MonsterStateId newState, int currentTick)
        {
            if (newState == _currentState)
                return false;

            _currentState = newState;
            _stateEnteredTick = currentTick;
            return true;
        }

        /// <summary>
        /// Manhattan distance between two tile coordinates.
        /// Admissible heuristic for 4-directional grid movement.
        /// </summary>
        public static int ManhattanDistance(TileCoord a, TileCoord b)
        {
            int dx = a.X - b.X;
            int dz = a.Z - b.Z;
            if (dx < 0) dx = -dx;
            if (dz < 0) dz = -dz;
            return dx + dz;
        }

        /// <summary>
        /// Returns true if two tiles are adjacent (Manhattan distance 1, not diagonal).
        /// </summary>
        public static bool IsAdjacent(TileCoord a, TileCoord b)
        {
            int dx = a.X - b.X;
            int dz = a.Z - b.Z;
            if (dx < 0) dx = -dx;
            if (dz < 0) dz = -dz;
            return (dx + dz) == 1;
        }

        /// <summary>
        /// Number of ticks remaining until the attack cooldown expires.
        /// Returns 0 if cooldown has already elapsed.
        /// </summary>
        public int CooldownRemaining(int currentTick)
        {
            int elapsed = currentTick - _lastAttackTick;
            int remaining = _definition.attackCooldownTicks - elapsed;
            return remaining > 0 ? remaining : 0;
        }
    }
}
