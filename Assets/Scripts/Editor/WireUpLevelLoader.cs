using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using DungeonVR.Level.Components;
using DungeonVR.Level.Data;

public static class WireUpLevelLoader
{
    [MenuItem("Dungeon VR/Wire Up Level Loader")]
    public static void WireUp()
    {
        // Find LevelLoader in the current scene
        LevelLoader loader = Object.FindFirstObjectByType<LevelLoader>();
        if (loader == null)
        {
            Debug.LogError("[WireUp] No LevelLoader found in the current scene.");
            return;
        }

        // Load TilePalette asset
        string palettePath = "Assets/Data/Levels/TilePalette.asset";
        TilePalette palette = AssetDatabase.LoadAssetAtPath<TilePalette>(palettePath);
        if (palette == null)
        {
            Debug.LogError($"[WireUp] TilePalette not found at {palettePath}");
            return;
        }

        // Load Floor01.json
        string dataPath = "Assets/Data/Floor01.json";
        TextAsset levelData = AssetDatabase.LoadAssetAtPath<TextAsset>(dataPath);
        if (levelData == null)
        {
            Debug.LogError($"[WireUp] Floor01.json not found at {dataPath}");
            return;
        }

        // Assign via SerializedObject
        SerializedObject so = new SerializedObject(loader);
        SerializedProperty propPalette = so.FindProperty("_palette");
        SerializedProperty propLevelData = so.FindProperty("_levelDataAsset");
        SerializedProperty propTileRoot = so.FindProperty("_tileRoot");

        if (propPalette != null) propPalette.objectReferenceValue = palette;
        if (propLevelData != null) propLevelData.objectReferenceValue = levelData;
        if (propTileRoot != null) propTileRoot.objectReferenceValue = loader.transform;

        so.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.MarkSceneDirty(loader.gameObject.scene);
        EditorSceneManager.SaveScene(loader.gameObject.scene);

        Debug.Log($"[WireUp] LevelLoader wired: Palette={(palette != null ? "OK" : "MISSING")}, LevelData={(levelData != null ? "OK" : "MISSING")}");
        EditorGUIUtility.PingObject(loader);
    }
}
