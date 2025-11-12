using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace CameraSystem
{
    /// <summary>
    /// Utility functions for camera operations.
    /// Static helper methods that can be used throughout your project.
    /// </summary>
    public static class CameraUtilities
    {
        /// <summary>
        /// Get the world bounds of the camera viewport
        /// </summary>
        public static Bounds GetCameraViewBounds(Camera camera)
        {
            if (camera == null)
            {
                Debug.LogWarning("CameraUtilities: Camera is null");
                return new Bounds();
            }

            if (!camera.orthographic)
            {
                Debug.LogWarning("CameraUtilities: GetCameraViewBounds is designed for orthographic cameras");
            }

            float height = camera.orthographicSize * 2f;
            float width = height * camera.aspect;

            Vector3 center = camera.transform.position;
            Vector3 size = new Vector3(width, height, 0f);

            return new Bounds(center, size);
        }

        /// <summary>
        /// Get the four corners of the camera view in world space
        /// </summary>
        public static Vector3[] GetCameraViewCorners(Camera camera)
        {
            Bounds bounds = GetCameraViewBounds(camera);

            return new Vector3[]
            {
                new Vector3(bounds.min.x, bounds.min.y, camera.transform.position.z), // Bottom-left
                new Vector3(bounds.max.x, bounds.min.y, camera.transform.position.z), // Bottom-right
                new Vector3(bounds.max.x, bounds.max.y, camera.transform.position.z), // Top-right
                new Vector3(bounds.min.x, bounds.max.y, camera.transform.position.z)  // Top-left
            };
        }

        /// <summary>
        /// Check if a world position is visible by the camera
        /// </summary>
        public static bool IsPositionVisible(Camera camera, Vector3 worldPosition)
        {
            Vector3 viewportPoint = camera.WorldToViewportPoint(worldPosition);
            return viewportPoint.x >= 0f && viewportPoint.x <= 1f &&
                   viewportPoint.y >= 0f && viewportPoint.y <= 1f &&
                   viewportPoint.z > 0f;
        }

        /// <summary>
        /// Check if a bounds is visible by the camera
        /// </summary>
        public static bool IsBoundsVisible(Camera camera, Bounds bounds)
        {
            Bounds cameraBounds = GetCameraViewBounds(camera);
            return cameraBounds.Intersects(bounds);
        }

        /// <summary>
        /// Get camera size in world units
        /// </summary>
        public static Vector2 GetCameraSize(Camera camera)
        {
            float height = camera.orthographicSize * 2f;
            float width = height * camera.aspect;
            return new Vector2(width, height);
        }

        /// <summary>
        /// Convert screen position to world position on a specific Z plane
        /// </summary>
        public static Vector3 ScreenToWorldPoint(Camera camera, Vector2 screenPosition, float zDistance = 0f)
        {
            Vector3 screenPoint = new Vector3(screenPosition.x, screenPosition.y, zDistance);
            return camera.ScreenToWorldPoint(screenPoint);
        }

        /// <summary>
        /// Clamp a position within camera bounds
        /// </summary>
        public static Vector3 ClampPositionToCameraBounds(Camera camera, Vector3 position)
        {
            Bounds bounds = GetCameraViewBounds(camera);

            return new Vector3(
                Mathf.Clamp(position.x, bounds.min.x, bounds.max.x),
                Mathf.Clamp(position.y, bounds.min.y, bounds.max.y),
                position.z
            );
        }

        /// <summary>
        /// Calculate the required orthographic size to fit a bounds
        /// </summary>
        public static float CalculateOrthographicSizeToFitBounds(Camera camera, Bounds bounds)
        {
            float verticalSize = bounds.size.y / 2f;
            float horizontalSize = bounds.size.x / (2f * camera.aspect);

            return Mathf.Max(verticalSize, horizontalSize);
        }

        /// <summary>
        /// Smoothly move camera to a target position
        /// </summary>
        public static Vector3 SmoothMoveTowards(Vector3 current, Vector3 target, ref Vector3 velocity, float smoothTime)
        {
            return Vector3.SmoothDamp(current, target, ref velocity, smoothTime);
        }

        /// <summary>
        /// Get the mouse position in world space
        /// </summary>
        public static Vector3 GetMouseWorldPosition(Camera camera)
        {
#if ENABLE_INPUT_SYSTEM
            var mouse = Mouse.current;
            if (mouse == null) return Vector3.zero;
            Vector3 mousePos = mouse.position.ReadValue();
#else
            Vector3 mousePos = Input.mousePosition;
#endif
            mousePos.z = Mathf.Abs(camera.transform.position.z);
            return camera.ScreenToWorldPoint(mousePos);
        }

        /// <summary>
        /// Get the mouse position in world space (2D)
        /// </summary>
        public static Vector2 GetMouseWorldPosition2D(Camera camera)
        {
            Vector3 worldPos = GetMouseWorldPosition(camera);
            return new Vector2(worldPos.x, worldPos.y);
        }

        /// <summary>
        /// Calculate aspect ratio
        /// </summary>
        public static float CalculateAspectRatio(int width, int height)
        {
            return (float)width / (float)height;
        }

        /// <summary>
        /// Shake camera by a random offset
        /// </summary>
        public static Vector3 GetShakeOffset(float intensity)
        {
            return Random.insideUnitCircle * intensity;
        }

        /// <summary>
        /// Linear interpolation between two orthographic sizes
        /// </summary>
        public static float LerpOrthographicSize(float current, float target, float speed)
        {
            return Mathf.Lerp(current, target, speed * Time.deltaTime);
        }

        /// <summary>
        /// Get distance from camera to position
        /// </summary>
        public static float GetDistanceFromCamera(Camera camera, Vector3 position)
        {
            return Vector3.Distance(camera.transform.position, position);
        }

        /// <summary>
        /// Check if camera is orthographic
        /// </summary>
        public static bool IsOrthographic(Camera camera)
        {
            return camera != null && camera.orthographic;
        }

        /// <summary>
        /// Set camera to orthographic mode with a specific size
        /// </summary>
        public static void SetOrthographic(Camera camera, float size)
        {
            if (camera != null)
            {
                camera.orthographic = true;
                camera.orthographicSize = size;
            }
        }

        /// <summary>
        /// Create bounds from min and max points
        /// </summary>
        public static Bounds CreateBounds(Vector2 min, Vector2 max)
        {
            Vector2 center = (min + max) / 2f;
            Vector2 size = max - min;
            return new Bounds(center, size);
        }

        /// <summary>
        /// Get the viewport rectangle in world space
        /// </summary>
        public static Rect GetViewportWorldRect(Camera camera)
        {
            Bounds bounds = GetCameraViewBounds(camera);
            return new Rect(bounds.min.x, bounds.min.y, bounds.size.x, bounds.size.y);
        }
    }
}
