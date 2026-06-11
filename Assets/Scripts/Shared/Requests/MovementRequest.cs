using DungeonVR.Shared.Enums;

namespace DungeonVR.Shared.Requests
{
    /// <summary>
    /// A request from the player to move or turn the champion.
    /// Consumed by MovementHandler in Gameplay/Logic.
    /// </summary>
    public readonly struct MovementRequest
    {
        public readonly MovementDirection Direction;
        public readonly int TickNumber;

        public MovementRequest(MovementDirection direction, int tickNumber)
        {
            Direction = direction;
            TickNumber = tickNumber;
        }

        public override string ToString()
            => $"MovementRequest [{Direction}] @ tick {TickNumber}";
    }
}
