using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class CreateTestScene
{
    [MenuItem("Tools/Create TestGrid Scene")]
    public static void Create()
    {
        // Create new empty scene
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // Create GameManager with script components by string name
        var gameManager = new GameObject("GameManager");
        
        // Add GridBuilder (in DungeonVR.Gameplay.Components)
        var gridType = System.Type.GetType("DungeonVR.Gameplay.Components.GridBuilder, DungeonVR.Gameplay");
        if (gridType != null) gameManager.AddComponent(gridType);
        else Debug.LogError("GridBuilder type not found!");
        
        // Add PlayerInputHandler (in DungeonVR.VR)
        var inputType = System.Type.GetType("DungeonVR.VR.PlayerInputHandler, DungeonVR.VR");
        if (inputType != null) gameManager.AddComponent(inputType);
        else Debug.LogError("PlayerInputHandler type not found!");

        // Create Camera as child with PlayerCameraController
        var camObj = new GameObject("Camera");
        camObj.transform.SetParent(gameManager.transform);
        var cam = camObj.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Color.gray;
        cam.nearClipPlane = 0.1f;
        cam.farClipPlane = 100f;
        
        // Add PlayerCameraController
        var camControlType = System.Type.GetType("DungeonVR.VR.PlayerCameraController, DungeonVR.VR");
        if (camControlType != null) camObj.AddComponent(camControlType);
        else Debug.LogError("PlayerCameraController type not found!");

        // Add simple floor plane
        var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
        floor.name = "Floor";
        floor.transform.position = new Vector3(0, -0.5f, 0);
        floor.transform.localScale = new Vector3(5, 1, 5);

        // Position camera
        camObj.transform.localPosition = new Vector3(0, 0.5f, 0);

        // Save scene
        var path = "Assets/Scenes/TestGrid.unity";
        EditorSceneManager.SaveScene(scene, path);
        Debug.Log($"Scene saved to {path}");
    }
}
