using UnityEngine;
using System.Collections;

namespace CameraSystem
{
    /// <summary>
    /// Provides camera shake effects for impacts, explosions, etc.
    /// Attach this to your Camera GameObject and call Shake() methods from other scripts.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class CameraShake : MonoBehaviour
    {
        [Header("Shake Settings")]
        [Tooltip("Default shake duration in seconds")]
        [SerializeField] private float defaultDuration = 0.5f;

        [Tooltip("Default shake intensity")]
        [SerializeField] private float defaultIntensity = 0.3f;

        [Tooltip("Shake reduction per second (higher = faster decay)")]
        [SerializeField] private float shakeDecay = 2f;

        [Header("Advanced Settings")]
        [Tooltip("Use trauma-based shake (more realistic)")]
        [SerializeField] private bool useTraumaShake = true;

        [Tooltip("Trauma decay rate")]
        [SerializeField] private float traumaDecay = 1.5f;

        // Private variables
        private Vector3 originalPosition;
        private float trauma = 0f;
        private float currentShakeIntensity = 0f;
        private float shakeTimer = 0f;
        private bool isShaking = false;

        private void Awake()
        {
            originalPosition = transform.localPosition;
        }

        private void Update()
        {
            if (useTraumaShake)
            {
                UpdateTraumaShake();
            }
        }

        private void LateUpdate()
        {
            if (isShaking && !useTraumaShake)
            {
                UpdateBasicShake();
            }
        }

        /// <summary>
        /// Basic shake implementation
        /// </summary>
        private void UpdateBasicShake()
        {
            if (shakeTimer > 0f)
            {
                // Random shake offset
                Vector3 shakeOffset = Random.insideUnitCircle * currentShakeIntensity;
                transform.localPosition = originalPosition + shakeOffset;

                // Decay shake over time
                currentShakeIntensity -= shakeDecay * Time.deltaTime;
                shakeTimer -= Time.deltaTime;

                if (shakeTimer <= 0f)
                {
                    StopShake();
                }
            }
        }

        /// <summary>
        /// Trauma-based shake (more realistic)
        /// </summary>
        private void UpdateTraumaShake()
        {
            if (trauma > 0f)
            {
                // Calculate shake intensity from trauma (squared for better feel)
                float shake = trauma * trauma;

                // Apply shake offset
                float offsetX = Mathf.PerlinNoise(Time.time * 25f, 0f) * 2f - 1f;
                float offsetY = Mathf.PerlinNoise(0f, Time.time * 25f) * 2f - 1f;

                Vector3 shakeOffset = new Vector3(offsetX, offsetY, 0f) * shake * currentShakeIntensity;
                transform.localPosition = originalPosition + shakeOffset;

                // Decay trauma over time
                trauma = Mathf.Max(0f, trauma - traumaDecay * Time.deltaTime);

                if (trauma <= 0f)
                {
                    StopShake();
                }
            }
        }

        /// <summary>
        /// Trigger a camera shake with default settings
        /// </summary>
        public void Shake()
        {
            Shake(defaultDuration, defaultIntensity);
        }

        /// <summary>
        /// Trigger a camera shake with custom intensity
        /// </summary>
        public void Shake(float intensity)
        {
            Shake(defaultDuration, intensity);
        }

        /// <summary>
        /// Trigger a camera shake with custom duration and intensity
        /// </summary>
        public void Shake(float duration, float intensity)
        {
            if (useTraumaShake)
            {
                // Add trauma (clamped to 1.0)
                trauma = Mathf.Min(1f, trauma + intensity);
                currentShakeIntensity = intensity;
            }
            else
            {
                shakeTimer = duration;
                currentShakeIntensity = intensity;
            }

            isShaking = true;
            originalPosition = transform.localPosition;
        }

        /// <summary>
        /// Stop the shake immediately
        /// </summary>
        public void StopShake()
        {
            isShaking = false;
            shakeTimer = 0f;
            trauma = 0f;
            transform.localPosition = originalPosition;
        }

        /// <summary>
        /// Add trauma to the shake (for trauma-based shake)
        /// </summary>
        public void AddTrauma(float amount)
        {
            if (useTraumaShake)
            {
                trauma = Mathf.Min(1f, trauma + amount);
                currentShakeIntensity = Mathf.Max(currentShakeIntensity, amount);
                isShaking = true;
            }
        }

        /// <summary>
        /// Shake with a coroutine (alternative method)
        /// </summary>
        public void ShakeCoroutine(float duration, float intensity)
        {
            StartCoroutine(ShakeRoutine(duration, intensity));
        }

        /// <summary>
        /// Coroutine for shake effect
        /// </summary>
        private IEnumerator ShakeRoutine(float duration, float intensity)
        {
            Vector3 startPosition = transform.localPosition;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                float progress = elapsed / duration;
                float currentIntensity = intensity * (1f - progress);

                Vector3 offset = Random.insideUnitCircle * currentIntensity;
                transform.localPosition = startPosition + offset;

                elapsed += Time.deltaTime;
                yield return null;
            }

            transform.localPosition = startPosition;
        }

        /// <summary>
        /// Quick preset shakes for common scenarios
        /// </summary>
        public void ShakeLight()
        {
            Shake(0.2f, 0.1f);
        }

        public void ShakeMedium()
        {
            Shake(0.4f, 0.3f);
        }

        public void ShakeHeavy()
        {
            Shake(0.6f, 0.6f);
        }

        /// <summary>
        /// Check if currently shaking
        /// </summary>
        public bool IsShaking()
        {
            return isShaking;
        }

        /// <summary>
        /// Get current trauma level (0-1)
        /// </summary>
        public float GetTrauma()
        {
            return trauma;
        }
    }
}
