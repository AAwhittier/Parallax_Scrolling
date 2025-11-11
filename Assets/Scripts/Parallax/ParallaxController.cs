using UnityEngine;
using System.Collections.Generic;

namespace ParallaxScrolling
{
    /// <summary>
    /// Controls and manages all parallax layers in the scene.
    /// Attach this to an empty GameObject in your scene.
    /// </summary>
    public class ParallaxController : MonoBehaviour
    {
        [Header("Camera Reference")]
        [Tooltip("The camera that the parallax effect follows. Usually the main camera.")]
        public Camera targetCamera;

        [Header("Parallax Layers")]
        [Tooltip("List of all parallax layers. Add/remove layers here. Order matters: first = furthest back.")]
        public List<ParallaxLayer> parallaxLayers = new List<ParallaxLayer>();

        [Header("Update Settings")]
        [Tooltip("Update method for parallax. LateUpdate is recommended for smooth camera following.")]
        public UpdateMethod updateMethod = UpdateMethod.LateUpdate;

        [Tooltip("Enable to automatically find all ParallaxLayer components in children")]
        public bool autoDetectLayers = true;

        public enum UpdateMethod
        {
            Update,
            LateUpdate,
            FixedUpdate
        }

        private void Start()
        {
            InitializeParallax();
        }

        /// <summary>
        /// Initialize all parallax layers
        /// </summary>
        private void InitializeParallax()
        {
            // Auto-assign main camera if not set
            if (targetCamera == null)
            {
                targetCamera = Camera.main;
                if (targetCamera == null)
                {
                    Debug.LogError("ParallaxController: No camera assigned and no main camera found!");
                    return;
                }
            }

            // Auto-detect layers if enabled
            if (autoDetectLayers)
            {
                AutoDetectParallaxLayers();
            }

            // Initialize each layer
            foreach (ParallaxLayer layer in parallaxLayers)
            {
                if (layer != null)
                {
                    layer.Initialize(targetCamera.transform);
                }
            }

            Debug.Log($"ParallaxController: Initialized {parallaxLayers.Count} parallax layers");
        }

        /// <summary>
        /// Automatically find and add all ParallaxLayer components in children
        /// </summary>
        private void AutoDetectParallaxLayers()
        {
            ParallaxLayer[] foundLayers = GetComponentsInChildren<ParallaxLayer>();

            if (foundLayers.Length > 0)
            {
                parallaxLayers.Clear();
                parallaxLayers.AddRange(foundLayers);
                Debug.Log($"ParallaxController: Auto-detected {foundLayers.Length} parallax layers");
            }
        }

        /// <summary>
        /// Add a new parallax layer at runtime
        /// </summary>
        public void AddLayer(ParallaxLayer layer)
        {
            if (layer != null && !parallaxLayers.Contains(layer))
            {
                parallaxLayers.Add(layer);
                layer.Initialize(targetCamera.transform);
                Debug.Log($"ParallaxController: Added layer '{layer.gameObject.name}'");
            }
        }

        /// <summary>
        /// Remove a parallax layer at runtime
        /// </summary>
        public void RemoveLayer(ParallaxLayer layer)
        {
            if (layer != null && parallaxLayers.Contains(layer))
            {
                parallaxLayers.Remove(layer);
                Debug.Log($"ParallaxController: Removed layer '{layer.gameObject.name}'");
            }
        }

        /// <summary>
        /// Remove a layer by index
        /// </summary>
        public void RemoveLayerAt(int index)
        {
            if (index >= 0 && index < parallaxLayers.Count)
            {
                ParallaxLayer layer = parallaxLayers[index];
                parallaxLayers.RemoveAt(index);
                Debug.Log($"ParallaxController: Removed layer at index {index}");
            }
        }

        /// <summary>
        /// Clear all parallax layers
        /// </summary>
        public void ClearLayers()
        {
            parallaxLayers.Clear();
            Debug.Log("ParallaxController: Cleared all layers");
        }

        /// <summary>
        /// Reset all layers to their starting positions
        /// </summary>
        public void ResetAllLayers()
        {
            foreach (ParallaxLayer layer in parallaxLayers)
            {
                if (layer != null)
                {
                    layer.ResetPosition();
                }
            }
        }

        /// <summary>
        /// Enable or disable all parallax layers
        /// </summary>
        public void SetParallaxEnabled(bool enabled)
        {
            foreach (ParallaxLayer layer in parallaxLayers)
            {
                if (layer != null)
                {
                    layer.enabled = enabled;
                }
            }
        }

        /// <summary>
        /// Update all parallax layers
        /// </summary>
        private void UpdateAllLayers()
        {
            foreach (ParallaxLayer layer in parallaxLayers)
            {
                if (layer != null && layer.enabled)
                {
                    layer.UpdateParallax();
                }
            }
        }

        private void Update()
        {
            if (updateMethod == UpdateMethod.Update)
            {
                UpdateAllLayers();
            }
        }

        private void LateUpdate()
        {
            if (updateMethod == UpdateMethod.LateUpdate)
            {
                UpdateAllLayers();
            }
        }

        private void FixedUpdate()
        {
            if (updateMethod == UpdateMethod.FixedUpdate)
            {
                UpdateAllLayers();
            }
        }

#if UNITY_EDITOR
        /// <summary>
        /// Refresh layers in editor
        /// </summary>
        [ContextMenu("Refresh Layers")]
        private void RefreshLayers()
        {
            if (autoDetectLayers)
            {
                AutoDetectParallaxLayers();
            }
        }

        private void OnValidate()
        {
            // Auto-assign camera in editor
            if (targetCamera == null)
            {
                targetCamera = Camera.main;
            }
        }
#endif
    }
}
