using DungeonVR.Level.Components;
using DungeonVR.Level.Data;
using DungeonVR.Level.Logic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Editor utility to set up the Phase 3 procedural test scene.
/// Access via Tools > DungeonVR > Setup Procedural Test Scene
/// </summary>
public static class SetupProceduralTestScene
{
    [MenuItem("Tools/DungeonVR/Setup Procedural Test Scene")]
    public static void Setup()
    {
        // Create new scene
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = "ProceduralTest";

        // --- LevelLoader GameObject ---
        GameObject loaderGO = new GameObject("LevelLoader");
        LevelLoader loader = loaderGO.AddComponent<LevelLoader>();

        // Create child "Tiles" for _tileRoot
        GameObject tilesGO = new GameObject("Tiles");
        tilesGO.transform.SetParent(loaderGO.transform);

        // Assign TilePalette asset
        string palettePath = "Assets/Data/Levels/TilePalette.asset";
        TilePalette palette = AssetDatabase.LoadAssetAtPath<TilePalette>(palettePath);
        if (palette == null)
        {
            Debug.LogError($"[Setup] TilePalette not found at {palettePath}");
            return;
        }

        // Use SerializedObject to set private serialized fields
        SerializedObject so = new SerializedObject(loader);
        so.FindProperty("_palette").objectReferenceValue = palette;
        so.FindProperty("_tileRoot").objectReferenceValue = tilesGO.transform;
        so.ApplyModifiedProperties();

        // Create a GridService on the same GO (or it will be auto-added)
        loaderGO.AddComponent<GridService>();

        // --- ProceduralGenStarter GameObject ---
        GameObject starterGO = new GameObject("ProceduralGenStarter");
        ProceduralTestStarter starter = starterGO.AddComponent<ProceduralTestStarter>();
        starter.levelLoader = loader;

        // Save scene
        string scenePath = "Assets/Scenes/ProceduralTest.unity";
        System.IO.Directory.CreateDirectory("Assets/Scenes");
        EditorSceneManager.SaveScene(scene, scenePath);
        AssetDatabase.Refresh();

        Debug.Log($"[Setup] ProceduralTest scene created and saved to {scenePath}");

        // Enter Play Mode
        EditorApplication.isPlaying = true;
    }
}
