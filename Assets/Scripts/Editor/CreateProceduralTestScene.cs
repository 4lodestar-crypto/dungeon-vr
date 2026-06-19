using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using DungeonVR.Level.Components;
using DungeonVR.Level.Data;

public static class CreateProceduralTestScene
{
    [MenuItem("Dungeon VR/Create Procedural Test Scene")]
    public static void Create()
    {
        // Create new blank scene
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // Ensure Scenes folder exists
        if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
            AssetDatabase.CreateFolder("Assets", "Scenes");

        // --- LevelLoader GameObject ---
        GameObject loaderObj = new GameObject("LevelLoader");
        LevelLoader loader = loaderObj.AddComponent<LevelLoader>();

        // Load TilePalette asset
        TilePalette palette = AssetDatabase.LoadAssetAtPath<TilePalette>("Assets/Data/Levels/TilePalette.asset");
        if (palette == null)
        {
            Debug.LogError("[CreateScene] TilePalette.asset not found!");
            return;
        }

        // Assign _palette via SerializedObject
        SerializedObject so = new SerializedObject(loader);
        SerializedProperty propPalette = so.FindProperty("_palette");
        if (propPalette != null)
        {
            propPalette.objectReferenceValue = palette;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // Create Tiles child
        GameObject tilesObj = new GameObject("Tiles");
        tilesObj.transform.SetParent(loaderObj.transform);

        // Assign _tileRoot via SerializedObject
        SerializedProperty propTileRoot = so.FindProperty("_tileRoot");
        if (propTileRoot != null)
        {
            propTileRoot.objectReferenceValue = tilesObj.transform;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // --- ProceduralGenStarter GameObject ---
        GameObject starterObj = new GameObject("ProceduralGenStarter");
        ProceduralTestStarter starter = starterObj.AddComponent<ProceduralTestStarter>();

        // Wire up LevelLoader reference
        SerializedObject soStarter = new SerializedObject(starter);
        SerializedProperty propLoader = soStarter.FindProperty("levelLoader");
        if (propLoader != null)
        {
            propLoader.objectReferenceValue = loader;
            soStarter.ApplyModifiedPropertiesWithoutUndo();
        }

        // Wire up palette reference
        SerializedProperty propStarterPalette = soStarter.FindProperty("palette");
        if (propStarterPalette != null)
        {
            propStarterPalette.objectReferenceValue = palette;
            soStarter.ApplyModifiedPropertiesWithoutUndo();
        }

        // Wire up tileRoot reference to the Tiles transform
        SerializedProperty propTileRootStarter = soStarter.FindProperty("tileRoot");
        if (propTileRootStarter != null)
        {
            propTileRootStarter.objectReferenceValue = tilesObj.transform;
            soStarter.ApplyModifiedPropertiesWithoutUndo();
        }

        // Add a Directional Light so we can see the dungeon
        GameObject lightObj = new GameObject("Directional Light");
        Light light = lightObj.AddComponent<Light>();
        light.type = LightType.Directional;
        lightObj.transform.rotation = Quaternion.Euler(50, -30, 0);

        // Save
        EditorSceneManager.SaveScene(scene, "Assets/Scenes/ProceduralTest.unity");
        Debug.Log("[CreateScene] ProceduralTest.unity created and wired successfully!");
        EditorGUIUtility.PingObject(loaderObj);
    }
}
