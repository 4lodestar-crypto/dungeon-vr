using DungeonVR.AI.Interfaces;
using UnityEngine;

namespace DungeonVR.AI.Data
{
    /// <summary>
    /// Categorisation of monster temperament. Drives default state transitions
    /// and the general shape of AI behaviour without referencing concrete classes.
    /// </summary>
    public enum MonsterArchetype
    {
        /// <summary>Actively seeks and pursues the player on detection.</summary>
        Aggressive,

        /// <summary>Ignores the player until provoked or attacked.</summary>
        Passive,

        /// <summary>Remains hidden (Idle/Patrol) until a proximity trigger fires, then attacks.</summary>
        Ambush,

        /// <summary>Special large-scale encounter unit with unique phase transitions.</summary>
        Boss
    }

    /// <summary>
    /// Authoring asset that defines the baseline stats, appearance, and cue
    /// specifications for a single monster type. Intended to be referenced by
    /// spawn tables and looked up by the AI spawner at runtime.
    ///
    /// All numeric values are in server-tick units except ranges (which are in
    /// tile units) so that everything is deterministic and frame-rate independent.
    /// </summary>
    [CreateAssetMenu(menuName = "DungeonVR/Monster Definition")]
    public class MonsterDefinition : ScriptableObject
    {
        [Header("Identity")]

        /// <summary>Human-readable name shown in debug overlays and tooltips.</summary>
        [Tooltip("Human-readable name shown in debug overlays and tooltips.")]
        public string monsterName;

        /// <summary>Categorisation that drives default AI transition weights.</summary>
        [Tooltip("Categorisation that drives default AI transition weights.")]
        public MonsterArchetype archetype;

        [Header("Combat Stats")]

        /// <summary>Maximum hit points. Entity starts at this value on spawn.</summary>
        [Tooltip("Maximum hit points. Entity starts at this value on spawn.")]
        public int maxHP;

        /// <summary>Tiles per server tick the monster can move along a path.</summary>
        [Tooltip("Tiles per server tick the monster can move along a path. Fractional values interpolate.")]
        public float moveSpeedTilesPerTick;

        /// <summary>Maximum manhattan (or path) distance in tiles the monster can detect the player.</summary>
        [Tooltip("Maximum detection range in tiles. Used by the perception system — not a Physics/Raycast query.")]
        public float detectionRangeTiles;

        /// <summary>Raw damage applied on a successful attack. Actual damage may be modified by equipment/abilities.</summary>
        [Tooltip("Raw damage applied on a successful attack. May be modified by equipment/abilities.")]
        public int damagePerHit;

        /// <summary>Number of ticks the monster must wait between consecutive attacks.</summary>
        [Tooltip("Number of ticks the monster must wait between consecutive attacks.")]
        public int attackCooldownTicks;

        [Header("AI Defaults")]

        /// <summary>State the monster spawns in. Typically Idle or Patrol.</summary>
        [Tooltip("State the monster spawns in. Typically Idle or Patrol.")]
        public MonsterStateId initialState;

        [Header("Presentation")]

        /// <summary>Optional reference to the visual prefab (used by the Art/VR layer, not by AI logic).</summary>
        [Tooltip("Optional reference to the visual prefab (used by the Art/VR layer, not by AI logic).")]
        public GameObject prefab;

        // ──────────────────────────────────────────────────────
        // Cue specifications (V1+ detail)
        // ──────────────────────────────────────────────────────

        [Header("Cues (V1+ Detail)")]

        /// <summary>Specification key for the visual/haptic cue played when the monster spawns.</summary>
        [Tooltip("Specification key for the visual/haptic cue played on spawn.")]
        public string spawnCueKey;

        /// <summary>Specification key for the cue played when the monster detects the player and enters Alert.</summary>
        [Tooltip("Specification key for the cue played on player detection (Alert transition).")]
        public string detectionCueKey;

        /// <summary>Specification key for the cue played when the monster attacks.</summary>
        [Tooltip("Specification key for the cue played on attack.")]
        public string attackCueKey;

        /// <summary>Specification key for the cue played when the monster takes damage.</summary>
        [Tooltip("Specification key for the cue played on taking damage.")]
        public string hurtCueKey;

        /// <summary>Specification key for the cue played on death.</summary>
        [Tooltip("Specification key for the cue played on death.")]
        public string deathCueKey;
    }
}
