using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace CameraSystem
{
    /// <summary>
    /// Simple and robust 2D camera controller with keyboard input.
    /// Attach this to your Camera GameObject for manual camera control.
    /// Supports both Legacy Input and New Input System.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class CameraController2D : MonoBehaviour
    {
        [Header("Movement Settings")]
        [Tooltip("Camera movement speed in units per second")]
        [SerializeField] private float moveSpeed = 5f;

        [Tooltip("Enable smooth damping for camera movement")]
        [SerializeField] private bool smoothMovement = true;

        [Tooltip("Smoothing factor (lower = smoother, 0.01-1.0)")]
        [Range(0.01f, 1f)]
        [SerializeField] private float smoothTime = 0.1f;

        [Header("Input Settings")]
        [Tooltip("Use arrow keys for movement")]
        [SerializeField] private bool useArrowKeys = true;

        [Tooltip("Use WASD keys for movement")]
        [SerializeField] private bool useWASD = true;

        [Tooltip("Enable mouse drag to move camera (right-click)")]
        [SerializeField] private bool useMouseDrag = false;

        [Tooltip("Mouse drag sensitivity")]
        [SerializeField] private float mouseDragSensitivity = 1f;

        [Header("Constraints")]
        [Tooltip("Enable movement constraints")]
        [SerializeField] private bool enableConstraints = false;

        [Tooltip("Minimum X position")]
        [SerializeField] private float minX = -10f;

        [Tooltip("Maximum X position")]
        [SerializeField] private float maxX = 10f;

        [Tooltip("Minimum Y position")]
        [SerializeField] private float minY = -10f;

        [Tooltip("Maximum Y position")]
        [SerializeField] private float maxY = 10f;

        // Private variables
        private Camera cam;
        private Vector3 targetPosition;
        private Vector3 velocity = Vector3.zero;
        private Vector3 lastMousePosition;
        private bool isDragging = false;

        private void Awake()
        {
            cam = GetComponent<Camera>();
            targetPosition = transform.position;
        }

        private void Update()
        {
            HandleKeyboardInput();
            HandleMouseInput();
        }

        private void LateUpdate()
        {
            UpdateCameraPosition();
        }

        /// <summary>
        /// Handle keyboard input for camera movement
        /// </summary>
        private void HandleKeyboardInput()
        {
            Vector2 input = Vector2.zero;

#if ENABLE_INPUT_SYSTEM
            // New Input System
            var keyboard = Keyboard.current;
            if (keyboard == null) return;

            // Arrow keys
            if (useArrowKeys)
            {
                if (keyboard.upArrowKey.isPressed) input.y += 1f;
                if (keyboard.downArrowKey.isPressed) input.y -= 1f;
                if (keyboard.leftArrowKey.isPressed) input.x -= 1f;
                if (keyboard.rightArrowKey.isPressed) input.x += 1f;
            }

            // WASD keys
            if (useWASD)
            {
                if (keyboard.wKey.isPressed) input.y += 1f;
                if (keyboard.sKey.isPressed) input.y -= 1f;
                if (keyboard.aKey.isPressed) input.x -= 1f;
                if (keyboard.dKey.isPressed) input.x += 1f;
            }
#else
            // Legacy Input System
            // Arrow keys
            if (useArrowKeys)
            {
                if (Input.GetKey(KeyCode.UpArrow)) input.y += 1f;
                if (Input.GetKey(KeyCode.DownArrow)) input.y -= 1f;
                if (Input.GetKey(KeyCode.LeftArrow)) input.x -= 1f;
                if (Input.GetKey(KeyCode.RightArrow)) input.x += 1f;
            }

            // WASD keys
            if (useWASD)
            {
                if (Input.GetKey(KeyCode.W)) input.y += 1f;
                if (Input.GetKey(KeyCode.S)) input.y -= 1f;
                if (Input.GetKey(KeyCode.A)) input.x -= 1f;
                if (Input.GetKey(KeyCode.D)) input.x += 1f;
            }
#endif

            // Normalize diagonal movement
            if (input.magnitude > 1f)
            {
                input.Normalize();
            }

            // Apply movement
            if (input != Vector2.zero)
            {
                Vector3 movement = new Vector3(input.x, input.y, 0f) * moveSpeed * Time.deltaTime;
                targetPosition += movement;
            }
        }

        /// <summary>
        /// Handle mouse drag input for camera movement
        /// </summary>
        private void HandleMouseInput()
        {
            if (!useMouseDrag) return;

#if ENABLE_INPUT_SYSTEM
            // New Input System
            var mouse = Mouse.current;
            if (mouse == null) return;

            // Start dragging
            if (mouse.rightButton.wasPressedThisFrame)
            {
                isDragging = true;
                lastMousePosition = mouse.position.ReadValue();
            }

            // Stop dragging
            if (mouse.rightButton.wasReleasedThisFrame)
            {
                isDragging = false;
            }

            // Process drag
            if (isDragging)
            {
                Vector3 currentMousePosition = mouse.position.ReadValue();
                Vector3 delta = currentMousePosition - lastMousePosition;
                Vector3 move = new Vector3(-delta.x, -delta.y, 0f) * mouseDragSensitivity * Time.deltaTime;
                targetPosition += move;
                lastMousePosition = currentMousePosition;
            }
#else
            // Legacy Input System
            // Start dragging
            if (Input.GetMouseButtonDown(1)) // Right mouse button
            {
                isDragging = true;
                lastMousePosition = Input.mousePosition;
            }

            // Stop dragging
            if (Input.GetMouseButtonUp(1))
            {
                isDragging = false;
            }

            // Process drag
            if (isDragging)
            {
                Vector3 delta = Input.mousePosition - lastMousePosition;
                Vector3 move = new Vector3(-delta.x, -delta.y, 0f) * mouseDragSensitivity * Time.deltaTime;
                targetPosition += move;
                lastMousePosition = Input.mousePosition;
            }
#endif
        }

        /// <summary>
        /// Update camera position with smoothing or direct movement
        /// </summary>
        private void UpdateCameraPosition()
        {
            // Apply constraints if enabled
            if (enableConstraints)
            {
                targetPosition.x = Mathf.Clamp(targetPosition.x, minX, maxX);
                targetPosition.y = Mathf.Clamp(targetPosition.y, minY, maxY);
            }

            // Preserve Z position
            targetPosition.z = transform.position.z;

            // Apply movement
            if (smoothMovement)
            {
                transform.position = Vector3.SmoothDamp(
                    transform.position,
                    targetPosition,
                    ref velocity,
                    smoothTime
                );
            }
            else
            {
                transform.position = targetPosition;
            }
        }

        /// <summary>
        /// Set camera position instantly (useful for scene transitions)
        /// </summary>
        public void SetPosition(Vector3 position)
        {
            position.z = transform.position.z;
            transform.position = position;
            targetPosition = position;
            velocity = Vector3.zero;
        }

        /// <summary>
        /// Set camera position with X and Y only
        /// </summary>
        public void SetPosition(float x, float y)
        {
            SetPosition(new Vector3(x, y, transform.position.z));
        }

        /// <summary>
        /// Move camera by offset
        /// </summary>
        public void MoveBy(Vector3 offset)
        {
            targetPosition += offset;
        }

        /// <summary>
        /// Set movement speed at runtime
        /// </summary>
        public void SetMoveSpeed(float speed)
        {
            moveSpeed = Mathf.Max(0f, speed);
        }

        /// <summary>
        /// Enable or disable camera movement
        /// </summary>
        public void SetEnabled(bool enabled)
        {
            this.enabled = enabled;
        }

        /// <summary>
        /// Set movement constraints
        /// </summary>
        public void SetConstraints(float minX, float maxX, float minY, float maxY)
        {
            this.minX = minX;
            this.maxX = maxX;
            this.minY = minY;
            this.maxY = maxY;
            enableConstraints = true;
        }

        /// <summary>
        /// Disable movement constraints
        /// </summary>
        public void DisableConstraints()
        {
            enableConstraints = false;
        }

#if UNITY_EDITOR
        /// <summary>
        /// Draw constraint bounds in editor
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            if (!enableConstraints) return;

            Gizmos.color = Color.yellow;
            Vector3 center = new Vector3((minX + maxX) / 2f, (minY + maxY) / 2f, transform.position.z);
            Vector3 size = new Vector3(maxX - minX, maxY - minY, 0.1f);
            Gizmos.DrawWireCube(center, size);
        }
#endif
    }
}
