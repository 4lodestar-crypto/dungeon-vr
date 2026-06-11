using DungeonVR.Shared.Data;
using DungeonVR.Shared.Enums;
using UnityEngine;

namespace DungeonVR.Level.Interfaces
{
    /// <summary>
    /// Maps TileType values to Unity prefabs for level instantiation.
    /// Implemented as a ScriptableObject in Level/Content domain.
    /// </summary>
    public interface ITilePalette
    {
        /// <summary>Returns the prefab GameObject for the given tile type, or null if not mapped.</summary>
        GameObject GetPrefab(TileType type);

        /// <summary>Returns true if all required TileType values have prefabs assigned.</summary>
        bool IsComplete { get; }
    }

    /// <summary>
    /// Loads level data from a source (JSON file, procedural generator, etc.)
    /// and produces instantiated tiles in the scene.
    /// </summary>
    public interface ILevelLoader
    {
        /// <summary>Load a level from a TextAsset (JSON). Returns true on success.</summary>
        bool LoadFromAsset(TextAsset levelData, ITilePalette palette, Transform tileRoot);

        /// <summary>Load a level from raw TileData array. Used by procedural generators.</summary>
        bool LoadFromData(TileData[] tiles, int width, int depth, ITilePalette palette, Transform tileRoot);

        /// <summary>Fired when level loading completes.</summary>
        event System.Action LevelLoaded;
    }

    /// <summary>
    /// Validates level data for correctness and solvability before loading.
    /// </summary>
    public interface ILevelValidator
    {
        /// <summary>Run all validation checks. Returns true if level is valid.</summary>
        bool Validate(TileData[] tiles, int width, int depth, ITilePalette palette, out string[] errors);

        /// <summary>Run solvability check (exit reachable from start).</summary>
        bool IsSolvable(TileData[] tiles, int width, int depth);
    }
}
