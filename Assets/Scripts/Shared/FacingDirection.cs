using UnityEngine;

namespace DungeonVR.Shared
{
    /// <summary>
    /// Grid directions for tile-based movement.
    /// Four cardinal directions only — no diagonal movement per ARCHITECTURE.md rule #2.
    /// </summary>
    public enum FacingDirection
    {
        North = 0,
        East = 1,
        South = 2,
        West = 3
    }

    /// <summary>
    /// Extension methods for FacingDirection — converts to/from Vector2Int for grid math.
    /// </summary>
    public static class FacingDirectionExtensions
    {
        /// <summary>Tile-space offset for the given direction.</summary>
        public static Vector2Int ToOffset(this FacingDirection dir)
        {
            return dir switch
            {
                FacingDirection.North => new Vector2Int(0, 1),
                FacingDirection.East  => new Vector2Int(1, 0),
                FacingDirection.South => new Vector2Int(0, -1),
                FacingDirection.West  => new Vector2Int(-1, 0),
                _ => Vector2Int.zero
            };
        }

        /// <summary>Rotate 90° counter-clockwise.</summary>
        public static FacingDirection RotateLeft(this FacingDirection dir)
        {
            int v = ((int)dir - 1) % 4;
            return (FacingDirection)(v < 0 ? v + 4 : v);
        }

        /// <summary>Rotate 90° clockwise.</summary>
        public static FacingDirection RotateRight(this FacingDirection dir)
        {
            return (FacingDirection)(((int)dir + 1) % 4);
        }
    }
}
