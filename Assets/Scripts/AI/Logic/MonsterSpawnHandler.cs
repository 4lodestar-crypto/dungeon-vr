using DungeonVR.AI.Data;
using DungeonVR.AI.Interfaces;
using DungeonVR.Shared.Data;
using System.Collections.Generic;
using UnityEngine;

namespace DungeonVR.AI.Logic
{
    /// <summary>
    /// Handles monster spawning: reads spawn tables, creates server-side
    /// monster entities with their AI behaviors, and registers them with
    /// the AI dispatcher.
    ///
    /// Server-authoritative: spawns are logical entities, not Unity GameObjects.
    /// The visual layer consumes entity state separately.
    ///
    /// Integration: called by MonsterSpawner after EvaluateSpawns produces
    /// SpawnRequest structs. After LevelLoadedEvent fires, the spawn pipeline
    /// activates: MonsterSpawner → MonsterSpawnHandler → MonsterAIDispatcher.
    /// </summary>
    public class MonsterSpawnHandler : IMonsterSpawnHandler
    {
        private readonly Dictionary<string, MonsterDefinition> _definitionCatalog;
        private readonly MonsterAIDispatcher _dispatcher;
        private readonly System.Random _rng;
        private int _nextEntityId;

        /// <summary>
        /// Creates the spawn handler with a definition catalog.
        /// </summary>
        /// <param name="definitionCatalog">Map of definition ID → MonsterDefinition asset.</param>
        /// <param name="dispatcher">AI dispatcher to register spawned monsters with.</param>
        /// <param name="seed">Deterministic seed for spawn RNG (entity ID generation).</param>
        public MonsterSpawnHandler(
            Dictionary<string, MonsterDefinition> definitionCatalog,
            MonsterAIDispatcher dispatcher,
            int seed)
        {
            _definitionCatalog = definitionCatalog;
            _dispatcher = dispatcher;
            _rng = new System.Random(seed);
            // Start entity IDs at 1000 to avoid collision with champion (entity 0).
            _nextEntityId = 1000;
        }

        /// <summary>
        /// Attempts to spawn a monster from the given request.
        /// Creates the server-side entity, instantiates the AI behavior, and
        /// registers it with the dispatcher.
        /// </summary>
        public bool TrySpawn(SpawnRequest request, out int entityId)
        {
            entityId = -1;

            // Look up definition.
            if (!_definitionCatalog.TryGetValue(request.MonsterDefinitionId, out MonsterDefinition definition))
            {
                Debug.LogWarning($"[MonsterSpawnHandler] Unknown monster definition: {request.MonsterDefinitionId}");
                return false;
            }

            // Assign entity ID.
            entityId = _nextEntityId++;

            // Create the behavior based on archetype.
            // V0-EXCEPTION: only Screamer implemented; other archetypes fall through.
            IMonsterBehavior behavior = CreateBehavior(definition, entityId, request.Position);

            if (behavior == null)
            {
                Debug.LogError($"[MonsterSpawnHandler] Failed to create behavior for: {definition.monsterName}");
                return false;
            }

            // Register with dispatcher for per-tick AI evaluation.
            _dispatcher.RegisterMonster(behavior, request.Position, entityId);

            return true;
        }

        /// <summary>
        /// Factory method: creates the appropriate IMonsterBehavior for a given definition.
        /// </summary>
        private IMonsterBehavior CreateBehavior(MonsterDefinition definition, int entityId, TileCoord position)
        {
            int seed = _rng.Next();

            switch (definition.archetype)
            {
                case MonsterArchetype.Aggressive:
                    // Screamer is the Aggressive archetype monster.
                    return new ScreamerBehavior(definition, entityId, position, seed);

                case MonsterArchetype.Ambush:
                case MonsterArchetype.Passive:
                case MonsterArchetype.Boss:
                default:
                    // Future monster types — not implemented in V0.
                    Debug.LogWarning($"[MonsterSpawnHandler] Archetype {definition.archetype} not implemented. Falling back to ScreamerBehavior.");
                    return new ScreamerBehavior(definition, entityId, position, seed);
            }
        }

        /// <summary>Returns the next entity ID that will be assigned.</summary>
        public int NextEntityId => _nextEntityId;
    }
}
