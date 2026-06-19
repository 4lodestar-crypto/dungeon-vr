using DungeonVR.Level.Components;
using DungeonVR.Level.Data;
using DungeonVR.Level.Logic;
using DungeonVR.Shared.Data;
using UnityEngine;

public class ProceduralTestStarter : MonoBehaviour
{
    public LevelLoader levelLoader;

    void Start()
    {
        var p = ScriptableObject.CreateInstance<DungeonParams>();
        p.Seed = 42;
        p.Width = 16;
        p.Depth = 16;

        TileData[] tiles = DungeonGenerator.Generate(p);
        bool success = levelLoader.LoadFromData(tiles, p.Width, p.Depth, 
            levelLoader.Palette, levelLoader.TileRoot);
        if (success)
            Debug.Log("[ProceduralTest] Dungeon generated successfully!");
        else
            Debug.LogError("[ProceduralTest] Failed to load dungeon");
    }
}
