using UnityEngine;

namespace DungeonVR.Shared.Results
{
    /// <summary>
    /// Result of processing a MovementRequest.
    /// </summary>
    public readonly struct MovementResult
    {
        public readonly bool Success;
        public readonly string BlockReason;
        public readonly Vector3 NewPosition;
        public readonly int NewFacingIndex;

        private MovementResult(bool success, string blockReason, Vector3 newPos, int newFacing)
        {
            Success = success;
            BlockReason = blockReason;
            NewPosition = newPos;
            NewFacingIndex = newFacing;
        }

        public static MovementResult Valid(Vector3 newPosition, int newFacing)
            => new MovementResult(true, null, newPosition, newFacing);

        public static MovementResult Blocked(string reason)
            => new MovementResult(false, reason, Vector3.zero, 0);

        public override string ToString()
            => Success
                ? $"MovementResult: OK → ({NewPosition.x:F1}, {NewPosition.z:F1}) facing {NewFacingIndex}"
                : $"MovementResult: BLOCKED — {BlockReason}";
    }
}
