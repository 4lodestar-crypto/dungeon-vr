using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using DungeonVR.Gameplay.Components;
using DungeonVR.VR;
using DungeonVR.Shared;

public static class VerifyTestScene
{
    [MenuItem("Tools/Verify TestGrid Scene")]
    public static void Verify()
    {
        // Open the scene
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/TestGrid.unity");
        EditorSceneManager.SetActiveScene(scene);

        // Find GameManager
        var gm = GameObject.Find("GameManager");
        if (gm == null) { Debug.LogError("FAIL: GameManager not found"); return; }
        if (gm.GetComponent<GridBuilder>() == null) { Debug.LogError("FAIL: GridBuilder missing"); return; }
        if (gm.GetComponent<PlayerInputHandler>() == null) { Debug.LogError("FAIL: PlayerInputHandler missing"); return; }
        Debug.Log("PASS: GameManager with GridBuilder + PlayerInputHandler");

        // Find Camera child
        var cam = GameObject.Find("Camera");
        if (cam == null) { Debug.LogError("FAIL: Camera not found"); return; }
        if (cam.GetComponent<PlayerCameraController>() == null) { Debug.LogError("FAIL: PlayerCameraController missing"); return; }
        if (cam.transform.parent != gm.transform) { Debug.LogError("FAIL: Camera not child of GameManager"); return; }
        Debug.Log("PASS: Camera with PlayerCameraController, child of GameManager");

        // Find Floor
        var floor = GameObject.Find("Floor");
        if (floor == null) { Debug.LogError("FAIL: Floor not found"); return; }
        Debug.Log("PASS: Floor exists");

        // Enter Play Mode
        EditorApplication.isPlaying = true;
        Debug.Log("SUCCESS: All components verified, entered Play Mode. WASD should move the champion.");
    }
}
