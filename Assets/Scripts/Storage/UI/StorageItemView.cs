using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

namespace AsakuShop.Storage
{
    /// <summary>
    /// Draggable item view in the inventory UI.
    /// Displays a preview sprite and allows dragging within inventory bounds.
    /// </summary>
    public class StorageItemView : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public StorageItemEntry Entry { get; private set; }
        public RectTransform RectTransform { get; private set; }

        [SerializeField] private Image itemImage;
        [SerializeField] private TextMeshProUGUI itemNameText;

        private CanvasGroup canvasGroup;
        private Canvas rootCanvas;
        private RectTransform inventoryRect;
        private Vector2 originalLocalPos;
        private Vector2 dragOffset;
        private StorageInventoryUI inventoryUI;

        private void Awake()
        {
            RectTransform = GetComponent<RectTransform>();
            canvasGroup = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();
        }

        public void Initialize(StorageItemEntry entry, StorageInventoryUI controller)
        {
            Entry = entry;
            inventoryUI = controller;

            rootCanvas = GetComponentInParent<Canvas>();
            inventoryRect = rootCanvas.GetComponent<RectTransform>();

            // Set initial position
            RectTransform.anchoredPosition = entry.uiPosition;

            // Display item name
            if (itemNameText != null && entry.itemInstance != null)
                itemNameText.text = entry.itemInstance.Definition.DisplayName;

            // Display preview sprite
            if (itemImage != null && entry.itemInstance != null && ItemPreviewManager.Instance != null)
            {
                var sprite = ItemPreviewManager.Instance.GetPreviewSprite(entry.itemInstance.Definition);
                if (sprite != null)
                {
                    itemImage.sprite = sprite;
                    itemImage.preserveAspect = true;
                    itemImage.alphaHitTestMinimumThreshold = 0.1f;
                }
            }
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            originalLocalPos = RectTransform.anchoredPosition;
            canvasGroup.alpha = 0.7f;
            canvasGroup.blocksRaycasts = false;
            RectTransform.SetAsLastSibling();

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    inventoryRect,
                    eventData.position,
                    rootCanvas.worldCamera,
                    out var mouseInInventory))
            {
                dragOffset = RectTransform.anchoredPosition - mouseInInventory;
            }
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    inventoryRect,
                    eventData.position,
                    rootCanvas.worldCamera,
                    out var mouseInInventory))
            {
                RectTransform.anchoredPosition = mouseInInventory + dragOffset;
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = true;

            // Check if item is still within inventory bounds
            if (IsWithinInventoryBounds(RectTransform.anchoredPosition))
            {
                // Update position in inventory
                inventoryUI.UpdateItemPosition(Entry, RectTransform.anchoredPosition);
            }
            else
            {
                // Drop item into world
                inventoryUI.DropItemToWorld(Entry);
                Destroy(gameObject); // Remove from UI
            }
        }

        private bool IsWithinInventoryBounds(Vector2 localPos)
        {
            if (inventoryRect == null)
                return false;

            return RectTransformUtility.RectangleContainsScreenPoint(
                inventoryRect,
                RectTransformUtility.WorldToScreenPoint(rootCanvas.worldCamera, 
                    inventoryRect.TransformPoint(localPos)),
                rootCanvas.worldCamera
            );
        }
    }
}