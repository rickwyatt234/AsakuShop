using System.Collections.Generic;
using UnityEngine;
using AsakuShop.Items;

namespace AsakuShop.Storage
{
    // Generates preview sprites from 3D item models using a dedicated camera.
    // Caches results to avoid re-rendering the same item definitions.
    public class ItemPreviewManager : MonoBehaviour
    {
        public static ItemPreviewManager Instance { get; private set; }

        [Header("Preview Camera Setup")]
        [SerializeField] private Camera previewCamera;
        [SerializeField] private Transform previewAnchor;
        [SerializeField] private int textureSize = 256;
        [SerializeField] private Color backgroundColor = new Color(0, 0, 0, 0);

        [Header("Preview Settings")]
        [SerializeField] private Vector3 previewRotationEuler = new Vector3(0f, 45f, 0f);
        [SerializeField] private float previewScale = 1.5f;
        [SerializeField] private Vector3 previewOffset = Vector3.zero;

        private readonly Dictionary<ItemDefinition, Sprite> spriteCache = new();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            if (previewCamera != null)
            {
                previewCamera.clearFlags = CameraClearFlags.SolidColor;
                previewCamera.backgroundColor = backgroundColor;
                previewCamera.orthographic = true;
                previewCamera.enabled = false;
            }
        }

        public Sprite GetPreviewSprite(ItemDefinition definition)
        {
            if (definition == null || definition.WorldPrefab == null)
                return null;

            // Return cached sprite if available
            if (spriteCache.TryGetValue(definition, out var cachedSprite) && cachedSprite != null)
                return cachedSprite;

            Sprite generatedSprite = GeneratePreviewSprite(definition);
            
            if (generatedSprite != null)
                spriteCache[definition] = generatedSprite;

            return generatedSprite;
        }

        private Sprite GeneratePreviewSprite(ItemDefinition definition)
        {
            if (previewCamera == null || previewAnchor == null)
                return null;

            // Clean up any previous preview instances
            for (int i = previewAnchor.childCount - 1; i >= 0; i--)
            {
                var child = previewAnchor.GetChild(i);
                child.gameObject.SetActive(false);
                Destroy(child.gameObject);
            }

            // Create RenderTexture
            var rt = new RenderTexture(textureSize, textureSize, 16, RenderTextureFormat.ARGB32)
            {
                name = $"{definition.ItemId}_PreviewRT"
            };

            var oldTarget = previewCamera.targetTexture;
            bool wasEnabled = previewCamera.enabled;
            var oldActive = RenderTexture.active;

            previewCamera.targetTexture = rt;
            previewCamera.enabled = true;
            previewCamera.orthographic = true;

            // Instantiate item model
            GameObject instance = Instantiate(definition.WorldPrefab, previewAnchor);
            instance.transform.localPosition = previewOffset;
            instance.transform.localRotation = Quaternion.Euler(previewRotationEuler);
            instance.transform.localScale = Vector3.one * previewScale;

            // Collect renderers
            var renderers = instance.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
            {
                previewCamera.enabled = wasEnabled;
                previewCamera.targetTexture = oldTarget;
                RenderTexture.active = oldActive;
                Destroy(instance);
                rt.Release();
                Destroy(rt);
                return null;
            }

            // Calculate bounds and camera fit
            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);

            Vector3 center = bounds.center;
            float padding = 1.2f;
            float maxExtent = Mathf.Max(bounds.extents.x, bounds.extents.y, bounds.extents.z);
            if (maxExtent <= Mathf.Epsilon)
                maxExtent = 0.001f;

            previewCamera.orthographicSize = (maxExtent * padding) / previewScale;

            Vector3 camDir = new Vector3(0f, 0f, -1f);
            float camDist = maxExtent * 2f;
            previewCamera.transform.position = center + camDir * camDist;
            previewCamera.transform.LookAt(center);

            // Render
            previewCamera.Render();

            // Copy to Texture2D
            RenderTexture.active = rt;
            var tex = new Texture2D(textureSize, textureSize, TextureFormat.ARGB32, false);
            tex.ReadPixels(new Rect(0, 0, textureSize, textureSize), 0, 0);
            tex.Apply();

            // Create Sprite
            var sprite = Sprite.Create(
                tex,
                new Rect(0, 0, tex.width, tex.height),
                new Vector2(0.5f, 0.5f),
                100f
            );
            sprite.name = $"{definition.ItemId}_PreviewSprite";

            // Cleanup
            Destroy(instance);
            previewCamera.enabled = wasEnabled;
            previewCamera.targetTexture = oldTarget;
            RenderTexture.active = oldActive;
            rt.Release();
            Destroy(rt);

            return sprite;
        }

        public void ClearCache()
        {
            spriteCache.Clear();
        }
    }
}