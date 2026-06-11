using UnityEngine;
using UnityEditor;
using System.IO;
using DungeonVR.Art;

namespace DungeonVR.Editor
{
    /// <summary>
    /// Editor-only tool that bakes runtime-generated tile prefabs into
    /// permanent .prefab assets on disk.
    /// 
    /// Usage: Tools → Dungeon VR → Bake Tile Prefabs
    /// </summary>
    public static class TilePrefabBaker
    {
        private const string PrefabRoot = "Assets/Art/Prefabs/Tiles";
        private const string ChampionPrefabRoot = "Assets/Art/Prefabs/Champion";

        /// <summary>
        /// Menu entry to bake all tile prefabs from the TileFactory into .prefab files.
        /// </summary>
        [MenuItem("Dungeon VR/Bake Tile Prefabs")]
        public static void BakeAllTilePrefabs()
        {
            // Ensure target folders exist
            EnsureFolderExists(PrefabRoot);
            EnsureFolderExists(ChampionPrefabRoot);

            int count = 0;

            // ── Wall ──────────────────────────────────────────────────────
            count += SavePrefab(TileFactory.CreateWallPrefab(), PrefabRoot, "Wall_Tile");

            // ── Floor ─────────────────────────────────────────────────────
            count += SavePrefab(TileFactory.CreateFloorPrefab(), PrefabRoot, "Floor_Tile");

            // ── Door ──────────────────────────────────────────────────────
            count += SavePrefab(TileFactory.CreateDoorPrefab(), PrefabRoot, "Door_Tile");

            // ── Altar ─────────────────────────────────────────────────────
            count += SavePrefab(TileFactory.CreateAltarPrefab(), PrefabRoot, "Altar_Tile");

            // ── Trap Trigger ──────────────────────────────────────────────
            count += SavePrefab(TileFactory.CreateTrapTriggerPrefab(), PrefabRoot, "Trap_Trigger_Tile");

            // ── Stairs ────────────────────────────────────────────────────
            count += SavePrefab(TileFactory.CreateStairsPrefab(), PrefabRoot, "Stairs_Tile");

            // ── Champion ──────────────────────────────────────────────────
            count += SavePrefab(TileFactory.CreateChampionPrefab(), ChampionPrefabRoot, "Champion");

            // Done
            if (count > 0)
            {
                Debug.Log($"[TilePrefabBaker] Successfully baked {count} prefabs to {PrefabRoot} and {ChampionPrefabRoot}.");
                AssetDatabase.Refresh();
            }
            else
            {
                Debug.LogWarning("[TilePrefabBaker] No prefabs were created.");
            }
        }

        /// <summary>
        /// Validates that the menu item is available (always true when Editor scripts compile).
        /// </summary>
        [MenuItem("Dungeon VR/Bake Tile Prefabs", validate = true)]
        private static bool ValidateBakeAllTilePrefabs()
        {
            return true;
        }

        // ── Helpers ───────────────────────────────────────────────────────

        /// <summary>
        /// Saves a temporary GameObject as a persistent .prefab at the given path.
        /// Returns 1 on success, 0 on failure.
        /// </summary>
        private static int SavePrefab(GameObject source, string folder, string name)
        {
            if (source == null)
            {
                Debug.LogError($"[TilePrefabBaker] Source GameObject for '{name}' is null — skipping.");
                return 0;
            }

            string path = Path.Combine(folder, $"{name}.prefab").Replace("\\", "/");

            // Ensure we're working with a temporary copy (DontSave objects can't be used directly)
            GameObject copy = Object.Instantiate(source);
            copy.hideFlags = HideFlags.None;
            copy.name = name;

            // Recursively clear DontSave flags on all children
            foreach (Transform child in copy.GetComponentsInChildren<Transform>(true))
            {
                child.gameObject.hideFlags = HideFlags.None;
            }

            // Save as prefab asset
            PrefabUtility.SaveAsPrefabAsset(copy, path, out bool success);

            // Clean up the temporary copy
            Object.DestroyImmediate(copy);

            // Also clean up the original (DontSave temp)
            Object.DestroyImmediate(source);

            if (success)
            {
                Debug.Log($"[TilePrefabBaker] Created prefab: {path}");
                return 1;
            }
            else
            {
                Debug.LogError($"[TilePrefabBaker] Failed to create prefab: {path}");
                return 0;
            }
        }

        /// <summary>
        /// Ensures a folder path exists under Assets/, creating intermediate directories as needed.
        /// </summary>
        private static void EnsureFolderExists(string relativePath)
        {
            string fullPath = Path.Combine(Application.dataPath, "..", relativePath);
            fullPath = Path.GetFullPath(fullPath);

            if (!Directory.Exists(fullPath))
            {
                Directory.CreateDirectory(fullPath);
                AssetDatabase.Refresh();
                Debug.Log($"[TilePrefabBaker] Created folder: {relativePath}");
            }
        }
    }
}
