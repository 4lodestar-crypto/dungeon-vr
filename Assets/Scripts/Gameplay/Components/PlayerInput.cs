using DungeonVR.Gameplay.Logic;
using DungeonVR.Server;
using DungeonVR.Shared;
using DungeonVR.Shared.Requests;
using UnityEngine;

namespace DungeonVR.Gameplay.Components
{
    /// <summary>
    /// MonoBehaviour that reads WASD input and enqueues MovementRequests to the tick system.
    /// W = forward 1 tile, S = back 1 tile, A = rotate 90° left, D = rotate 90° right.
    /// V0-EXCEPTION: refactor through proper server layer in V1 — currently enqueues directly.
    /// </summary>
    public class PlayerInput : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GameTick _gameTick;

        private void Update()
        {
            if (_gameTick == null)
                return;

            int currentTick = _gameTick.CurrentTickNumber;

            if (Input.GetKeyDown(KeyCode.W))
            {
                Vector2Int forward = _gameTick.Champion.FacingDirection.ToOffset();
                _gameTick.EnqueueRequest(new MovementRequest(forward, currentTick));
            }
            else if (Input.GetKeyDown(KeyCode.S))
            {
                Vector2Int backward = -_gameTick.Champion.FacingDirection.ToOffset();
                _gameTick.EnqueueRequest(new MovementRequest(backward, currentTick));
            }
            else if (Input.GetKeyDown(KeyCode.A))
            {
                FacingDirection newFacing = _gameTick.Champion.FacingDirection.RotateLeft();
                _gameTick.EnqueueRequest(new MovementRequest(Vector2Int.zero, currentTick, newFacing));
            }
            else if (Input.GetKeyDown(KeyCode.D))
            {
                FacingDirection newFacing = _gameTick.Champion.FacingDirection.RotateRight();
                _gameTick.EnqueueRequest(new MovementRequest(Vector2Int.zero, currentTick, newFacing));
            }
        }

        private void OnValidate()
        {
            if (_gameTick == null)
                _gameTick = FindObjectOfType<GameTick>(); // V0-EXCEPTION: replace with DI in V1
        }
    }
}
