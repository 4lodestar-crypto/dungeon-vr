using DungeonVR.Gameplay;
using UnityEngine;

namespace DungeonVR.Shared.Results
{
    /// <summary>
    /// The result of processing a <c>MovementRequest</c> through the tick system.
    /// Contains the new <see cref="ChampionState"/> (cloned) on success, or the unchanged
    /// current state on failure — the caller decides whether to adopt it.
    /// V0-EXCEPTION: refactor through server response layer in V1.
    /// </summary>
    public readonly struct MovementResult
    {
        /// <summary>True if the move/rotate was applied successfully.</summary>
        public bool Success { get; }

        /// <summary>
        /// The champion state after processing this request.
        /// On success: a cloned, mutated copy of the input champion state (immutability pattern).
        /// On failure: the original champion state reference (unchanged).
        /// </summary>
        public ChampionState NewState { get; }

        /// <summary>Human-readable reason for rejection, empty on success.</summary>
        public string BlockReason { get; }

        public MovementResult(bool success, ChampionState newState, string blockReason)
        {
            Success = success;
            NewState = newState;
            BlockReason = blockReason ?? string.Empty;
        }

        public static MovementResult Succeeded(ChampionState newState) =>
            new MovementResult(true, newState, string.Empty);

        public static MovementResult Blocked(ChampionState currentState, string reason) =>
            new MovementResult(false, currentState, reason);
    }
}
