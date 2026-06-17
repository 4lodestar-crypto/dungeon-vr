using UnityEngine;
using UnityEditor;
using DungeonVR.Level.Data;
using System.Reflection;

public static class GenerateTilePalette
{
    [MenuItem("Dungeon VR/Generate Tile Palette")]
    public static void Generate()
    {
        string folderPath = "Assets/Data/Levels";
        if (!AssetDatabase.IsValidFolder("Assets/Data"))
            AssetDatabase.CreateFolder("Assets", "Data");
        if (!AssetDatabase.IsValidFolder(folderPath))
            AssetDatabase.CreateFolder("Assets/Data", "Levels");

        string assetPath = $"{folderPath}/TilePalette.asset";
        TilePalette existing = AssetDatabase.LoadAssetAtPath<TilePalette>(assetPath);
        if (existing != null)
            AssetDatabase.DeleteAsset(assetPath);

        TilePalette palette = ScriptableObject.CreateInstance<TilePalette>();
        string tilePrefabRoot = "Assets/Art/Prefabs/Tiles";

        SetPrivateField(palette, "_floorPrefab", LoadPrefab(tilePrefabRoot, "Floor_Tile"));
        SetPrivateField(palette, "_wallPrefab", LoadPrefab(tilePrefabRoot, "Wall_Tile"));
        SetPrivateField(palette, "_doorPrefab", LoadPrefab(tilePrefabRoot, "Door_Tile"));
        SetPrivateField(palette, "_trapPrefab", LoadPrefab(tilePrefabRoot, "Trap_Trigger_Tile"));
        SetPrivateField(palette, "_altarPrefab", LoadPrefab(tilePrefabRoot, "Altar_Tile"));
        SetPrivateField(palette, "_spawnPrefab", LoadPrefab(tilePrefabRoot, "Floor_Tile"));
        SetPrivateField(palette, "_stairsPrefab", LoadPrefab(tilePrefabRoot, "Stairs_Tile"));

        AssetDatabase.CreateAsset(palette, assetPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        TilePalette loaded = AssetDatabase.LoadAssetAtPath<TilePalette>(assetPath);
        bool complete = loaded != null && loaded.IsComplete;
        Debug.Log($"[GenerateTilePalette] Created TilePalette at {assetPath}. IsComplete: {complete}");
        EditorGUIUtility.PingObject(loaded);
    }

    private static GameObject LoadPrefab(string root, string name)
    {
        string path = $"{root}/{name}.prefab";
        GameObject go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (go == null) Debug.LogError($"[GenerateTilePalette] Failed to load prefab: {path}");
        return go;
    }

    private static void SetPrivateField(object obj, string fieldName, object value)
    {
        var field = obj.GetType().GetField(fieldName,
            BindingFlags.NonPublic | BindingFlags.Instance);
        if (field != null) field.SetValue(obj, value);
        else Debug.LogError($"[GenerateTilePalette] Field '{fieldName}' not found");
    }
}
