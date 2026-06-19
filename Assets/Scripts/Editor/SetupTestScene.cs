using UnityEngine;
using UnityEditor;
using DungeonVR.Level.Components;
using DungeonVR.Level.Data;

public static class SetupTestScene
{
    [MenuItem("Dungeon VR/Setup Test Scene")]
    public static void Setup()
    {
        // Create LevelLoader
        GameObject levelLoaderObj = new GameObject("LevelLoader");
        LevelLoader loader = levelLoaderObj.AddComponent<LevelLoader>();

        // Load the TilePalette asset
        TilePalette palette = AssetDatabase.LoadAssetAtPath<TilePalette>("Assets/Data/Levels/TilePalette.asset");
        if (palette == null)
        {
            Debug.LogError("TilePalette not found at Assets/Data/Levels/TilePalette.asset");
            return;
        }

        // Assign palette via serialized properties
        SerializedObject so = new SerializedObject(loader);
        so.FindProperty("_palette").objectReferenceValue = palette;
        so.ApplyModifiedProperties();

        // Create child Tiles
        GameObject tilesObj = new GameObject("Tiles");
        tilesObj.transform.SetParent(levelLoaderObj.transform);

        // Assign tile root via serialized properties
        so.Update();
        so.FindProperty("_tileRoot").objectReferenceValue = tilesObj.transform;
        so.ApplyModifiedProperties();

        // Create ProceduralGenStarter
        GameObject starterObj = new GameObject("ProceduralGenStarter");
        ProceduralTestStarter starter = starterObj.AddComponent<ProceduralTestStarter>();
        starter.levelLoader = loader;

        // Select the LevelLoader in the hierarchy
        Selection.activeGameObject = levelLoaderObj;

        Debug.Log("[SetupTestScene] Test scene setup complete!");
    }
}
