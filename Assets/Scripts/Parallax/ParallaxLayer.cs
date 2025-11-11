using UnityEngine;

namespace ParallaxScrolling
{
    /// <summary>
    /// Represents a single parallax scrolling layer.
    /// Attach this to each background layer sprite that should scroll.
    /// </summary>
    public class ParallaxLayer : MonoBehaviour
    {
        [Header("Layer Settings")]
        [Tooltip("The speed multiplier for this layer. Lower values = slower movement (farther away). Typical range: 0.1 to 1.0")]
        [Range(0f, 2f)]
        public float parallaxSpeed = 0.5f;

        [Tooltip("Enable infinite horizontal scrolling for this layer")]
        public bool infiniteScrolling = true;

        [Tooltip("The sprite renderer for this layer (auto-assigned if not set)")]
        public SpriteRenderer spriteRenderer;

        [Header("Optional Settings")]
        [Tooltip("Enable vertical parallax (for games with vertical movement)")]
        public bool enableVerticalParallax = false;

        [Tooltip("Vertical parallax speed multiplier")]
        [Range(0f, 2f)]
        public float verticalParallaxSpeed = 0.5f;

        // Private fields
        private Transform cameraTransform;
        private Vector3 previousCameraPosition;
        private float spriteWidth;
        private float spriteHeight;

        // For infinite scrolling
        private Vector3 startPosition;

        /// <summary>
        /// Initialize the layer with a camera reference
        /// </summary>
        public void Initialize(Transform camera)
        {
            cameraTransform = camera;
            previousCameraPosition = cameraTransform.position;
            startPosition = transform.position;

            // Auto-assign sprite renderer if not set
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
            }

            // Calculate sprite dimensions for infinite scrolling
            if (spriteRenderer != null && infiniteScrolling)
            {
                spriteWidth = spriteRenderer.bounds.size.x;
                spriteHeight = spriteRenderer.bounds.size.y;
            }
        }

        /// <summary>
        /// Update the layer position based on camera movement
        /// </summary>
        public void UpdateParallax()
        {
            if (cameraTransform == null) return;

            // Calculate camera movement delta
            Vector3 deltaMovement = cameraTransform.position - previousCameraPosition;

            // Apply parallax effect (inverse of speed for proper depth effect)
            float horizontalParallax = deltaMovement.x * parallaxSpeed;
            float verticalParallax = enableVerticalParallax ? deltaMovement.y * verticalParallaxSpeed : 0f;

            // Move the layer
            transform.position += new Vector3(horizontalParallax, verticalParallax, 0f);

            // Handle infinite scrolling
            if (infiniteScrolling && spriteRenderer != null)
            {
                HandleInfiniteScrolling();
            }

            // Update previous camera position
            previousCameraPosition = cameraTransform.position;
        }

        /// <summary>
        /// Handle infinite horizontal scrolling by repositioning the sprite
        /// </summary>
        private void HandleInfiniteScrolling()
        {
            // Calculate how far the camera has moved from the start
            float distanceFromStart = cameraTransform.position.x - startPosition.x;

            // Calculate the effective parallax offset
            float parallaxOffset = distanceFromStart * parallaxSpeed;

            // Check if we need to reposition
            if (Mathf.Abs(parallaxOffset) >= spriteWidth)
            {
                // Calculate how many sprite widths we've traveled
                float offsetMultiplier = Mathf.Floor(parallaxOffset / spriteWidth);

                // Reposition the sprite
                Vector3 newPosition = transform.position;
                newPosition.x = startPosition.x + (offsetMultiplier * spriteWidth);
                transform.position = newPosition;
            }
        }

        /// <summary>
        /// Reset the layer to its starting position
        /// </summary>
        public void ResetPosition()
        {
            transform.position = startPosition;
            if (cameraTransform != null)
            {
                previousCameraPosition = cameraTransform.position;
            }
        }

        /// <summary>
        /// Update the parallax speed at runtime
        /// </summary>
        public void SetParallaxSpeed(float speed)
        {
            parallaxSpeed = Mathf.Clamp(speed, 0f, 2f);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            // Auto-assign sprite renderer in editor
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
            }
        }
#endif
    }
}
