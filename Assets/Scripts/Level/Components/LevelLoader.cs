using System;
using System.Collections.Generic;
using UnityEngine;
using DungeonVR.Shared;
using DungeonVR.Shared.Data;
using DungeonVR.Shared.Enums;
using DungeonVR.Level.Data;
using DungeonVR.Level.Interfaces;
using DungeonVR.Level.Logic;

namespace DungeonVR.Level.Components
{
    /// <summary>
    /// MonoBehaviour that loads a level from a JSON TextAsset,
    /// validates it, instantiates tiles via an ITilePalette, and
    /// registers the grid with a GridService for runtime queries.
    /// </summary>
    public class LevelLoader : MonoBehaviour, ILevelLoader
    {
        [Header("Level Data")]
        [SerializeField, Tooltip("JSON file containing the level definition (e.g. Floor01.json).")]
        private TextAsset _levelDataAsset;

        [SerializeField, Tooltip("Tile Palette ScriptableObject mapping TileTypes to prefabs.")]
        private TilePalette _palette;

        [SerializeField, Tooltip("Parent transform under which instantiated tiles will be placed.")]
        private Transform _tileRoot;

        [Header("Runtime")]
        [SerializeField, Tooltip("(Optional) GridService reference. If null, one will be created.")]
        private GridService _gridService;

        /// <summary>
        /// Fired when level loading completes, whether successful or not.
        /// Receives true on success, false on failure.
        /// </summary>
        public event Action LevelLoaded;

        /// <summary>
        /// The runtime GridService for tile queries after loading.
        /// </summary>
        public GridService GridService => _gridService;

        /// <summary>
        /// Loads a level from a TextAsset (JSON format, schema version 1).
        /// Deserializes, validates, instantiates tiles, and fires LevelLoaded.
        /// </summary>
        public bool LoadFromAsset(TextAsset levelData, ITilePalette palette, Transform tileRoot)
        {
            if (levelData == null)
            {
                Debug.LogError("[LevelLoader] Level data asset is null.");
                LevelLoaded?.Invoke();
                return false;
            }

            if (palette == null)
            {
                Debug.LogError("[LevelLoader] Tile palette is null.");
                LevelLoaded?.Invoke();
                return false;
            }

            // Deserialize JSON
            LevelDataJson levelJson;
            try
            {
                levelJson = JsonUtility.FromJson<LevelDataJson>(levelData.text);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[LevelLoader] Failed to deserialize JSON: {ex.Message}");
                LevelLoaded?.Invoke();
                return false;
            }

            if (levelJson == null)
            {
                Debug.LogError("[LevelLoader] Deserialized JSON is null.");
                LevelLoaded?.Invoke();
                return false;
            }

            // Validate schema version
            if (levelJson.schemaVersion != 1)
            {
                Debug.LogError($"[LevelLoader] Unsupported schema version: {levelJson.schemaVersion}. Expected: 1.");
                LevelLoaded?.Invoke();
                return false;
            }

            // Validate dimensions
            if (levelJson.width <= 0 || levelJson.depth <= 0)
            {
                Debug.LogError($"[LevelLoader] Invalid grid dimensions: {levelJson.width}x{levelJson.depth}.");
                LevelLoaded?.Invoke();
                return false;
            }

            if (levelJson.tiles == null)
            {
                levelJson.tiles = new TileDataJson[0];
            }

            // Apply default tile type
            TileType defaultType = TileType.Floor;
            if (levelJson.defaultTile != null)
            {
                if (!TryParseTileType(levelJson.defaultTile.type, out defaultType))
                {
                    Debug.LogWarning($"[LevelLoader] Unknown default tile type '{levelJson.defaultTile.type}'. Using Floor.");
                    defaultType = TileType.Floor;
                }
            }

            // Build TileData array filling defaults
            TileData[] tiles = BuildTileData(levelJson, defaultType, out string[] errors);

            if (errors.Length > 0)
            {
                foreach (string err in errors)
                {
                    Debug.LogWarning($"[LevelLoader] {err}");
                }
            }

            // Now load from data
            return LoadFromData(tiles, levelJson.width, levelJson.depth, palette, tileRoot);
        }

