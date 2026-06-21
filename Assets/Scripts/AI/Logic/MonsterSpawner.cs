using System;
using System.Collections.Generic;
using DungeonVR.AI.Data;
using DungeonVR.AI.Interfaces;
using DungeonVR.Shared.Data;

namespace DungeonVR.AI.Logic
{
    /// <summary>
    /// Spawns monsters on the dungeon grid based on a MonsterSpawnTable
    /// and available spawn points.
    ///
    /// Deterministic: uses seeded System.Random for weighted selection.
    /// No UnityEngine.Random, no Physics queries.
    ///
    /// Server-authoritative: outputs SpawnRequest structs consumed by
    /// IMonsterSpawnHandler.TrySpawn(). Does not instantiate GameObjects.
    ///
    /// Integration: call EvaluateSpawns() after LevelLoadedEvent fires,
    /// then submit the resulting requests to the spawn handler.
    /// </summary>
    public class MonsterSpawner
    {
        private readonly MonsterSpawnTable _spawnTable;
        private readonly List<TileCoord> _spawnPoints;
        private readonly System.Random _rng;
        private readonly int _seed;

        /// <summary>
        /// Create a spawner bound to a specific table and set of spawn points.
        /// </summary>
        /// <param name="spawnTable">The monster spawn table to draw from.</param>
        /// <param name="spawnPoints">Available spawn positions (from GridService.SpawnPoints).</param>
        /// <param name="seed">Deterministic seed for weighted random selection.</param>
        public MonsterSpawner(MonsterSpawnTable spawnTable, IReadOnlyList<TileCoord> spawnPoints, int seed)
        {
            _spawnTable = spawnTable ?? throw new ArgumentNullException(nameof(spawnTable));
            _spawnPoints = spawnPoints != null
                ? new List<TileCoord>(spawnPoints)
                : new List<TileCoord>(0);
            _seed = seed;
            _rng = new System.Random(seed);
        }

        /// <summary>
        /// Evaluate the spawn table for a given floor depth and produce spawn requests.
        /// Each spawn point gets one monster (if the table yields one).
        /// </summary>
        /// <param name="floorDepth">Current floor depth for table filtering.</param>
        /// <param name="tickStamp">Server tick at time of spawn evaluation.</param>
        /// <returns>List of SpawnRequest structs to submit to IMonsterSpawnHandler.</returns>
        public List<SpawnRequest> EvaluateSpawns(int floorDepth, int tickStamp)
        {
            List<SpawnRequest> requests = new List<SpawnRequest>();

            // ── No spawn points → no spawns ────────────────────────────────
            if (_spawnPoints.Count == 0)
                return requests;

            // ── No spawn table → no spawns ─────────────────────────────────
            if (_spawnTable.entries == null || _spawnTable.entries.Length == 0)
                return requests;

            // ── Filter entries by floor depth ──────────────────────────────
            List<MonsterSpawnEntry> eligible = new List<MonsterSpawnEntry>();
            float totalWeight = 0f;

            foreach (MonsterSpawnEntry entry in _spawnTable.entries)
            {
                if (floorDepth >= entry.minFloor && floorDepth <= entry.maxFloor)
                {
                    if (entry.weight > 0f)
                    {
                        eligible.Add(entry);
                        totalWeight += entry.weight;
                    }
                }
            }

            // ── No eligible entries → no spawns ────────────────────────────
            if (eligible.Count == 0 || totalWeight <= 0f)
                return requests;

            // ── For each spawn point, pick a random entry ──────────────────
            foreach (TileCoord spawnPoint in _spawnPoints)
            {
                MonsterSpawnEntry chosen = WeightedRandomSelect(eligible, totalWeight);
                if (string.IsNullOrEmpty(chosen.monsterDefinitionId))
                    continue;

                SpawnRequest request = new SpawnRequest(
                    position: spawnPoint,
                    facingIndex: _rng.Next(0, 4), // Random cardinal direction.
                    monsterDefinitionId: chosen.monsterDefinitionId,
                    spawnDelayTicks: 0 // V0: instant spawn; V1+ supports staggered waves.
                );

                requests.Add(request);
            }

            return requests;
        }

        /// <summary>
        /// Weighted random selection from a list of entries.
        /// Uses the seeded System.Random instance — deterministic given seed + call order.
        /// </summary>
        private MonsterSpawnEntry WeightedRandomSelect(List<MonsterSpawnEntry> entries, float totalWeight)
        {
            double roll = _rng.NextDouble() * totalWeight;
            double cumulative = 0.0;

            foreach (MonsterSpawnEntry entry in entries)
            {
                cumulative += entry.weight;
                if (roll <= cumulative)
                    return entry;
            }

            // Fallback (floating-point edge): return last entry.
            return entries[entries.Count - 1];
        }

        /// <summary>The seed used for deterministic RNG.</summary>
        public int Seed => _seed;

        /// <summary>Number of spawn points available.</summary>
        public int SpawnPointCount => _spawnPoints.Count;
    }
}
