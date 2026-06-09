using UnityEngine;

namespace DungeonVR.Gameplay.Level
{
    /// <summary>
    /// Data container for a procedurally generated dungeon level.
    /// Passed from the generator to consumers that build visual tiles, collision, etc.
    /// V0-EXCEPTION: pure data — no server-layer wrapping yet (V1 will add content schema).
    /// </summary>
    public struct DungeonData
    {
        /// <summary>Grid width in tiles (columns).</summary>
        public int Width { get; }

        /// <summary>Grid height in tiles (rows).</summary>
        public int Height { get; }

        /// <summary>
        /// 2D bool array indexed [x, y] where true = blocked/wall, false = walkable/floor.
        /// x ranges 0..Width-1 (column), y ranges 0..Height-1 (row).
        /// </summary>
        public bool[,] Walls { get; }

        /// <summary>
        /// The first walkable tile in the first room's centre region.
        /// Guaranteed to be inside the grid and walkable.
        /// </summary>
        public Vector2Int StartPosition { get; }

        public DungeonData(int width, int height, bool[,] walls, Vector2Int startPosition)
        {
            Width = width;
            Height = height;
            Walls = walls;
            StartPosition = startPosition;
        }
    }

    /// <summary>
    /// Interface for procedural dungeon generation strategies.
    /// All generators produce a fully walled grid with at least one
    /// walkable chamber, guaranteed connectivity, and a valid start tile.
    /// V0-EXCEPTION: pure logic — no MonoBehaviour, no Unity scene dependencies.
    /// </summary>
    public interface IDungeonGenerator
    {
        /// <summary>
        /// Generate a complete dungeon layout deterministically from the given seed.
        /// The same seed always produces the same layout within the same generator
        /// implementation (same parameters).
        /// </summary>
        /// <param name="seed">Deterministic random seed. Use System.Random internally, NOT UnityEngine.Random.</param>
        /// <returns>A fully populated DungeonData with walls, walkable tiles, and a valid start position.</returns>
        DungeonData Generate(int seed);
    }
}
