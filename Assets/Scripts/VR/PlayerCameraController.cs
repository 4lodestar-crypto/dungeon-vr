using UnityEngine;

namespace DungeonVR.VR
{
    /// <summary>
    /// First-person camera controller for DungeonVR.
    /// Reads mouse input to rotate the camera with configurable sensitivity
    /// and clamped vertical rotation (~90 degrees).
    /// Attach to the Main Camera GameObject.
    /// </summary>
    public class PlayerCameraController : MonoBehaviour
    {
        [Header("Mouse Settings")]
        [SerializeField]
        [Tooltip("Horizontal mouse sensitivity multiplier.")]
        private float mouseSensitivityX = 2.0f;

        [SerializeField]
        [Tooltip("Vertical mouse sensitivity multiplier.")]
        private float mouseSensitivityY = 2.0f;

        [Header("Rotation Limits")]
        [SerializeField]
        [Tooltip("Maximum upward look angle in degrees.")]
        private float maxLookUp = 80f;

        [SerializeField]
        [Tooltip("Maximum downward look angle in degrees.")]
        private float maxLookDown = 80f;

        [Header("Input")]
        [SerializeField]
        [Tooltip("Name of the horizontal mouse axis.")]
        private string mouseXAxis = "Mouse X";

        [SerializeField]
        [Tooltip("Name of the vertical mouse axis.")]
        private string mouseYAxis = "Mouse Y";

        [SerializeField]
        [Tooltip("Invert vertical look (true = pushing mouse up looks down).")]
        private bool invertY = false;

        [Header("Cursor")]
        [SerializeField]
        [Tooltip("Lock cursor to screen center and hide it on start.")]
        private bool lockCursor = true;

        // Accumulated vertical rotation for clamping.
        private float verticalRotation;

        private void Start()
        {
            if (lockCursor)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }

            // Initialise vertical rotation from the camera's current local X rotation.
            verticalRotation = transform.localEulerAngles.x;

            // eulerAngles.x is in the range [0, 360), but we work in [-180, 180].
            if (verticalRotation > 180f)
            {
                verticalRotation -= 360f;
            }

            // Warn if the camera is a child of a rotating parent (body rotation on the parent,
            // camera only handles vertical look in that setup).
            if (transform.parent != null)
            {
                Debug.Log(
                    $"{nameof(PlayerCameraController)} on '{transform.name}' has a parent " +
                    $"({transform.parent.name}). Ensure horizontal rotation is applied on the " +
                    $"parent GameObject, not on this camera.",
                    this
                );
            }
        }

        private void Update()
        {
            if (Cursor.lockState != CursorLockMode.Locked)
            {
                // When the cursor is unlocked (e.g. menu is open), do not rotate.
                return;
            }

            // Read mouse deltas for this frame.
            float mouseX = Input.GetAxis(mouseXAxis) * mouseSensitivityX;
            float mouseY = Input.GetAxis(mouseYAxis) * mouseSensitivityY;

            if (invertY)
            {
                mouseY = -mouseY;
            }

            // --- Vertical rotation (local X-axis) ---
            verticalRotation -= mouseY;
            verticalRotation = Mathf.Clamp(verticalRotation, -maxLookDown, maxLookUp);

            transform.localEulerAngles = new Vector3(verticalRotation, transform.localEulerAngles.y, 0f);

            // --- Horizontal rotation (world Y-axis) ---
            // Rotate the entire GameObject around the world up vector.
            transform.Rotate(Vector3.up, mouseX, Space.World);
        }

        /// <summary>
        /// Programmatically release or re-lock the cursor.
        /// Useful when opening in-game menus that require free cursor movement.
        /// </summary>
        public void SetCursorLock(bool locked)
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }
    }
}
