using UnityEngine;

namespace CameraSystem
{
    /// <summary>
    /// Handles camera zoom functionality with smooth transitions.
    /// Attach this to your Camera GameObject for zoom controls.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class CameraZoom : MonoBehaviour
    {
        [Header("Zoom Settings")]
        [Tooltip("Minimum orthographic size (zoomed in)")]
        [SerializeField] private float minZoom = 2f;

        [Tooltip("Maximum orthographic size (zoomed out)")]
        [SerializeField] private float maxZoom = 10f;

        [Tooltip("Default/starting zoom level")]
        [SerializeField] private float defaultZoom = 5f;

        [Tooltip("Zoom speed for smooth transitions")]
        [SerializeField] private float zoomSpeed = 5f;

        [Header("Input Settings")]
        [Tooltip("Enable mouse scroll wheel zoom")]
        [SerializeField] private bool enableScrollWheelZoom = true;

        [Tooltip("Scroll wheel sensitivity")]
        [SerializeField] private float scrollSensitivity = 1f;

        [Tooltip("Enable keyboard zoom (+ and - keys)")]
        [SerializeField] private bool enableKeyboardZoom = true;

        [Tooltip("Keyboard zoom speed")]
        [SerializeField] private float keyboardZoomSpeed = 2f;

        // Private variables
        private Camera cam;
        private float targetZoom;
        private float currentZoom;

        private void Awake()
        {
            cam = GetComponent<Camera>();

            if (!cam.orthographic)
            {
                Debug.LogWarning("CameraZoom: This component is designed for orthographic cameras. For perspective cameras, modify FOV instead.");
            }

            currentZoom = defaultZoom;
            targetZoom = defaultZoom;
            cam.orthographicSize = currentZoom;
        }

        private void Update()
        {
            HandleInput();
        }

        private void LateUpdate()
        {
            UpdateZoom();
        }

        /// <summary>
        /// Handle zoom input
        /// </summary>
        private void HandleInput()
        {
            // Mouse scroll wheel zoom
            if (enableScrollWheelZoom)
            {
                float scroll = Input.GetAxis("Mouse ScrollWheel");
                if (scroll != 0f)
                {
                    targetZoom -= scroll * scrollSensitivity;
                    targetZoom = Mathf.Clamp(targetZoom, minZoom, maxZoom);
                }
            }

            // Keyboard zoom (+ and - keys, or Equals and Minus)
            if (enableKeyboardZoom)
            {
                if (Input.GetKey(KeyCode.Equals) || Input.GetKey(KeyCode.KeypadPlus))
                {
                    targetZoom -= keyboardZoomSpeed * Time.deltaTime;
                }

                if (Input.GetKey(KeyCode.Minus) || Input.GetKey(KeyCode.KeypadMinus))
                {
                    targetZoom += keyboardZoomSpeed * Time.deltaTime;
                }

                targetZoom = Mathf.Clamp(targetZoom, minZoom, maxZoom);
            }
        }

        /// <summary>
        /// Smoothly update camera zoom
        /// </summary>
        private void UpdateZoom()
        {
            currentZoom = Mathf.Lerp(currentZoom, targetZoom, Time.deltaTime * zoomSpeed);
            cam.orthographicSize = currentZoom;
        }

        /// <summary>
        /// Set zoom level instantly
        /// </summary>
        public void SetZoom(float zoom)
        {
            targetZoom = Mathf.Clamp(zoom, minZoom, maxZoom);
            currentZoom = targetZoom;
            cam.orthographicSize = currentZoom;
        }

        /// <summary>
        /// Set zoom level with smooth transition
        /// </summary>
        public void SetZoomSmooth(float zoom)
        {
            targetZoom = Mathf.Clamp(zoom, minZoom, maxZoom);
        }

        /// <summary>
        /// Reset to default zoom
        /// </summary>
        public void ResetZoom()
        {
            targetZoom = defaultZoom;
        }

        /// <summary>
        /// Zoom in by a specific amount
        /// </summary>
        public void ZoomIn(float amount)
        {
            targetZoom = Mathf.Clamp(targetZoom - amount, minZoom, maxZoom);
        }

        /// <summary>
        /// Zoom out by a specific amount
        /// </summary>
        public void ZoomOut(float amount)
        {
            targetZoom = Mathf.Clamp(targetZoom + amount, minZoom, maxZoom);
        }

        /// <summary>
        /// Set zoom limits
        /// </summary>
        public void SetZoomLimits(float min, float max)
        {
            minZoom = min;
            maxZoom = max;
            targetZoom = Mathf.Clamp(targetZoom, minZoom, maxZoom);
        }

        /// <summary>
        /// Get current zoom level
        /// </summary>
        public float GetCurrentZoom()
        {
            return currentZoom;
        }

        /// <summary>
        /// Get target zoom level
        /// </summary>
        public float GetTargetZoom()
        {
            return targetZoom;
        }

        /// <summary>
        /// Enable or disable zoom controls
        /// </summary>
        public void SetEnabled(bool enabled)
        {
            this.enabled = enabled;
        }
    }
}
