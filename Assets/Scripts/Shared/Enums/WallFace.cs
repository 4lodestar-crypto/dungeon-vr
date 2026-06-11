using System;

namespace DungeonVR.Shared.Enums
{
    /// <summary>
    /// Bitmask describing which edges of a tile have wall geometry.
    /// Used for interior walls, room boundaries, and door frames.
    /// </summary>
    [Flags]
    public enum WallFace
    {
        None  = 0,
        North = 1 << 0,  // +Z face
        South = 1 << 1,  // -Z face
        East  = 1 << 2,  // +X face
        West  = 1 << 3,  // -X face
        All   = North | South | East | West
    }
}
