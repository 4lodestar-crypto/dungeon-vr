using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class CreateTestScene
{
    [MenuItem("Tools/Create Procedural Test Scene")]
    public static void Execute()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        EditorSceneManager.SaveScene(scene, "Assets/Scenes/ProceduralTest.unity");
        Debug.Log("[CreateTestScene] ProceduralTest.unity created and saved.");
    }
}
