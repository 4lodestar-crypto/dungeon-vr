using DungeonVR.Shared;
using DungeonVR.Shared.Enums;
using DungeonVR.Shared.Interfaces;
using DungeonVR.Shared.Requests;
using DungeonVR.Shared.Results;
using UnityEngine;

namespace DungeonVR.Gameplay.Logic
{
    /// <summary>
    /// Handles movement requests for the champion. Validates against grid walls
    /// via IGridQueryService and applies position/facing changes to the GameState.
    /// </summary>
    public class MovementHandler : IMovementRequestHandler
    {
        private readonly IGridQueryService _gridQuery;

        public MovementHandler(IGridQueryService gridQuery)
        {
            _gridQuery = gridQuery;
        }

        public MovementResult Handle(MovementRequest request, GameState state)
        {
            var champion = state.Champion;
            if (champion == null)
                return MovementResult.Blocked("No champion in game state");

            int newX = champion.GridX;
            int newZ = champion.GridZ;
            int newFacing = champion.FacingIndex;

            switch (request.Direction)
            {
                case MovementDirection.Forward:
                    MoveForward(champion.FacingIndex, ref newX, ref newZ);
                    break;

                case MovementDirection.Backward:
                    MoveBackward(champion.FacingIndex, ref newX, ref newZ);
                    break;

                case MovementDirection.RotateLeft:
                    newFacing = (champion.FacingIndex + 3) % GameConstants.DIRECTION_COUNT;
                    champion.FacingIndex = newFacing;
                    return MovementResult.Valid(champion.WorldPosition, newFacing);

                case MovementDirection.RotateRight:
                    newFacing = (champion.FacingIndex + 1) % GameConstants.DIRECTION_COUNT;
                    champion.FacingIndex = newFacing;
                    return MovementResult.Valid(champion.WorldPosition, newFacing);

                default:
                    return MovementResult.Blocked($"Unknown direction: {request.Direction}");
            }

            if (!_gridQuery.IsWalkable(newX, newZ))
            {
                return MovementResult.Blocked($"Tile ({newX}, {newZ}) is blocked");
            }

            champion.GridX = newX;
            champion.GridZ = newZ;
            return MovementResult.Valid(champion.WorldPosition, champion.FacingIndex);
        }

        private static void MoveForward(int facingIndex, ref int x, ref int z)
        {
            switch (facingIndex)
            {
                case 0: z += 1; break; // North (+Z)
                case 1: x += 1; break; // East  (+X)
                case 2: z -= 1; break; // South (-Z)
                case 3: x -= 1; break; // West  (-X)
            }
        }

        private static void MoveBackward(int facingIndex, ref int x, ref int z)
        {
            switch (facingIndex)
            {
                case 0: z -= 1; break; // Opposite of North
                case 1: x -= 1; break; // Opposite of East
                case 2: z += 1; break; // Opposite of South
                case 3: x += 1; break; // Opposite of West
            }
        }
    }
}
