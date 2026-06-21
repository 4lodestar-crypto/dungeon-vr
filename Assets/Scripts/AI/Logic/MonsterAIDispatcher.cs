using DungeonVR.AI.Interfaces;
using DungeonVR.Shared;
using DungeonVR.Shared.Data;
using DungeonVR.Shared.Interfaces;
using System.Collections.Generic;
using System.Diagnostics;

namespace DungeonVR.AI.Logic
{
    /// <summary>
    /// Dispatches AI ticks to all active monsters each server tick.
    ///
    /// Enforces the global AI tick budget (0.5ms default at 20Hz).
    /// Monsters closest to the player are ticked first — distant monsters
    /// may skip ticks if budget is exhausted.
    ///
    /// Zero allocations in the hot TickAllMonsters path: uses pre-allocated
    /// sort buffer and integer distance keys.
    /// </summary>
    public class MonsterAIDispatcher : IMonsterAIDispatcher
    {
        /// <summary>Default per-tick budget for all monster AI (0.5ms at 20Hz).</summary>
        public const float DefaultBudgetMs = 0.5f;

        /// <summary>Maximum number of concurrently active monsters.</summary>
        public const int MaxMonsters = 256;

        private readonly List<IMonsterBehavior> _activeBehaviors;
        private readonly List<MonsterEntityProxy> _entityProxies;
        private readonly IGridQueryService _gridQuery;

        // Pre-allocated sort buffers (zero allocation during tick).
        private readonly SortEntry[] _sortBuffer;
        private int _sortCount;

        /// <summary>Sort entry for proximity-based tick ordering.</summary>
        private struct SortEntry
        {
            public IMonsterBehavior Behavior;
            public MonsterEntityProxy Proxy;
            public int DistanceSq;
        }

        /// <summary>
        /// Lightweight proxy holding position data for a monster entity.
        /// Avoids boxing IMonsterBehavior for position queries during sorting.
        /// </summary>
        public class MonsterEntityProxy
        {
            public IMonsterBehavior Behavior;
            public TileCoord Position;
            public int EntityId;
            public bool IsActive;
        }

        public MonsterAIDispatcher(IGridQueryService gridQuery)
        {
            _gridQuery = gridQuery;
            _activeBehaviors = new List<IMonsterBehavior>(MaxMonsters);
            _entityProxies = new List<MonsterEntityProxy>(MaxMonsters);
            _sortBuffer = new SortEntry[MaxMonsters];
            _sortCount = 0;
        }

        /// <summary>
        /// Registers a monster for AI ticking. Called by MonsterSpawnHandler on spawn.
        /// </summary>
        public void RegisterMonster(IMonsterBehavior behavior, TileCoord position, int entityId)
        {
            MonsterEntityProxy proxy = new MonsterEntityProxy
            {
                Behavior = behavior,
                Position = position,
                EntityId = entityId,
                IsActive = true
            };

            _activeBehaviors.Add(behavior);
            _entityProxies.Add(proxy);
        }

        /// <summary>
        /// Removes a monster from the dispatch list. Called on death or despawn.
        /// </summary>
        public void UnregisterMonster(int entityId)
        {
            for (int i = 0; i < _entityProxies.Count; i++)
            {
                if (_entityProxies[i].EntityId == entityId)
                {
                    _activeBehaviors.RemoveAt(i);
                    _entityProxies.RemoveAt(i);
                    return;
                }
            }
        }

        /// <summary>
        /// Ticks all active monsters within the global AI budget.
        /// Monsters are sorted by proximity to the player (closest first).
        /// Monsters exceeding the budget skip their tick this frame.
        /// </summary>
        /// <param name="gameStateObj">The current GameState object (cast to GameState internally).</param>
        /// <param name="currentTick">Current server tick number.</param>
        /// <param name="totalBudgetMs">Total AI budget in milliseconds (uses DefaultBudgetMs if ≤ 0).</param>
        public void TickAllMonsters(object gameStateObj, int currentTick, float totalBudgetMs)
        {
            GameState gameState = gameStateObj as GameState;
            if (gameState?.Champion == null || _activeBehaviors.Count == 0)
                return;

            TileCoord playerPos = new TileCoord(gameState.Champion.GridX, gameState.Champion.GridZ);

            // Update proxy positions from behaviors.
            UpdateProxies();

            // Sort by distance to player (closest first).
            SortByDistance(playerPos);

            // Tick monsters until budget exhausted.
            Stopwatch stopwatch = Stopwatch.StartNew();
            float budgetMs = totalBudgetMs > 0 ? totalBudgetMs : DefaultBudgetMs;
            long budgetTicks = (long)(budgetMs * Stopwatch.Frequency / 1000.0);

            for (int i = 0; i < _sortCount; i++)
            {
                if (stopwatch.ElapsedTicks >= budgetTicks)
                    break;

                SortEntry entry = _sortBuffer[i];
                if (!entry.Proxy.IsActive)
                    continue;

                MonsterContext context = new MonsterContext(
                    gameState,
                    currentTick,
                    budgetMs,
                    _gridQuery
                );

                entry.Behavior.Tick(context);

                // Update proxy position after tick (monster may have moved).
                if (entry.Behavior is ScreamerBehavior screamer)
                {
                    entry.Proxy.Position = screamer.Position;
                    entry.Proxy.IsActive = screamer.IsAlive;
                }
            }

            stopwatch.Stop();
        }

        /// <summary>Sync proxy state from current behavior state.</summary>
        private void UpdateProxies()
        {
            for (int i = 0; i < _entityProxies.Count; i++)
            {
                MonsterEntityProxy proxy = _entityProxies[i];
                if (proxy.Behavior is ScreamerBehavior screamer)
                {
                    proxy.Position = screamer.Position;
                    proxy.IsActive = screamer.IsAlive;
                }
            }
        }

        /// <summary>
        /// Fill sort buffer and sort by squared distance to player (insertion sort).
        /// Efficient for small, nearly-sorted arrays.
        /// </summary>
        private void SortByDistance(TileCoord playerPos)
        {
            _sortCount = 0;

            for (int i = 0; i < _entityProxies.Count && _sortCount < MaxMonsters; i++)
            {
                MonsterEntityProxy proxy = _entityProxies[i];
                if (!proxy.IsActive)
                    continue;

                int dx = proxy.Position.X - playerPos.X;
                int dz = proxy.Position.Z - playerPos.Z;
                int distSq = dx * dx + dz * dz;

                _sortBuffer[_sortCount].Behavior = proxy.Behavior;
                _sortBuffer[_sortCount].Proxy = proxy;
                _sortBuffer[_sortCount].DistanceSq = distSq;
                _sortCount++;
            }

            // Insertion sort.
            for (int i = 1; i < _sortCount; i++)
            {
                SortEntry key = _sortBuffer[i];
                int j = i - 1;
                while (j >= 0 && _sortBuffer[j].DistanceSq > key.DistanceSq)
                {
                    _sortBuffer[j + 1] = _sortBuffer[j];
                    j--;
                }
                _sortBuffer[j + 1] = key;
            }
        }

        /// <summary>Returns the number of currently active monsters.</summary>
        public int ActiveCount => _activeBehaviors.Count;
    }
}
