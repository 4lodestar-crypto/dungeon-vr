using UnityEngine;
using UnityEditor;

namespace DungeonVR.Editor
{
    /// <summary>
    /// Stub - pipeline completed. File retained to avoid missing-script warnings.
    /// </summary>
    public static class AutoTileCleanup
    {
        [MenuItem("Dungeon VR/Cleanup Temporary Scripts")]
        public static void RemoveTempScripts()
        {
            Debug.Log("[Cleanup] Temporary editor scripts can be removed from Assets/Scripts/Editor/");
        }
    }
}
