namespace DungeonVR.Shared.Enums
{
    /// <summary>
    /// Classification for each tile in the dungeon grid.
    /// V1+ uses this in TileData for data-driven level pipeline.
    /// </summary>
    public enum TileType
    {
        /// <summary>Walkable ground tile — default.</summary>
        Floor,
        /// <summary>Impassable obstacle occupying the full tile.</summary>
        Wall,
        /// <summary>Walkable when open, blocks when closed. State managed by Gameplay Systems.</summary>
        Door,
        /// <summary>Walkable; triggers an effect when stepped on (V3+).</summary>
        Trap,
        /// <summary>Walkable; interaction point (save, heal, etc. — V3+).</summary>
        Altar,
        /// <summary>Player/champion spawn point. Exactly one per floor.</summary>
        Spawn,
        /// <summary>Walkable; transitions to next/previous floor (V2+).</summary>
        Stairs,
        /// <summary>Void tile — no geometry, out of bounds.</summary>
        Empty
    }
}
