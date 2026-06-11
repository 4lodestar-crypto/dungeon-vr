using DungeonVR.Shared.Requests;
using DungeonVR.Shared.Results;

namespace DungeonVR.Shared.Interfaces
{
    /// <summary>
    /// Generic handler for gameplay requests following the server-authoritative
    /// request → validate → apply → emit pattern.
    /// V0-EXCEPTION: runs in-process; will route through network transport in V4+.
    /// </summary>
    public interface IMovementRequestHandler
    {
        MovementResult Handle(MovementRequest request, DungeonVR.Shared.GameState state);
    }

    /// <summary>
    /// Placeholder for V1+ expansion. V0-EXCEPTION: not yet used.
    /// </summary>
    public interface IGameState
    {
        int CurrentTick { get; }
    }
}
