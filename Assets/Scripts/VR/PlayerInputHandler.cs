using UnityEngine;

namespace DungeonVR.VR
{
    /// <summary>
    /// Handles WASD movement, jumping, and sprinting for a first-person dungeon game.
    /// Attach to a GameObject with a CharacterController component.
    /// Movement is relative to the camera's forward direction.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerInputHandler : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float walkSpeed = 5f;
        [SerializeField] private float sprintMultiplier = 2f;
        [SerializeField] private KeyCode sprintKey = KeyCode.LeftShift;

        [Header("Jump")]
        [SerializeField] private float jumpHeight = 2f;
        [SerializeField] private KeyCode jumpKey = KeyCode.Space;

        [Header("Gravity")]
        [SerializeField] private float gravity = -9.81f;

        [Header("References")]
        [SerializeField] private Camera playerCamera;

        private CharacterController controller;
        private Vector3 playerVelocity;
        private bool isGrounded;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();

            if (playerCamera == null)
                playerCamera = GetComponentInChildren<Camera>();
        }

        private void Update()
        {
            HandleGroundCheck();
            HandleMovement();
            HandleJump();
            ApplyGravity();
        }

        /// <summary>
        /// Checks if the character is grounded and resets vertical velocity when landing.
        /// </summary>
        private void HandleGroundCheck()
        {
            isGrounded = controller.isGrounded;

            if (isGrounded && playerVelocity.y < 0)
            {
                // Keep a small downward force so the character stays grounded
                playerVelocity.y = -2f;
            }
        }

        /// <summary>
        /// Reads WASD input and moves the character relative to the camera's orientation.
        /// Sprint (Shift) doubles the speed.
        /// </summary>
        private void HandleMovement()
        {
            float moveX = Input.GetAxisRaw("Horizontal");
            float moveZ = Input.GetAxisRaw("Vertical");

            Vector3 moveDirection = (moveX * GetCameraRight()) + (moveZ * GetCameraForward());
            moveDirection.y = 0f;
            moveDirection.Normalize();

            float currentSpeed = walkSpeed;

            if (Input.GetKey(sprintKey))
                currentSpeed *= sprintMultiplier;

            controller.Move(moveDirection * (currentSpeed * Time.deltaTime));
        }

        /// <summary>
        /// Reads jump input and applies an upward velocity when grounded.
        /// Uses the formula: velocity = sqrt(2 * gravity * height).
        /// </summary>
        private void HandleJump()
        {
            if (Input.GetKeyDown(jumpKey) && isGrounded)
            {
                playerVelocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            }
        }

        /// <summary>
        /// Applies gravity each frame regardless of grounded state.
        /// </summary>
        private void ApplyGravity()
        {
            playerVelocity.y += gravity * Time.deltaTime;
            controller.Move(playerVelocity * Time.deltaTime);
        }

        /// <summary>
        /// Returns the camera's forward direction projected onto the XZ plane.
        /// Falls back to transform.forward if no camera is assigned.
        /// </summary>
        private Vector3 GetCameraForward()
        {
            if (playerCamera == null)
                return transform.forward;

            Vector3 forward = playerCamera.transform.forward;
            forward.y = 0f;
            return forward.normalized;
        }

        /// <summary>
        /// Returns the camera's right direction projected onto the XZ plane.
        /// Falls back to transform.right if no camera is assigned.
        /// </summary>
        private Vector3 GetCameraRight()
        {
            if (playerCamera == null)
                return transform.right;

            Vector3 right = playerCamera.transform.right;
            right.y = 0f;
            return right.normalized;
        }

        #if UNITY_EDITOR
        private void OnValidate()
        {
            if (walkSpeed < 0f) walkSpeed = 0f;
            if (sprintMultiplier < 1f) sprintMultiplier = 1f;
            if (jumpHeight < 0f) jumpHeight = 0f;
        }
        #endif
    }
}
