using UnityEngine;

namespace SimpleNetworking.Authoritative
{
    /// <summary>
    /// Smoothly interpolates remote entity movement between snapshots
    /// </summary>
    public class EntityInterpolator : MonoBehaviour
    {
        [Header("Settings")]
        [Tooltip("Interpolation speed")]
        public float interpolationSpeed = 10f;

        [Tooltip("Use extrapolation when no recent snapshots")]
        public bool useExtrapolation = true;

        [Tooltip("Maximum extrapolation time")]
        public float maxExtrapolationTime = 0.2f;

        [Header("Status")]
        [SerializeField]
        private float timeSinceLastSnapshot = 0f;

        [SerializeField]
        private bool isInterpolating = false;

        // Interpolation state
        private Vector2 fromPosition;
        private Vector2 toPosition;
        private Vector2 lastVelocity;
        private float interpolationTime;
        private float interpolationProgress;

        // Animation
        private int facingDirection = 1;
        private string currentAnimation;

        void Update()
        {
            if (isInterpolating)
            {
                UpdateInterpolation();
            }
            else if (useExtrapolation && timeSinceLastSnapshot < maxExtrapolationTime)
            {
                // Extrapolate using last known velocity
                Vector2 extrapolatedPos = (Vector2)transform.position + lastVelocity * Time.deltaTime;
                transform.position = new Vector3(extrapolatedPos.x, extrapolatedPos.y, transform.position.z);
            }

            timeSinceLastSnapshot += Time.deltaTime;
        }

        /// <summary>
        /// Update interpolation
        /// </summary>
        private void UpdateInterpolation()
        {
            interpolationProgress += Time.deltaTime;

            float t = Mathf.Clamp01(interpolationProgress / interpolationTime);

            // Smooth interpolation
            Vector2 currentPos = Vector2.Lerp(fromPosition, toPosition, t);

            transform.position = new Vector3(currentPos.x, currentPos.y, transform.position.z);

            // Update facing direction based on movement
            if (Mathf.Abs(lastVelocity.x) > 0.1f)
            {
                facingDirection = lastVelocity.x > 0 ? 1 : -1;
                UpdateVisualFacing();
            }

            // Done interpolating
            if (t >= 1f)
            {
                isInterpolating = false;
            }
        }

        /// <summary>
        /// Receive snapshot and start interpolation
        /// </summary>
        public void OnSnapshot(EntitySnapshot snapshot)
        {
            Vector2 newPosition = snapshot.GetPosition();
            Vector2 newVelocity = snapshot.GetVelocity();

            // Start interpolation from current position
            fromPosition = transform.position;
            toPosition = newPosition;
            lastVelocity = newVelocity;

            // Reset interpolation
            interpolationProgress = 0f;
            interpolationTime = timeSinceLastSnapshot;
            if (interpolationTime < 0.016f) interpolationTime = 0.016f; // Min 1 frame

            isInterpolating = true;
            timeSinceLastSnapshot = 0f;

            // Update animation
            facingDirection = snapshot.facing;
            currentAnimation = snapshot.anim;
            UpdateVisualFacing();
        }

        /// <summary>
        /// Teleport to position (no interpolation)
        /// </summary>
        public void TeleportTo(Vector2 position)
        {
            transform.position = new Vector3(position.x, position.y, transform.position.z);
            fromPosition = position;
            toPosition = position;
            isInterpolating = false;
        }

        /// <summary>
        /// Update visual facing direction
        /// </summary>
        private void UpdateVisualFacing()
        {
            // Flip sprite based on facing direction
            Vector3 scale = transform.localScale;
            scale.x = Mathf.Abs(scale.x) * facingDirection;
            transform.localScale = scale;
        }

        /// <summary>
        /// Get current animation
        /// </summary>
        public string GetCurrentAnimation()
        {
            return currentAnimation;
        }

        /// <summary>
        /// Get facing direction
        /// </summary>
        public int GetFacingDirection()
        {
            return facingDirection;
        }
    }
}
