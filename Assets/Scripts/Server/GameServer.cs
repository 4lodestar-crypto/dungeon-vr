using System.Collections.Generic;
using DungeonVR.Gameplay.Logic;
using DungeonVR.Shared;
using DungeonVR.Shared.Interfaces;
using DungeonVR.Shared.Requests;

namespace DungeonVR.Server
{
    /// <summary>
    /// Server-authoritative game loop. Owns GameState, queues requests,
    /// processes ticks, and exposes state snapshots.
    /// </summary>
    public class GameServer
    {
        public GameState State { get; private set; }
        private readonly Queue<MovementRequest> _requestQueue = new Queue<MovementRequest>();
        private IMovementRequestHandler _movementHandler;

        public GameServer() : this(new GameState(), null) { }

        public GameServer(GameState initialState, IMovementRequestHandler handler)
        {
            State = initialState ?? new GameState();
            _movementHandler = handler;
        }

        public void SetMovementHandler(IMovementRequestHandler handler)
        {
            _movementHandler = handler;
        }

        /// <summary>
        /// Queue a movement request for processing on the next tick.
        /// </summary>
        public void QueueRequest(MovementRequest request)
        {
            _requestQueue.Enqueue(request);
        }

        /// <summary>
        /// Process one tick: consume all queued requests and increment tick counter.
        /// </summary>
        public void ProcessTick()
        {
            while (_requestQueue.Count > 0)
            {
                var request = _requestQueue.Dequeue();
                if (_movementHandler != null)
                {
                    _movementHandler.Handle(request, State);
                }
            }
            State.CurrentTick++;
        }

        /// <summary>
        /// Returns a snapshot of the current game state.
        /// </summary>
        public GameState GetStateSnapshot()
        {
            return State;
        }
    }
}
