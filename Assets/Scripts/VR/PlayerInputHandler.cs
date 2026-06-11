using DungeonVR.Gameplay.Components;
using DungeonVR.Shared;
using DungeonVR.Shared.Enums;
using UnityEngine;

namespace DungeonVR.VR
{
    /// <summary>
    /// Handles WASD and arrow-key input, converting presses into MovementRequest
    /// structs and dispatching them to the InputQueueBridge.
    ///
    /// V0-EXCEPTION: no rebinding system yet; uses hardcoded KeyCode checks.
    /// V0-EXCEPTION: FindObjectOfType for InputQueueBridge; replace with DI in V1.
    /// </summary>
    public class PlayerInputHandler : MonoBehaviour
    {
        // ------------------------------------------------------------------
        //  Inspector — V0 stub for future rebinding system
        // ------------------------------------------------------------------
        [Header("Rebinding (V0 stub — not used yet)")]
        // [SerializeField] private InputActionMap _actionMap;

        // ------------------------------------------------------------------
        //  Private state
        // ------------------------------------------------------------------

        private InputQueueBridge _inputQueueBridge;

        // Per-key "press was already dispatched" flags — prevents repeat triggers
        // while a key is held down (holding W sends ONE move, not a stream).
        private bool _wHeld;
        private bool _sHeld;
        private bool _aHeld;
        private bool _dHeld;

        private bool _upHeld;
        private bool _downHeld;
        private bool _leftHeld;
        private bool _rightHeld;

        // ------------------------------------------------------------------
        //  Unity lifecycle
        // ------------------------------------------------------------------

        private void Awake()
        {
            // V0-EXCEPTION: InputQueueBridge found in Start() not Awake()
            // because GridBuilder creates it during its Awake() and execution order
            // of components on the same GameObject is non-deterministic.
        }

        private void Start()
        {
            _inputQueueBridge = FindObjectOfType<DungeonVR.Gameplay.Components.InputQueueBridge>();

            if (_inputQueueBridge == null)
            {
                Debug.LogError("[PlayerInputHandler] No InputQueueBridge found in scene. Input will be ignored.", this);
            }
        }

        private void Update()
        {
            if (_inputQueueBridge == null)
                return;

            // --- WASD (primary) ---
            HandleKey(KeyCode.W, MovementDirection.Forward,  ref _wHeld);
            HandleKey(KeyCode.S, MovementDirection.Backward, ref _sHeld);
            HandleKey(KeyCode.A, MovementDirection.RotateLeft,  ref _aHeld);
            HandleKey(KeyCode.D, MovementDirection.RotateRight, ref _dHeld);

            // --- Arrow keys (secondary) ---
            HandleKey(KeyCode.UpArrow,    MovementDirection.Forward,      ref _upHeld);
            HandleKey(KeyCode.DownArrow,  MovementDirection.Backward,     ref _downHeld);
            HandleKey(KeyCode.LeftArrow,  MovementDirection.RotateLeft,   ref _leftHeld);
            HandleKey(KeyCode.RightArrow, MovementDirection.RotateRight,  ref _rightHeld);
        }

        // ------------------------------------------------------------------
        //  Input dispatch
        // ------------------------------------------------------------------

        /// <summary>
        /// Processes a single key: queues one MovementRequest on first press,
        /// then suppresses repeats until the key is released.
        /// </summary>
        private void HandleKey(KeyCode key, MovementDirection direction, ref bool heldFlag)
        {
            if (Input.GetKey(key))
            {
                // First frame the key is held — dispatch exactly one request
                if (!heldFlag)
                {
                    heldFlag = true;
                    DispatchMovement(direction);
                }
                // Subsequent held frames: do nothing (repeat suppression)
            }
            else
            {
                // Key released — allow next press
                heldFlag = false;
            }
        }

        /// <summary>
        /// Builds a MovementRequest with the current tick and sends it to the bridge.
        /// </summary>
        private void DispatchMovement(MovementDirection direction)
        {
            // V0-EXCEPTION: tick number sourced from InputQueueBridge/GameLoopController;
            // server-authoritative tick in V1
            int tick = _inputQueueBridge.CurrentTick;
            var request = new Shared.Requests.MovementRequest(direction, tick);
            _inputQueueBridge.EnqueueRequest(request);
        }
    }
}
