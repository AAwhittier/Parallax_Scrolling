using UnityEngine;

namespace ParallaxScrolling
{
    /// <summary>
    /// Helper utilities for setting up parallax scrolling systems.
    /// </summary>
    public static class ParallaxHelper
    {
        /// <summary>
        /// Create a complete parallax system in the scene
        /// </summary>
        public static GameObject CreateParallaxSystem(string name = "Parallax System")
        {
            GameObject systemObject = new GameObject(name);
            ParallaxController controller = systemObject.AddComponent<ParallaxController>();

            Debug.Log($"Created parallax system: {name}");
            return systemObject;
        }

        /// <summary>
        /// Create a parallax layer with a sprite
        /// </summary>
        public static GameObject CreateParallaxLayer(string layerName, Sprite sprite, float parallaxSpeed, Transform parent = null)
        {
            GameObject layerObject = new GameObject(layerName);

            if (parent != null)
            {
                layerObject.transform.SetParent(parent);
            }

            // Add sprite renderer
            SpriteRenderer spriteRenderer = layerObject.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = sprite;

            // Add parallax layer
            ParallaxLayer parallaxLayer = layerObject.AddComponent<ParallaxLayer>();
            parallaxLayer.spriteRenderer = spriteRenderer;
            parallaxLayer.parallaxSpeed = parallaxSpeed;

            return layerObject;
        }

        /// <summary>
        /// Setup typical layering for a 2D sidescroller (MapleStory style)
        /// </summary>
        public static void SetupTypicalLayerDepths(ParallaxController controller)
        {
            if (controller == null || controller.parallaxLayers.Count == 0)
            {
                Debug.LogWarning("ParallaxHelper: No controller or layers to setup");
                return;
            }

            int layerCount = controller.parallaxLayers.Count;

            for (int i = 0; i < layerCount; i++)
            {
                ParallaxLayer layer = controller.parallaxLayers[i];
                if (layer == null) continue;

                // Calculate depth: furthest layer has lowest parallax speed
                // Layer 0 (back) = 0.1, Layer 1 = 0.3, Layer 2 = 0.5, etc.
                float normalizedDepth = (float)i / (layerCount - 1);
                layer.parallaxSpeed = Mathf.Lerp(0.1f, 0.9f, normalizedDepth);

                // Set Z position for proper depth sorting
                Vector3 pos = layer.transform.position;
                pos.z = i * 0.1f;
                layer.transform.position = pos;

                // Update sorting order
                if (layer.spriteRenderer != null)
                {
                    layer.spriteRenderer.sortingOrder = -layerCount + i;
                }

                Debug.Log($"Setup layer {i}: {layer.gameObject.name} - Speed: {layer.parallaxSpeed:F2}");
            }
        }

        /// <summary>
        /// Calculate recommended parallax speed based on visual distance
        /// </summary>
        /// <param name="visualDistance">0 = very far (sky), 1 = close to camera</param>
        public static float CalculateParallaxSpeed(float visualDistance)
        {
            // Closer objects move faster relative to camera
            return Mathf.Lerp(0.05f, 0.95f, Mathf.Clamp01(visualDistance));
        }

        /// <summary>
        /// Auto-tile a sprite for infinite scrolling
        /// </summary>
        public static void SetupInfiniteScrollingSprite(SpriteRenderer spriteRenderer, int tileCount = 3)
        {
            if (spriteRenderer == null) return;

            // Create parent container
            GameObject container = new GameObject($"{spriteRenderer.gameObject.name}_Container");
            Transform parent = spriteRenderer.transform.parent;

            container.transform.SetParent(parent);
            container.transform.position = spriteRenderer.transform.position;

            // Move original sprite to container
            spriteRenderer.transform.SetParent(container.transform);
            spriteRenderer.transform.localPosition = Vector3.zero;

            // Get sprite width
            float spriteWidth = spriteRenderer.bounds.size.x;

            // Create duplicates for seamless tiling
            for (int i = 1; i < tileCount; i++)
            {
                GameObject duplicate = Object.Instantiate(spriteRenderer.gameObject, container.transform);
                duplicate.name = $"{spriteRenderer.gameObject.name}_Tile{i}";
                duplicate.transform.localPosition = new Vector3(spriteWidth * i, 0, 0);
            }

            // Move parallax layer to container
            ParallaxLayer layer = spriteRenderer.GetComponent<ParallaxLayer>();
            if (layer != null)
            {
                ParallaxLayer containerLayer = container.AddComponent<ParallaxLayer>();
                containerLayer.parallaxSpeed = layer.parallaxSpeed;
                containerLayer.infiniteScrolling = layer.infiniteScrolling;
                containerLayer.enableVerticalParallax = layer.enableVerticalParallax;
                containerLayer.verticalParallaxSpeed = layer.verticalParallaxSpeed;

                Object.DestroyImmediate(layer);
            }
        }
    }
}
