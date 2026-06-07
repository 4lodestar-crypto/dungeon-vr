using DungeonVR.Gameplay.Logic;
using DungeonVR.Server;
using DungeonVR.Shared;
using UnityEngine;

namespace DungeonVR.Gameplay.Components
{
    /// <summary>
    /// MonoBehaviour that positions a first-person camera at champion eye height (1.7m),
    /// facing the champion's facing direction.
    /// V0-EXCEPTION: refactor through proper visual layer separation in V1.
    /// </summary>
    public class PlayerCamera : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GameTick _gameTick;

        [Header("Settings")]
        [SerializeField] private float _eyeHeight = 1.7f;
        [SerializeField] private float _smoothTime = 0.1f;

        private Camera _camera;
        private Vector3 _velocity;

        private void Awake()
        {
            _camera = GetComponent<Camera>();
            if (_camera == null)
                _camera = gameObject.AddComponent<Camera>();
        }

        private void LateUpdate()
        {
            if (_gameTick?.Champion == null)
                return;

            ChampionState champion = _gameTick.Champion;
            Vector3 targetPos = MovementHandler.TileToWorld(champion.GridPosition);
            targetPos.y = _eyeHeight;

            // Smooth position follow
            transform.position = Vector3.SmoothDamp(transform.position, targetPos, ref _velocity, _smoothTime);

            // Set facing rotation based on direction
            transform.rotation = GetFacingRotation(champion.FacingDirection);
        }

        private static Quaternion GetFacingRotation(FacingDirection facing)
        {
            return facing switch
            {
                FacingDirection.North => Quaternion.LookRotation(Vector3.forward),
                FacingDirection.East  => Quaternion.LookRotation(Vector3.right),
                FacingDirection.South => Quaternion.LookRotation(Vector3.back),
                FacingDirection.West  => Quaternion.LookRotation(Vector3.left),
                _ => Quaternion.identity
            };
        }

        private void OnValidate()
        {
            if (_gameTick == null)
                _gameTick = FindObjectOfType<GameTick>(); // V0-EXCEPTION: replace with DI in V1
        }
    }
}
