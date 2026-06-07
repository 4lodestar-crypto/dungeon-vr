using UnityEngine;

namespace DungeonVR.Shared.Requests
{
    /// <summary>
    /// A movement request submitted by the player input layer to the tick system.
    /// V0-EXCEPTION: refactor through server request layer in V1 — currently enqueued directly.
    /// </summary>
    public readonly struct MovementRequest
    {
        /// <summary>
        /// The direction to move or face.
        /// For W/S: direction is the world-space direction (forward facing or reverse).
        /// For A/D: direction is zero; facing change is indicated by <see cref="DesiredFacing"/> being set.
        /// </summary>
        public Vector2Int Direction { get; }

        /// <summary>The tick number when this request was submitted.</summary>
        public int TickNumber { get; }

        /// <summary>
        /// Optional — set when the request is a rotation (A/D) rather than translation (W/S).
        /// Null means translation; non-null means rotate to this facing.
        /// </summary>
        public FacingDirection? DesiredFacing { get; }

        public MovementRequest(Vector2Int direction, int tickNumber, FacingDirection? desiredFacing = null)
        {
            Direction = direction;
            TickNumber = tickNumber;
            DesiredFacing = desiredFacing;
        }

        public bool IsRotation => DesiredFacing.HasValue;
    }
}