        /// <summary>
        /// Load a level from a raw TileData array. Used by procedural generators.
        /// Validates, instantiates tiles, registers grid, fires LevelLoaded.
        /// </summary>
        public bool LoadFromData(TileData[] tiles, int width, int depth, ITilePalette palette, Transform tileRoot)
        {
            if (tiles == null || tiles.Length == 0)
            {
                Debug.LogError("[LevelLoader] TileData array is null or empty.");
                LevelLoaded?.Invoke();
                return false;
            }

            if (palette == null)
            {
                Debug.LogError("[LevelLoader] Tile palette is null.");
                LevelLoaded?.Invoke();
                return false;
            }

            if (tileRoot == null)
            {
                Debug.LogError("[LevelLoader] Tile root transform is null.");
                LevelLoaded?.Invoke();
                return false;
            }

            // Validate
            ILevelValidator validator = new LevelValidator();
            if (!validator.Validate(tiles, width, depth, palette, out string[] validationErrors))
            {
                Debug.LogError("[LevelLoader] Level validation failed:");
                foreach (string err in validationErrors)
                {
                    Debug.LogError($"  - {err}");
                }
                LevelLoaded?.Invoke();
                return false;
            }

            // Ensure GridService exists
            if (_gridService == null)
            {
                _gridService = GetComponent<GridService>();
                if (_gridService == null)
                {
                    _gridService = gameObject.AddComponent<GridService>();
                }
            }

            // Instantiate tiles
            InstantiateTiles(tiles, width, depth, palette, tileRoot);

            // Register grid with GridService
            _gridService.RegisterFromTiles(tiles, width, depth);

            Debug.Log($"[LevelLoader] Level loaded: {tiles.Length} tiles, {width}x{depth} grid.");

            LevelLoaded?.Invoke();
            return true;
        }

        /// <summary>
        /// Parses a TileType from its string name (case-insensitive).
        /// </summary>
        private static bool TryParseTileType(string typeStr, out TileType result)
        {
            if (string.IsNullOrEmpty(typeStr))
            {
                result = TileType.Floor;
                return false;
            }

            // Capitalize first letter for proper enum parsing
            string normalized = char.ToUpperInvariant(typeStr[0]) + typeStr.Substring(1).ToLowerInvariant();
            return Enum.TryParse(normalized, out result);
        }

        /// <summary>
        /// Builds a complete TileData array from the JSON structure,
        /// filling default tiles for any coordinates not explicitly listed.
        /// </summary>
        private static TileData[] BuildTileData(LevelDataJson levelJson, TileType defaultType, out string[] warnings)
        {
            List<string> warnList = new List<string>();
            Dictionary<(int, int), TileData> tileMap = new Dictionary<(int, int), TileData>();

            // Add all explicit tiles from JSON
            foreach (TileDataJson tileJson in levelJson.tiles)
            {
                TileType type;
                if (!TryParseTileType(tileJson.type, out type))
                {
                    warnList.Add($"Unknown tile type '{tileJson.type}' at ({tileJson.x},{tileJson.z}): using default.");
                    type = defaultType;
                }

                WallFace wallFaces = WallFace.None;
                if (!string.IsNullOrEmpty(tileJson.wallFaces))
                {
                    if (!TryParseWallFace(tileJson.wallFaces, out wallFaces))
                    {
                        warnList.Add($"Unknown wallFace value '{tileJson.wallFaces}' at ({tileJson.x},{tileJson.z}): using None.");
                    }
                }

                var key = (tileJson.x, tileJson.z);
                if (tileMap.ContainsKey(key))
                {
                    warnList.Add($"Duplicate tile at ({tileJson.x},{tileJson.z}): overwriting.");
                }

                tileMap[key] = new TileData(tileJson.x, tileJson.z, type, wallFaces);
            }

            // Fill defaults for any missing coordinates within the grid
            for (int x = 0; x < levelJson.width; x++)
            {
                for (int z = 0; z < levelJson.depth; z++)
                {
                    var key = (x, z);
                    if (!tileMap.ContainsKey(key))
                    {
                        tileMap[key] = new TileData(x, z, defaultType);
                    }
                }
            }

            warnings = warnList.ToArray();

            // Convert dictionary to array
            TileData[] result = new TileData[tileMap.Count];
            int i = 0;
            foreach (var kvp in tileMap)
            {
                result[i++] = kvp.Value;
            }

            return result;
        }

        /// <summary>
        /// Parses a WallFace value from its string name (case-insensitive).
        /// Supports single values and comma-separated combinations like "north,east".
        /// </summary>
        private static bool TryParseWallFace(string faceStr, out WallFace result)
        {
            result = WallFace.None;

            if (string.IsNullOrEmpty(faceStr))
                return false;

            // Check for "all" first
            if (faceStr.Trim().ToLowerInvariant() == "all")
            {
                result = WallFace.All;
                return true;
            }

            // Try parsing as a single value
            string normalized = char.ToUpperInvariant(faceStr[0]) + faceStr.Substring(1).ToLowerInvariant();
            if (Enum.TryParse(normalized, out result))
                return true;

            // Try comma-separated flags
            string[] parts = faceStr.Split(',');
            WallFace combined = WallFace.None;
            foreach (string part in parts)
            {
                string trimmed = part.Trim();
                if (string.IsNullOrEmpty(trimmed))
                    continue;

                string partNorm = char.ToUpperInvariant(trimmed[0]) + trimmed.Substring(1).ToLowerInvariant();
                if (Enum.TryParse(partNorm, out WallFace single))
                {
                    combined |= single;
                }
                else
                {
                    return false;
                }
            }

            result = combined;
            return true;
        }

