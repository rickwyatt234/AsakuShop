using System.Collections.Generic;
using UnityEngine;
using AsakuShop.Items;

namespace AsakuShop.Storage
{
    public class ItemPreviewManager : MonoBehaviour
    {
        public static ItemPreviewManager Instance { get; private set; }

        [Header("Preview Camera Setup")]
        [SerializeField] private Camera previewCamera;
        [SerializeField] private Transform previewAnchor;
        [SerializeField] private int textureSize = 2048;
        [Header("Preview Settings")]
        [SerializeField] private Vector3 previewRotationEuler = new Vector3(20f, 45f, 0f);
        [SerializeField] private float previewScale = 2.0f;
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
                previewCamera.backgroundColor = Color.black;
                previewCamera.orthographic = true;
                previewCamera.enabled = false;
                previewCamera.cullingMask = LayerMask.GetMask("Item");
                previewCamera.allowHDR = false; 
                previewCamera.allowMSAA = false;
            }
        }

        public Sprite GetPreviewSprite(ItemDefinition definition)
        {
            if (definition == null || definition.WorldPrefab == null)
            {
                return null;
            }

            // Return cached sprite if available
            if (spriteCache.TryGetValue(definition, out var cachedSprite) && cachedSprite != null)
            {
                return cachedSprite;
            }

            Sprite generatedSprite = GeneratePreviewSprite(definition);
            
            if (generatedSprite != null)
            {
                spriteCache[definition] = generatedSprite;
            }

            return generatedSprite;
        }

        private Sprite GeneratePreviewSprite(ItemDefinition definition)
        {
            if (previewCamera == null || previewAnchor == null)
            {
                return null;
            }

            // Clean up any previous preview instances
            for (int i = previewAnchor.childCount - 1; i >= 0; i--)
            {
                var child = previewAnchor.GetChild(i);
                Destroy(child.gameObject);
            }

            // Create RenderTexture
            var rt = new RenderTexture(textureSize, textureSize, 16, RenderTextureFormat.ARGB32)
            {
                name = $"{definition.ItemId}_PreviewRT",
                filterMode = FilterMode.Bilinear,
                antiAliasing = 1
            };

            var oldTarget = previewCamera.targetTexture;
            bool wasEnabled = previewCamera.enabled;
            var oldActive = RenderTexture.active;

            try
            {
                previewCamera.targetTexture = rt;
                previewCamera.enabled = true;
                previewCamera.orthographic = true;

                // Instantiate item model
                GameObject instance = Instantiate(definition.WorldPrefab, previewAnchor);
                instance.transform.localPosition = previewOffset;
                instance.transform.localRotation = Quaternion.Euler(previewRotationEuler);
                instance.transform.localScale = Vector3.one * previewScale;

                // Force all child objects to Item layer to prevent world geometry from showing
                int itemLayer = LayerMask.NameToLayer("Item");
                foreach (var renderer in instance.GetComponentsInChildren<Renderer>())
                {
                    renderer.gameObject.layer = itemLayer;
                }

                // Collect renderers
                var renderers = instance.GetComponentsInChildren<Renderer>();

                if (renderers.Length == 0)
                {
                    Destroy(instance);
                    return null;
                }

                // Disable shadow casting
                for (int i = 0; i < renderers.Length; i++)
                {
                    renderers[i].shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                }

                // Calculate bounds
                Bounds bounds = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++)
                    bounds.Encapsulate(renderers[i].bounds);

                Vector3 center = bounds.center;
                float padding = 1.01f; 
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
                tex.filterMode = FilterMode.Bilinear;
                tex.ReadPixels(new Rect(0, 0, textureSize, textureSize), 0, 0);
                tex.Apply(false);

                // Remove black background with loose tolerance
                Color[] pixels = tex.GetPixels();
                Color black = new Color(0f, 0f, 0f, 1f);
                for (int i = 0; i < pixels.Length; i++)
                {
                    // Increase tolerance to 0.1f to ensure we catch black pixels
                    if (IsColorSimilar(pixels[i], black, 0.1f))
                    {
                        pixels[i].a = 0f;
                    }
                }
                tex.SetPixels(pixels);
                tex.Apply(false);

                
                // Create Sprite
                var sprite = Sprite.Create(
                    tex,
                    new Rect(0, 0, tex.width, tex.height),
                    new Vector2(0.5f, 0.5f),
                    100f
                );
                sprite.name = $"{definition.ItemId}_PreviewSprite";

                Destroy(instance);
                return sprite;
            }
            finally
            {
                previewCamera.enabled = wasEnabled;
                previewCamera.targetTexture = oldTarget;
                RenderTexture.active = oldActive;
                rt.Release();
                Destroy(rt);
            }
        }

        private bool IsColorSimilar(Color a, Color b, float tolerance)
        {
            return Mathf.Abs(a.r - b.r) < tolerance &&
                Mathf.Abs(a.g - b.g) < tolerance &&
                Mathf.Abs(a.b - b.b) < tolerance;
        }
        public void ClearCache()
        {
            spriteCache.Clear();
        }
    }
}