using UnityEngine;

namespace CameraSystem
{
    /// <summary>
    /// Constrains camera movement within defined boundaries.
    /// Works with any camera movement system (CameraController2D, CameraFollowTarget, etc.)
    /// Attach this to your Camera GameObject to restrict its movement area.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class CameraBounds : MonoBehaviour
    {
        [Header("Boundary Settings")]
        [Tooltip("Enable boundary constraints")]
        [SerializeField] private bool enableBounds = true;

        [Tooltip("Minimum X position")]
        [SerializeField] private float minX = -20f;

        [Tooltip("Maximum X position")]
        [SerializeField] private float maxX = 20f;

        [Tooltip("Minimum Y position")]
        [SerializeField] private float minY = -10f;

        [Tooltip("Maximum Y position")]
        [SerializeField] private float maxY = 10f;

        [Header("Auto-Fit Settings")]
        [Tooltip("Automatically calculate bounds from a collider")]
        [SerializeField] private bool autoFitToCollider = false;

        [Tooltip("Collider to fit bounds to (BoxCollider2D or PolygonCollider2D)")]
        [SerializeField] private Collider2D boundsCollider;

        [Tooltip("Padding inside the collider bounds")]
        [SerializeField] private float boundsPadding = 1f;

        // Private variables
        private Camera cam;
        private float cameraHalfWidth;
        private float cameraHalfHeight;

        private void Awake()
        {
            cam = GetComponent<Camera>();
            UpdateCameraSize();

            if (autoFitToCollider && boundsCollider != null)
            {
                FitBoundsToCollider();
            }
        }

        private void LateUpdate()
        {
            if (!enableBounds) return;

            ApplyBounds();
        }

        /// <summary>
        /// Update camera viewport size (call if camera size changes)
        /// </summary>
        public void UpdateCameraSize()
        {
            if (cam.orthographic)
            {
                cameraHalfHeight = cam.orthographicSize;
                cameraHalfWidth = cameraHalfHeight * cam.aspect;
            }
            else
            {
                Debug.LogWarning("CameraBounds: Non-orthographic cameras not fully supported. Use orthographic camera for 2D.");
            }
        }

        /// <summary>
        /// Apply boundary constraints to camera position
        /// </summary>
        private void ApplyBounds()
        {
            Vector3 pos = transform.position;

            // Calculate effective bounds (accounting for camera size)
            float effectiveMinX = minX + cameraHalfWidth;
            float effectiveMaxX = maxX - cameraHalfWidth;
            float effectiveMinY = minY + cameraHalfHeight;
            float effectiveMaxY = maxY - cameraHalfHeight;

            // Clamp camera position
            pos.x = Mathf.Clamp(pos.x, effectiveMinX, effectiveMaxX);
            pos.y = Mathf.Clamp(pos.y, effectiveMinY, effectiveMaxY);

            transform.position = pos;
        }

        /// <summary>
        /// Automatically fit bounds to a collider
        /// </summary>
        private void FitBoundsToCollider()
        {
            if (boundsCollider == null)
            {
                Debug.LogWarning("CameraBounds: No collider assigned for auto-fit.");
                return;
            }

            Bounds colliderBounds = boundsCollider.bounds;

            minX = colliderBounds.min.x + boundsPadding;
            maxX = colliderBounds.max.x - boundsPadding;
            minY = colliderBounds.min.y + boundsPadding;
            maxY = colliderBounds.max.y - boundsPadding;

            Debug.Log($"CameraBounds: Auto-fitted to collider. Bounds: ({minX}, {minY}) to ({maxX}, {maxY})");
        }

        /// <summary>
        /// Set bounds manually
        /// </summary>
        public void SetBounds(float minX, float maxX, float minY, float maxY)
        {
            this.minX = minX;
            this.maxX = maxX;
            this.minY = minY;
            this.maxY = maxY;
            enableBounds = true;
        }

        /// <summary>
        /// Set bounds from a Rect
        /// </summary>
        public void SetBounds(Rect bounds)
        {
            minX = bounds.xMin;
            maxX = bounds.xMax;
            minY = bounds.yMin;
            maxY = bounds.yMax;
            enableBounds = true;
        }

        /// <summary>
        /// Set bounds from two corner points
        /// </summary>
        public void SetBounds(Vector2 bottomLeft, Vector2 topRight)
        {
            minX = bottomLeft.x;
            minY = bottomLeft.y;
            maxX = topRight.x;
            maxY = topRight.y;
            enableBounds = true;
        }

        /// <summary>
        /// Enable or disable bounds
        /// </summary>
        public void SetEnabled(bool enabled)
        {
            enableBounds = enabled;
        }

        /// <summary>
        /// Check if a position is within bounds
        /// </summary>
        public bool IsPositionInBounds(Vector3 position)
        {
            return position.x >= minX && position.x <= maxX &&
                   position.y >= minY && position.y <= maxY;
        }

        /// <summary>
        /// Get the current bounds as a Rect
        /// </summary>
        public Rect GetBounds()
        {
            return new Rect(minX, minY, maxX - minX, maxY - minY);
        }

        /// <summary>
        /// Get the center point of the bounds
        /// </summary>
        public Vector2 GetBoundsCenter()
        {
            return new Vector2((minX + maxX) / 2f, (minY + maxY) / 2f);
        }

#if UNITY_EDITOR
        /// <summary>
        /// Draw bounds in editor
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            if (!enableBounds) return;

            // Update camera size for accurate visualization
            if (cam == null) cam = GetComponent<Camera>();
            if (cam != null) UpdateCameraSize();

            // Draw outer bounds (actual limits)
            Gizmos.color = Color.red;
            Vector3 center = new Vector3((minX + maxX) / 2f, (minY + maxY) / 2f, transform.position.z);
            Vector3 size = new Vector3(maxX - minX, maxY - minY, 0.1f);
            Gizmos.DrawWireCube(center, size);

            // Draw effective bounds (accounting for camera size)
            Gizmos.color = Color.yellow;
            float effectiveMinX = minX + cameraHalfWidth;
            float effectiveMaxX = maxX - cameraHalfWidth;
            float effectiveMinY = minY + cameraHalfHeight;
            float effectiveMaxY = maxY - cameraHalfHeight;

            if (effectiveMaxX >= effectiveMinX && effectiveMaxY >= effectiveMinY)
            {
                Vector3 effectiveCenter = new Vector3(
                    (effectiveMinX + effectiveMaxX) / 2f,
                    (effectiveMinY + effectiveMaxY) / 2f,
                    transform.position.z
                );
                Vector3 effectiveSize = new Vector3(
                    effectiveMaxX - effectiveMinX,
                    effectiveMaxY - effectiveMinY,
                    0.1f
                );
                Gizmos.DrawWireCube(effectiveCenter, effectiveSize);
            }

            // Draw camera viewport
            Gizmos.color = Color.cyan;
            Vector3 cameraSize = new Vector3(cameraHalfWidth * 2f, cameraHalfHeight * 2f, 0.1f);
            Gizmos.DrawWireCube(transform.position, cameraSize);

            // Draw corner markers
            Gizmos.color = Color.red;
            float markerSize = 0.5f;
            Gizmos.DrawWireSphere(new Vector3(minX, minY, transform.position.z), markerSize);
            Gizmos.DrawWireSphere(new Vector3(maxX, minY, transform.position.z), markerSize);
            Gizmos.DrawWireSphere(new Vector3(minX, maxY, transform.position.z), markerSize);
            Gizmos.DrawWireSphere(new Vector3(maxX, maxY, transform.position.z), markerSize);
        }
#endif
    }
}