        /// <summary>
        /// Instantiates all tiles from TileData array, positioning them in world space.
        /// Uses TILE_SIZE as the grid pitch.
        /// </summary>
        private void InstantiateTiles(TileData[] tiles, int width, int depth, ITilePalette palette, Transform root)
        {
            // Clear existing tiles first
            ClearTileRoot(root);

            foreach (TileData tile in tiles)
            {
                if (tile.Type == TileType.Empty)
                    continue; // Don't instantiate void tiles

                GameObject prefab = palette.GetPrefab(tile.Type);
                if (prefab == null)
                {
                    Debug.LogWarning($"[LevelLoader] No prefab for tile type '{tile.Type}' at ({tile.X},{tile.Z}). Skipping.");
                    continue;
                }

                // Calculate world position: center-bottom of tile
                Vector3 position = new Vector3(
                    tile.X * GameConstants.TILE_SIZE + GameConstants.TILE_SIZE * 0.5f,
                    tile.FloorHeight,
                    tile.Z * GameConstants.TILE_SIZE + GameConstants.TILE_SIZE * 0.5f
                );

                // Determine rotation based on wall faces
                Quaternion rotation = GetRotationForWallFaces(tile.WallFaces);

                GameObject instance = Instantiate(prefab, position, rotation, root);
                instance.name = $"{tile.Type}_{tile.X}_{tile.Z}";

                // Tag the tile for debugging and editor tools
                TileInstanceMarker marker = instance.GetComponent<TileInstanceMarker>();
                if (marker == null)
                {
                    marker = instance.AddComponent<TileInstanceMarker>();
                }
                marker.Initialize(tile);
            }

            Debug.Log($"[LevelLoader] Instantiated {tiles.Length} tiles under '{root.name}'.");
        }

        /// <summary>
        /// Determines the rotation for wall face geometry.
        /// Currently handles single-face and all-face cases.
        /// </summary>
        private static Quaternion GetRotationForWallFaces(WallFace faces)
        {
            // Default: no rotation
            if (faces == WallFace.None)
                return Quaternion.identity;

            // All faces: no rotation needed (full enclosure)
            if (faces == WallFace.All)
                return Quaternion.identity;

            // Single face: rotate to align
            if (faces == WallFace.North)
                return Quaternion.identity;
            if (faces == WallFace.South)
                return Quaternion.Euler(0, 180, 0);
            if (faces == WallFace.East)
                return Quaternion.Euler(0, 90, 0);
            if (faces == WallFace.West)
                return Quaternion.Euler(0, 270, 0);

            // Multi-face: default identity; art pipeline handles multi-face prefabs
            return Quaternion.identity;
        }

        /// <summary>
        /// Destroys all child objects under the tile root.
        /// </summary>
        private static void ClearTileRoot(Transform root)
        {
            if (root == null)
                return;

            // Destroy children in reverse order
            for (int i = root.childCount - 1; i >= 0; i--)
            {
#if UNITY_EDITOR
                if (Application.isPlaying)
                    Destroy(root.GetChild(i).gameObject);
                else
                    DestroyImmediate(root.GetChild(i).gameObject);
#else
                Destroy(root.GetChild(i).gameObject);
#endif
            }
        }

        /// <summary>
        /// Editor/Startup convenience: loads the assigned asset on Awake.
        /// </summary>
        private void Awake()
        {
            if (_gridService == null)
            {
                _gridService = GetComponent<GridService>();
                if (_gridService == null)
                {
                    _gridService = gameObject.AddComponent<GridService>();
                }
            }
        }

        private void Start()
        {
            if (_levelDataAsset != null && _palette != null)
            {
                Transform root = _tileRoot != null ? _tileRoot : transform;
                LoadFromAsset(_levelDataAsset, _palette, root);
            }
        }

        // ------------------------------------------------------------------
        //  Serialization classes for JSON (schema version 1)
        // ------------------------------------------------------------------

        [Serializable]
        private class LevelDataJson
        {
            public int schemaVersion = 1;
            public string levelName;
            public int width;
            public int depth;
            public TileDataJson[] tiles;
            public DefaultTileJson defaultTile;
        }

        [Serializable]
        private class TileDataJson
        {
            public int x;
            public int z;
            public string type = "floor";
            public string wallFaces;
            public float floorHeight;
        }

        [Serializable]
        private class DefaultTileJson
        {
            public string type = "floor";
        }
    }
}
