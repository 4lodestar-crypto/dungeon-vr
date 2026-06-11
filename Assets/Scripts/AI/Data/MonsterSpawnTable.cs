using System;
using UnityEngine;

namespace DungeonVR.AI.Data
{
    /// <summary>
    /// A single row in a spawn table. Defines which monster can appear,
    /// how likely it is compared to its peers, and which floors it belongs to.
    /// </summary>
    [Serializable]
    public struct MonsterSpawnEntry
    {
        /// <summary>
        /// Logical group identifier. All entries sharing the same spawnGroupId
        /// on the same floor compete via weighted random selection.
        /// </summary>
        [Tooltip("Logical group identifier. Entries sharing the same group on the same floor compete via weighted selection.")]
        public string spawnGroupId;

        /// <summary>
        /// Lookup key into the <see cref="MonsterDefinition"/> catalog.
        /// Must match a definition asset's name or a dedicated lookup ID.
        /// </summary>
        [Tooltip("Lookup key into the MonsterDefinition catalog.")]
        public string monsterDefinitionId;

        /// <summary>
        /// Relative weight for weighted random selection within the spawn group.
        /// Higher values increase the chance this entry is chosen.
        /// </summary>
        [Tooltip("Relative weight for weighted random selection within the spawn group. Higher = more likely.")]
        public float weight;

        /// <summary>
        /// Minimum floor depth (inclusive) where this entry is eligible to spawn.
        /// </summary>
        [Tooltip("Minimum floor depth (inclusive).")]
        public int minFloor;

        /// <summary>
        /// Maximum floor depth (inclusive) where this entry is eligible to spawn.
        /// </summary>
        [Tooltip("Maximum floor depth (inclusive).")]
        public int maxFloor;
    }

    /// <summary>
    /// Authoring asset that defines the monster roster for a floor, wing, or encounter.
    /// The spawn system reads this table and combines it with <see cref="ISpawnPointProvider"/>
    /// data to produce <see cref="SpawnRequest"/> instances.
    ///
    /// All probability resolution is deterministic using a seeded server RNG —
    /// no UnityEngine.Random calls at any point.
    /// </summary>
    [CreateAssetMenu(menuName = "DungeonVR/Monster Spawn Table")]
    public class MonsterSpawnTable : ScriptableObject
    {
        /// <summary>
        /// Array of spawn entries. Filtered by current floor depth at runtime
        /// before weighted selection.
        /// </summary>
        [Tooltip("Spawn entries for this table. Filtered by floor depth at runtime before weighted selection.")]
        public MonsterSpawnEntry[] entries;

        /// <summary>
        /// Optional seed override. If non-zero, used for deterministic spawn
        /// selection. If zero, the server tick seed at the moment of request is used.
        /// </summary>
        [Tooltip("Optional deterministic seed override. Zero = use server tick seed.")]
        public int seedOverride;
    }
}
