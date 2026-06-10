using DungeonVR.Gameplay.Logic;
using DungeonVR.Shared;
using DungeonVR.Shared.Requests;
using DungeonVR.Shared.Results;

namespace DungeonVR.Server
{
    /// <summary>
    /// Server-layer facade for processing game requests.
    /// Routes incoming requests to the appropriate handler.
    /// V0: thin wrapper routing to MovementHandler. V1: add networking, validation, authentication, queuing.
    /// </summary>
    public class GameServer
    {
        /// <summary>
        /// Process a game request by routing it to the correct handler.
        /// Constrained to MovementRequest for V0; future request types will extend this pattern.
        /// </summary>
        /// <typeparam name="T">The request type (must be or extend MovementRequest).</typeparam>
        /// <param name="request">The request to process.</param>
        /// <param name="champion">Current champion state (mutated on success).</param>
        /// <param name="walls">2D grid wall data (true = blocked).</param>
        /// <returns>The result of processing the request.</returns>
        public MovementResult ProcessRequest<T>(T request, ChampionState champion, bool[,] walls)
            where T : MovementRequest
        {
            return MovementHandler.Handle(request, champion, walls);
        }
    }
}
