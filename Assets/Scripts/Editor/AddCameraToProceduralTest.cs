using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using DungeonVR.VR;

/// <summary>
/// Emergency fix: Adds a Main Camera and PlayerInputHandler to the existing
/// ProceduralTest.unity scene which has no camera.
/// </summary>
public static class AddCameraToProceduralTest
{
    [MenuItem("Dungeon VR/Add Camera to ProceduralTest")]
    public static void AddCamera()
    {
        // Open the existing scene
        string scenePath = "Assets/Scenes/ProceduralTest.unity";
        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

        if (!scene.IsValid())
        {
            Debug.LogError("[AddCamera] Could not open ProceduralTest.unity");
            return;
        }

        Debug.Log($"[AddCamera] Opened scene: {scene.name}");

        // --- Create the Player GameObject (parent for camera + input handler) ---
        GameObject playerGO = new GameObject("Player");
        playerGO.transform.position = new Vector3(0, 1.7f, 0);

        // Add CharacterController (required by PlayerInputHandler)
        CharacterController cc = playerGO.AddComponent<CharacterController>();
        cc.height = 1.8f;
        cc.radius = 0.4f;
        cc.center = new Vector3(0, 0.9f, 0);

        // Add PlayerInputHandler for WASD movement + jump + sprint
        PlayerInputHandler inputHandler = playerGO.AddComponent<PlayerInputHandler>();

        // --- Create Main Camera as child of Player ---
        GameObject camGO = new GameObject("Main Camera");
        camGO.tag = "MainCamera";
        camGO.transform.SetParent(playerGO.transform);
        camGO.transform.localPosition = Vector3.zero; // Player transform handles eye height

        // Add Camera component
        Camera cam = camGO.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.Skybox;
        cam.nearClipPlane = 0.1f;
        cam.farClipPlane = 1000f;
        cam.fieldOfView = 75f;

        // Add PlayerCameraController
        PlayerCameraController camController = camGO.AddComponent<PlayerCameraController>();

        // Wire up the camera reference in PlayerInputHandler
        SerializedObject so = new SerializedObject(inputHandler);
        SerializedProperty propCam = so.FindProperty("playerCamera");
        if (propCam != null)
        {
            propCam.objectReferenceValue = cam;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // --- Ensure a Directional Light exists ---
        Light existingLight = Object.FindObjectOfType<Light>();
        if (existingLight == null)
        {
            GameObject lightGO = new GameObject("Directional Light");
            Light light = lightGO.AddComponent<Light>();
            light.type = LightType.Directional;
            lightGO.transform.rotation = Quaternion.Euler(50, -30, 0);
            Debug.Log("[AddCamera] Added Directional Light");
        }

        // Also add an AudioListener to the camera (Unity requires one)
        AudioListener al = camGO.GetComponent<AudioListener>();
        if (al == null)
        {
            camGO.AddComponent<AudioListener>();
        }

        // Save the scene
        EditorSceneManager.SaveScene(scene, scenePath);
        AssetDatabase.Refresh();

        Debug.Log("[AddCamera] SUCCESS: Main Camera + PlayerInputHandler added to ProceduralTest.unity");
        Debug.Log($"[AddCamera] Player at (0, 1.7, 0), Camera as child at local (0, 0, 0)");
        Debug.Log($"[AddCamera] Components: CharacterController, PlayerInputHandler (Player) + Camera, PlayerCameraController, AudioListener (Main Camera)");

        // Ping the Player so it's visible in hierarchy
        EditorGUIUtility.PingObject(playerGO);
    }
}
