using DungeonVR.Gameplay.Components;
using DungeonVR.Gameplay.Logic;
using DungeonVR.Shared;
using UnityEngine;

namespace DungeonVR.VR
{
    /// <summary>
    /// First-person camera that follows the champion's position and facing direction
    /// with smooth interpolation. Supports mouse-look (right-button + drag) for
    /// free vertical and horizontal glance without changing the snap-turn scheme.
    ///
    /// V0-EXCEPTION: reads GameLoopController.GameState.Champion directly;
    /// server-layer state provider in V1.
    /// V0-EXCEPTION: FindObjectOfType for GameLoopController; replace with DI in V1.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class PlayerCameraController : MonoBehaviour
    {
        // ------------------------------------------------------------------
        //  Inspector tunables
        // ------------------------------------------------------------------

        [Header("Smoothing")]
        [SerializeField] private float _positionLerpSpeed = 10f;
        [SerializeField] private float _rotationLerpSpeed = 10f;

        [Header("Mouse Look")]
        [SerializeField] private float _mouseSensitivity = 2f;
        [SerializeField] private float _verticalClamp = 85f; // degrees from horizon

        // ------------------------------------------------------------------
        //  Private state
        // ------------------------------------------------------------------

        private Camera _camera;
        private GameLoopController _gameLoopController;

        // Champion-facing-derived base rotation (snap-turn)
        private Quaternion _targetBaseRotation;

        // Mouse-look offset accumulated while right button is held
        private float _mouseLookYaw;   // degrees, added to base rotation Y
        private float _mouseLookPitch; // degrees, applied as X rotation offset

        // ------------------------------------------------------------------
        //  Unity lifecycle
        // ------------------------------------------------------------------

        private void Awake()
        {
            _camera = GetComponent<Camera>();
            // V0-EXCEPTION: GameLoopController found in Start() not Awake()
            // because GridBuilder creates it during its Awake() and execution order
            // is non-deterministic.
        }

        private void Start()
        {
            _gameLoopController = FindObjectOfType<DungeonVR.Gameplay.Components.GameLoopController>();

            if (_gameLoopController == null)
            {
                Debug.LogError("[PlayerCameraController] No GameLoopController found in scene. Camera will not follow champion.", this);
                return;
            }

            // Snap to initial champion position immediately
            if (_gameLoopController.GameState?.Champion != null)
            {
                var champion = _gameLoopController.GameState.Champion;
                _targetBaseRotation = FacingIndexToRotation(champion.FacingIndex);
                transform.position = champion.WorldPosition + Vector3.up * GameConstants.EYE_HEIGHT;
                transform.rotation = _targetBaseRotation;
            }
        }

        private void Update()
        {
            if (_gameLoopController?.GameState?.Champion == null)
                return;

            ChampionState champion = _gameLoopController.GameState.Champion;

            // --- 1. Update base rotation from champion facing ---
            _targetBaseRotation = FacingIndexToRotation(champion.FacingIndex);

            // --- 2. Mouse look (right-mouse button) ---
            HandleMouseLook();

            // --- 3. Compute target transform ---
            Vector3 targetPosition = champion.WorldPosition + Vector3.up * GameConstants.EYE_HEIGHT;

            // Compose: champion base rotation first, then mouse-look yaw (horizontal),
            // then mouse-look pitch (vertical / local X).
            Quaternion targetRotation = _targetBaseRotation
                                        * Quaternion.Euler(0f, _mouseLookYaw, 0f)
                                        * Quaternion.Euler(_mouseLookPitch, 0f, 0f);

            // --- 4. Smooth interpolation ---
            transform.position = Vector3.Lerp(transform.position, targetPosition, _positionLerpSpeed * Time.deltaTime);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, _rotationLerpSpeed * Time.deltaTime);
        }

        // ------------------------------------------------------------------
        //  Mouse look
        // ------------------------------------------------------------------

        /// <summary>
        /// Accumulates mouse deltas while the right mouse button is held,
        /// clamped vertically to prevent over-rotation / flipping.
        /// </summary>
        private void HandleMouseLook()
        {
            if (Input.GetMouseButton(1)) // Right mouse button
            {
                float deltaX = Input.GetAxis("Mouse X") * _mouseSensitivity;
                float deltaY = Input.GetAxis("Mouse Y") * _mouseSensitivity;

                _mouseLookYaw   += deltaX;
                _mouseLookPitch -= deltaY; // Invert Y for natural look
                _mouseLookPitch  = Mathf.Clamp(_mouseLookPitch, -_verticalClamp, _verticalClamp);
            }
        }

        // ------------------------------------------------------------------
        //  Helpers
        // ------------------------------------------------------------------

        /// <summary>
        /// Maps FacingIndex to a world-space rotation.
        ///   0 (North) → (0,   0, 0)
        ///   1 (East)  → (0,  90, 0)
        ///   2 (South) → (0, 180, 0)
        ///   3 (West)  → (0, 270, 0)
        /// </summary>
        private static Quaternion FacingIndexToRotation(int facingIndex)
        {
            float yAngle = facingIndex switch
            {
                0 => 0f,
                1 => 90f,
                2 => 180f,
                3 => 270f,
                _ => 0f
            };

            return Quaternion.Euler(0f, yAngle, 0f);
        }
    }
}
