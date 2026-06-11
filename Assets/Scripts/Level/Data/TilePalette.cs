using UnityEngine;
using DungeonVR.Shared.Enums;
using DungeonVR.Level.Interfaces;

namespace DungeonVR.Level.Data
{
    /// <summary>
    /// ScriptableObject that maps each TileType value to a Unity prefab.
    /// Create via Assets → Create → DungeonVR → Tile Palette.
    /// </summary>
    [CreateAssetMenu(menuName = "DungeonVR/Tile Palette")]
    public class TilePalette : ScriptableObject, ITilePalette
    {
        [Header("Tile Prefab Assignments")]
        [SerializeField, Tooltip("Prefab for Floor tiles. Walkable ground.")]
        private GameObject _floorPrefab;

        [SerializeField, Tooltip("Prefab for Wall tiles. Impassable obstacle.")]
        private GameObject _wallPrefab;

        [SerializeField, Tooltip("Prefab for Door tiles. Walkable when open.")]
        private GameObject _doorPrefab;

        [SerializeField, Tooltip("Prefab for Trap tiles. Triggers effect on step.")]
        private GameObject _trapPrefab;

        [SerializeField, Tooltip("Prefab for Altar tiles. Interaction point.")]
        private GameObject _altarPrefab;

        [SerializeField, Tooltip("Prefab for Spawn tiles. Player spawn point.")]
        private GameObject _spawnPrefab;

        [SerializeField, Tooltip("Prefab for Stairs tiles. Floor transition.")]
        private GameObject _stairsPrefab;

        /// <summary>
        /// Internal lookup array indexed by TileType int value.
        /// Built once when the asset is loaded or refreshed.
        /// </summary>
        private GameObject[] _prefabsByType;

        private void OnEnable()
        {
            BuildLookup();
        }

        private void OnValidate()
        {
            BuildLookup();
        }

        private void BuildLookup()
        {
            _prefabsByType = new GameObject[System.Enum.GetValues(typeof(TileType)).Length];
            _prefabsByType[(int)TileType.Floor] = _floorPrefab;
            _prefabsByType[(int)TileType.Wall] = _wallPrefab;
            _prefabsByType[(int)TileType.Door] = _doorPrefab;
            _prefabsByType[(int)TileType.Trap] = _trapPrefab;
            _prefabsByType[(int)TileType.Altar] = _altarPrefab;
            _prefabsByType[(int)TileType.Spawn] = _spawnPrefab;
            _prefabsByType[(int)TileType.Stairs] = _stairsPrefab;
            // Empty has no prefab — it's a void tile.
        }

        /// <summary>
        /// Returns the prefab GameObject for the given TileType.
        /// Logs a warning if the prefab is null (e.g. Empty type or unassigned slot).
        /// </summary>
        public GameObject GetPrefab(TileType type)
        {
            if (_prefabsByType == null)
                BuildLookup();

            int index = (int)type;

            if (index < 0 || index >= _prefabsByType.Length)
            {
                Debug.LogWarning($"[TilePalette] TileType '{type}' has no index in lookup array.");
                return null;
            }

            GameObject prefab = _prefabsByType[index];

            if (prefab == null && type != TileType.Empty)
            {
                Debug.LogWarning($"[TilePalette] No prefab assigned for TileType '{type}'. " +
                    $"Assign it in the Tile Palette inspector.");
            }

            return prefab;
        }

        /// <summary>
        /// Returns true if all non-Empty TileType values have a prefab assigned.
        /// </summary>
        public bool IsComplete
        {
            get
            {
                if (_prefabsByType == null)
                    BuildLookup();

                foreach (TileType type in System.Enum.GetValues(typeof(TileType)))
                {
                    if (type == TileType.Empty)
                        continue;

                    int index = (int)type;
                    if (index < 0 || index >= _prefabsByType.Length)
                        return false;
                    if (_prefabsByType[index] == null)
                        return false;
                }

                return true;
            }
        }
    }
}
