using UnityEngine;

namespace CameraSystem
{
    /// <summary>
    /// Makes the camera smoothly follow a target object.
    /// Attach this to your Camera GameObject and assign a target.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class CameraFollowTarget : MonoBehaviour
    {
        [Header("Target Settings")]
        [Tooltip("The target to follow (e.g., Player)")]
        [SerializeField] private Transform target;

        [Tooltip("Offset from target position")]
        [SerializeField] private Vector3 offset = new Vector3(0f, 0f, -10f);

        [Header("Follow Settings")]
        [Tooltip("Follow smoothing (0 = instant, higher = smoother)")]
        [Range(0f, 1f)]
        [SerializeField] private float smoothTime = 0.3f;

        [Tooltip("Follow on X axis")]
        [SerializeField] private bool followX = true;

        [Tooltip("Follow on Y axis")]
        [SerializeField] private bool followY = true;

        [Header("Look Ahead")]
        [Tooltip("Enable look ahead based on target velocity")]
        [SerializeField] private bool enableLookAhead = false;

        [Tooltip("Look ahead distance multiplier")]
        [SerializeField] private float lookAheadDistance = 2f;

        [Tooltip("Look ahead smoothing")]
        [Range(0f, 1f)]
        [SerializeField] private float lookAheadSmoothing = 0.5f;

        [Header("Dead Zone")]
        [Tooltip("Enable dead zone (camera only moves when target leaves zone)")]
        [SerializeField] private bool enableDeadZone = false;

        [Tooltip("Dead zone width")]
        [SerializeField] private float deadZoneWidth = 2f;

        [Tooltip("Dead zone height")]
        [SerializeField] private float deadZoneHeight = 2f;

        // Private variables
        private Camera cam;
        private Vector3 velocity = Vector3.zero;
        private Vector3 lookAheadPos = Vector3.zero;
        private Vector3 lastTargetPosition;

        private void Awake()
        {
            cam = GetComponent<Camera>();
        }

        private void Start()
        {
            if (target != null)
            {
                lastTargetPosition = target.position;

                // Initialize camera position
                Vector3 desiredPosition = target.position + offset;
                transform.position = new Vector3(
                    followX ? desiredPosition.x : transform.position.x,
                    followY ? desiredPosition.y : transform.position.y,
                    offset.z
                );
            }
        }

        private void LateUpdate()
        {
            if (target == null) return;

            FollowTarget();
        }

        /// <summary>
        /// Main follow logic
        /// </summary>
        private void FollowTarget()
        {
            Vector3 targetPosition = target.position;

            // Calculate look ahead
            Vector3 lookAhead = Vector3.zero;
            if (enableLookAhead)
            {
                Vector3 targetVelocity = (targetPosition - lastTargetPosition) / Time.deltaTime;
                Vector3 desiredLookAhead = targetVelocity.normalized * lookAheadDistance;
                lookAheadPos = Vector3.Lerp(lookAheadPos, desiredLookAhead, lookAheadSmoothing);
                lookAhead = lookAheadPos;
            }

            // Calculate desired position
            Vector3 desiredPosition = targetPosition + offset + lookAhead;

            // Apply dead zone
            if (enableDeadZone)
            {
                Vector3 currentPos = transform.position;
                float deltaX = desiredPosition.x - currentPos.x;
                float deltaY = desiredPosition.y - currentPos.y;

                // Only move if outside dead zone
                if (Mathf.Abs(deltaX) > deadZoneWidth / 2f)
                {
                    desiredPosition.x = currentPos.x + (deltaX - Mathf.Sign(deltaX) * deadZoneWidth / 2f);
                }
                else
                {
                    desiredPosition.x = currentPos.x;
                }

                if (Mathf.Abs(deltaY) > deadZoneHeight / 2f)
                {
                    desiredPosition.y = currentPos.y + (deltaY - Mathf.Sign(deltaY) * deadZoneHeight / 2f);
                }
                else
                {
                    desiredPosition.y = currentPos.y;
                }
            }

            // Apply axis constraints
            if (!followX) desiredPosition.x = transform.position.x;
            if (!followY) desiredPosition.y = transform.position.y;

            // Always preserve Z from offset
            desiredPosition.z = offset.z;

            // Apply smooth follow
            Vector3 smoothPosition = Vector3.SmoothDamp(
                transform.position,
                desiredPosition,
                ref velocity,
                smoothTime
            );

            transform.position = smoothPosition;

            // Update last target position
            lastTargetPosition = targetPosition;
        }

        /// <summary>
        /// Set the target to follow
        /// </summary>
        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
            if (newTarget != null)
            {
                lastTargetPosition = newTarget.position;
            }
        }

        /// <summary>
        /// Set follow offset
        /// </summary>
        public void SetOffset(Vector3 newOffset)
        {
            offset = newOffset;
        }

        /// <summary>
        /// Set smoothing time
        /// </summary>
        public void SetSmoothTime(float time)
        {
            smoothTime = Mathf.Clamp01(time);
        }

        /// <summary>
        /// Enable or disable following
        /// </summary>
        public void SetEnabled(bool enabled)
        {
            this.enabled = enabled;
        }

        /// <summary>
        /// Snap camera to target position instantly (no smoothing)
        /// </summary>
        public void SnapToTarget()
        {
            if (target == null) return;

            Vector3 desiredPosition = target.position + offset;
            transform.position = new Vector3(
                followX ? desiredPosition.x : transform.position.x,
                followY ? desiredPosition.y : transform.position.y,
                offset.z
            );

            velocity = Vector3.zero;
            lookAheadPos = Vector3.zero;
            lastTargetPosition = target.position;
        }

#if UNITY_EDITOR
        /// <summary>
        /// Draw dead zone and offset in editor
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            if (target != null)
            {
                // Draw connection to target
                Gizmos.color = Color.cyan;
                Gizmos.DrawLine(transform.position, target.position);

                // Draw offset position
                Gizmos.color = Color.green;
                Vector3 offsetPos = target.position + new Vector3(offset.x, offset.y, 0f);
                Gizmos.DrawWireSphere(offsetPos, 0.3f);

                // Draw dead zone
                if (enableDeadZone)
                {
                    Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
                    Gizmos.DrawWireCube(transform.position, new Vector3(deadZoneWidth, deadZoneHeight, 0.1f));
                }
            }
        }
#endif
    }
}
