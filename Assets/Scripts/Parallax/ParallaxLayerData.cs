using UnityEngine;

namespace ParallaxScrolling
{
    /// <summary>
    /// ScriptableObject to store parallax layer configuration data.
    /// Create instances via: Right-click in Project > Create > Parallax > Layer Data
    /// </summary>
    [CreateAssetMenu(fileName = "New Parallax Layer", menuName = "Parallax/Layer Data", order = 1)]
    public class ParallaxLayerData : ScriptableObject
    {
        [Header("Visual Settings")]
        [Tooltip("The sprite to display for this layer")]
        public Sprite layerSprite;

        [Tooltip("Sorting layer name (e.g., 'Background')")]
        public string sortingLayerName = "Default";

        [Tooltip("Order in layer (lower values render behind)")]
        public int orderInLayer = 0;

        [Header("Parallax Settings")]
        [Tooltip("Parallax speed multiplier (0 = static, 1 = moves with camera)")]
        [Range(0f, 2f)]
        public float parallaxSpeed = 0.5f;

        [Tooltip("Enable infinite horizontal scrolling")]
        public bool infiniteScrolling = true;

        [Tooltip("Enable vertical parallax")]
        public bool enableVerticalParallax = false;

        [Tooltip("Vertical parallax speed")]
        [Range(0f, 2f)]
        public float verticalParallaxSpeed = 0.5f;

        [Header("Position Settings")]
        [Tooltip("Initial Z position (for depth sorting)")]
        public float zPosition = 0f;

        /// <summary>
        /// Apply this data to a ParallaxLayer component
        /// </summary>
        public void ApplyToLayer(ParallaxLayer layer)
        {
            if (layer == null) return;

            layer.parallaxSpeed = parallaxSpeed;
            layer.infiniteScrolling = infiniteScrolling;
            layer.enableVerticalParallax = enableVerticalParallax;
            layer.verticalParallaxSpeed = verticalParallaxSpeed;

            // Apply sprite and rendering settings
            if (layer.spriteRenderer != null)
            {
                layer.spriteRenderer.sprite = layerSprite;
                layer.spriteRenderer.sortingLayerName = sortingLayerName;
                layer.spriteRenderer.sortingOrder = orderInLayer;
            }

            // Apply position
            Vector3 pos = layer.transform.position;
            pos.z = zPosition;
            layer.transform.position = pos;
        }

        /// <summary>
        /// Create a new GameObject with this layer configuration
        /// </summary>
        public GameObject CreateLayerGameObject(Transform parent = null)
        {
            GameObject layerObject = new GameObject(name);

            if (parent != null)
            {
                layerObject.transform.SetParent(parent);
            }

            // Add sprite renderer
            SpriteRenderer spriteRenderer = layerObject.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = layerSprite;
            spriteRenderer.sortingLayerName = sortingLayerName;
            spriteRenderer.sortingOrder = orderInLayer;

            // Add parallax layer component
            ParallaxLayer parallaxLayer = layerObject.AddComponent<ParallaxLayer>();
            parallaxLayer.spriteRenderer = spriteRenderer;

            // Apply settings
            ApplyToLayer(parallaxLayer);

            return layerObject;
        }
    }
}
