using UnityEngine;

namespace DungeonVR.Shared.Results
{
    /// <summary>
    /// The result of processing a <c>MovementRequest</c> through the tick system.
    /// V0-EXCEPTION: refactor through server response layer in V1.
    /// </summary>
    public readonly struct MovementResult
    {
        /// <summary>True if the move/rotate was applied successfully.</summary>
        public bool Success { get; }

        /// <summary>The champion's new grid position after the move (unchanged if blocked).</summary>
        public Vector2Int NewPosition { get; }

        /// <summary>The champion's new facing direction after the move/rotate.</summary>
        public FacingDirection NewFacing { get; }

        /// <summary>Human-readable reason for rejection, empty on success.</summary>
        public string BlockReason { get; }

        public MovementResult(bool success, Vector2Int newPosition, FacingDirection newFacing, string blockReason)
        {
            Success = success;
            NewPosition = newPosition;
            NewFacing = newFacing;
            BlockReason = blockReason ?? string.Empty;
        }

        public static MovementResult Succeeded(Vector2Int newPosition, FacingDirection newFacing) =>
            new MovementResult(true, newPosition, newFacing, string.Empty);

        public static MovementResult Blocked(Vector2Int currentPosition, FacingDirection currentFacing, string reason) =>
            new MovementResult(false, currentPosition, currentFacing, reason);
    }
}
