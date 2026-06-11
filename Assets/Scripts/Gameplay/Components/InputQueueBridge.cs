using DungeonVR.Server;
using DungeonVR.Shared.Requests;
using UnityEngine;

namespace DungeonVR.Gameplay.Components
{
    /// <summary>
    /// MonoBehaviour that bridges player input to the server's request queue.
    /// V0 stub — direct input binding; proper input system in V1+.
    /// V0-EXCEPTION: static accessor pattern; DI container in V1.
    /// </summary>
    public class InputQueueBridge : MonoBehaviour
    {
        /// <summary>
        /// V0-EXCEPTION: static accessor for cross-component lookup; DI in V1.
        /// </summary>
        public static InputQueueBridge Instance { get; private set; }

        private GameServer _gameServer;

        /// <summary>Current tick number, sourced from the GameServer.</summary>
        public int CurrentTick => _gameServer?.State?.CurrentTick ?? 0;

        private void Awake()
        {
            // V0-EXCEPTION: static singleton; replace with constructor DI in V1
            Instance = this;
        }

        /// <summary>
        /// Wire up the GameServer reference. Called by GridBuilder on setup.
        /// </summary>
        public void Initialize(GameServer gameServer)
        {
            _gameServer = gameServer;
        }

        /// <summary>
        /// Enqueue a movement request to the server for processing on the next tick.
        /// </summary>
        public void EnqueueRequest(MovementRequest request)
        {
            if (_gameServer != null)
            {
                _gameServer.QueueRequest(request);
            }
            else
            {
                Debug.LogWarning("[InputQueueBridge] No GameServer assigned. Request ignored.");
            }
        }
    }
}
