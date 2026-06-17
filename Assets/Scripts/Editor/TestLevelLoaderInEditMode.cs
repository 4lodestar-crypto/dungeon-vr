using UnityEngine;
using UnityEditor;
using DungeonVR.Level.Components;
using DungeonVR.Level.Data;

public static class TestLevelLoaderInEditMode
{
    [MenuItem("Dungeon VR/Test Level Loader")]
    public static void Test()
    {
        LevelLoader loader = Object.FindAnyObjectByType<LevelLoader>();
        if (loader == null)
        {
            Debug.LogError("[TestLevelLoader] No LevelLoader found.");
            return;
        }

        string palettePath = "Assets/Data/Levels/TilePalette.asset";
        TilePalette palette = AssetDatabase.LoadAssetAtPath<TilePalette>(palettePath);
        string dataPath = "Assets/Data/Floor01.json";
        TextAsset levelData = AssetDatabase.LoadAssetAtPath<TextAsset>(dataPath);

        if (palette == null)
        {
            Debug.LogError($"[TestLevelLoader] TilePalette not found at {palettePath}");
            return;
        }
        if (levelData == null)
        {
            Debug.LogError($"[TestLevelLoader] Floor01.json not found at {dataPath}");
            return;
        }

        Debug.Log($"[TestLevelLoader] Palette.IsComplete: {palette.IsComplete}");
        foreach (TileType t in System.Enum.GetValues(typeof(TileType)))
        {
            if (t == TileType.Empty) continue;
            GameObject p = palette.GetPrefab(t);
            Debug.Log($"  {t}: {(p != null ? p.name + " (" + p.GetInstanceID() + ")" : "NULL")}");
        }

        // Manually call LoadFromAsset in edit mode
        bool result = loader.LoadFromAsset(levelData, palette, loader.transform);
        Debug.Log($"[TestLevelLoader] LoadFromAsset result: {result}");
        EditorGUIUtility.PingObject(loader);
    }
}
