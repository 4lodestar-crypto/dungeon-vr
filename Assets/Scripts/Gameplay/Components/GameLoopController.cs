using DungeonVR.Shared;
using DungeonVR.Shared.Enums;
using DungeonVR.Shared.Requests;
using UnityEngine;

namespace DungeonVR.Gameplay.Components
{
    /// <summary>
    /// MonoBehaviour driver that calls ProcessTick() on FixedUpdate.
    /// V0 stub — ties the game loop to Unity's fixed timestep.
    /// </summary>
    public class GameLoopController : MonoBehaviour
    {
        [SerializeField]
        private float _tickInterval = GameConstants.TICK_DELTA;

        private float _timer;
        private DungeonVR.Server.GameServer _gameServer;

        /// <summary>
        /// Wire up the GameServer reference. Called by GridBuilder on setup.
        /// </summary>
        public void Initialize(DungeonVR.Server.GameServer gameServer)
        {
            _gameServer = gameServer;
        }

        /// <summary>
        /// Current GameState for use by other components (e.g. PlayerCameraController).
        /// </summary>
        public DungeonVR.Shared.GameState GameState => _gameServer?.State;

        private void FixedUpdate()
        {
            _timer += Time.fixedDeltaTime;
            if (_timer >= _tickInterval)
            {
                _timer -= _tickInterval;
                OnTick();
            }
        }

        /// <summary>
        /// Called once per game tick. Processes queued requests on the GameServer.
        /// </summary>
        private void OnTick()
        {
            _gameServer?.ProcessTick();
        }
    }
}
