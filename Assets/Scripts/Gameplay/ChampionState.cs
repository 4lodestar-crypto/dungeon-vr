using UnityEngine;

namespace DungeonVR.Gameplay
{
    /// <summary>
    /// Pure data class representing the champion's current state on the grid.
    /// Owned by the server layer (GameState), read by visual layer.
    /// V0-EXCEPTION: refactor through proper server layer in V1.
    /// </summary>
    public class ChampionState
    {
        /// <summary>Tile coordinate on the grid (x = column, y = row).</summary>
        public Vector2Int GridPosition { get; set; }

        /// <summary>Which cardinal direction the champion is facing.</summary>
        public FacingDirection FacingDirection { get; set; }

        public ChampionState(Vector2Int gridPosition, FacingDirection facingDirection)
        {
            GridPosition = gridPosition;
            FacingDirection = facingDirection;
        }

        /// <summary>
        /// Create a deep copy — used for state snapshots after each tick.
        /// </summary>
        public ChampionState Clone()
        {
            return new ChampionState(GridPosition, FacingDirection);
        }
    }
}
