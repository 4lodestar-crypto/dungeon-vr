using DungeonVR.Shared;

namespace DungeonVR.AI.Interfaces
{
    /// <summary>
    /// High-level states a monster can be in during gameplay.
    /// Maps to transitions driven by perception, health, and tick timers.
    /// </summary>
    public enum MonsterStateId
    {
        Idle,
        Patrol,
        Alert,
        Attack,
        Cooldown,
        Hurt,
        Death
    }

    /// <summary>
    /// Read-only snapshot of a living monster's core state.
    /// Intended for query by AI evaluators and debugging views only — mutation is
    /// owned by the server-side monster entity (not by MonoBehaviour).
    /// </summary>
    public interface IMonsterState
    {
        /// <summary>Current high-level state.</summary>
        MonsterStateId State { get; }

        /// <summary>Grid-aligned tile position (X, Z).</summary>
        TileCoord Position { get; }

        /// <summary>Current hit points remaining.</summary>
        int CurrentHP { get; }

        /// <summary>True while CurrentHP > 0.</summary>
        bool IsAlive { get; }
    }

    /// <summary>
    /// Snapshot of global game state passed into AI evaluation each tick.
    /// Provided by the Gameplay Systems layer; never references Unity scene objects.
    /// </summary>
    public readonly struct MonsterContext
    {
        /// <summary>Opaque handle to the current game world state (players, tiles, timers, etc.).</summary>
        public readonly object GameState;

        /// <summary>Monotonically incrementing server tick.</summary>
        public readonly int CurrentTick;

        /// <summary>Remaining budget (ms) for this monster's evaluation.</summary>
        public readonly float BudgetMs;

        /// <summary>Grid query interface — the only way AI reads tile-level data (no Physics queries).</summary>
        public readonly IGridQueryService GridQuery;

        public MonsterContext(object gameState, int currentTick, float budgetMs, IGridQueryService gridQuery)
        {
            GameState = gameState;
            CurrentTick = currentTick;
            BudgetMs = budgetMs;
            GridQuery = gridQuery;
        }
    }

    /// <summary>
    /// Contract for a single monster's AI brain.
    /// </summary>
    public interface IMonsterBehavior
    {
        /// <summary>
        /// Called once per server tick while the monster is alive.
        /// Mutates internal state; the owning entity applies the resulting commands
        /// (movement, attack requests) back onto the server entity after evaluation.
        /// </summary>
        /// <param name="context">Read-only snapshot of world state for this tick.</param>
        void Tick(MonsterContext context);

        /// <summary>
        /// Evaluate current situation and return the most appropriate state.
        /// The caller uses this to decide which Behavior sub-system to Tick next.
        /// Pure query — does not mutate internal state.
        /// </summary>
        MonsterStateId Evaluate();
    }

    /// <summary>
    /// Describes a single spawn request at a specific tile.
    /// Created by the content-layer spawn table and submitted to the spawn system.
    /// </summary>
    public readonly struct SpawnRequest
    {
        /// <summary>Tile coordinates where the monster should appear.</summary>
        public readonly TileCoord Position;

        /// <summary>Index (0-7) into the facing-direction lookup table.</summary>
        public readonly int FacingIndex;

        /// <summary>Lookup key into the monster definition catalog.</summary>
        public readonly string MonsterDefinitionId;

        /// <summary>Number of game ticks to wait before the monster becomes active.</summary>
        public readonly int SpawnDelayTicks;

        public SpawnRequest(TileCoord position, int facingIndex, string monsterDefinitionId, int spawnDelayTicks)
        {
            Position = position;
            FacingIndex = facingIndex;
            MonsterDefinitionId = monsterDefinitionId;
            SpawnDelayTicks = spawnDelayTicks;
        }
    }

    /// <summary>
    /// Handles receiving spawn requests, instantiating the server-side entity,
    /// and returning the newly created entity ID.
    /// </summary>
    public interface IMonsterSpawnHandler
    {
        /// <summary>
        /// Attempt to spawn a monster from the given request.
        /// </summary>
        /// <param name="request">Spawn parameters (position, facing, definition).</param>
        /// <param name="entityId">Out: server-side entity ID if successful, -1 otherwise.</param>
        /// <returns>True if the spawn was accepted.</returns>
        bool TrySpawn(SpawnRequest request, out int entityId);
    }

    // ──────────────────────────────────────────────────────────
    // Contract stubs that will be fully defined in V1+
    // ──────────────────────────────────────────────────────────

    /// <summary>
    /// V1+ — The only interface allowed for querying tile-grid data during AI evaluation.
    /// V0 placeholder — use DungeonVR.Shared.Interfaces.IGridQueryService instead.
    /// </summary>
    public interface IGridQueryService
    {
        // V0 stub — use DungeonVR.Shared.Interfaces.IGridQueryService in V1
    }
}
