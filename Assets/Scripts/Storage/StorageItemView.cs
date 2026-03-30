using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using AsakuShop.UI;
using TMPro;

namespace AsakuShop.Storage
{
    public class StorageItemView : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerEnterHandler, IPointerExitHandler
    {
        public StorageItemEntry Entry { get; private set; }
        public RectTransform RectTransform { get; private set; }

        [SerializeField] private Image itemImage;

        private CanvasGroup canvasGroup;
        private Canvas rootCanvas;
        private RectTransform inventoryRect;
        private Rect containerBounds;
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
            InitializeWithCanvas(entry, controller, GetComponentInParent<Canvas>(), Rect.zero);
        }

        public void InitializeWithCanvas(StorageItemEntry entry, StorageInventoryUI controller, Canvas parentCanvas, Rect containerBounds)
        {
            Entry = entry;
            inventoryUI = controller;
            this.containerBounds = containerBounds;

            rootCanvas = parentCanvas;
            
            if (rootCanvas == null)
            {
                return;
            }

            inventoryRect = rootCanvas.GetComponent<RectTransform>();
            
            if (inventoryRect == null)
            {
                return;
            }

            RectTransform.anchoredPosition = entry.uiPosition;

             // Load and set preview sprite
             if (itemImage != null && entry.itemInstance != null && entry.itemInstance.Definition != null)
             {
                 var sprite = ItemPreviewManager.Instance.GetPreviewSprite(entry.itemInstance.Definition);
                 
                if (sprite != null)
                {
                    itemImage.sprite = sprite;
                    itemImage.preserveAspect = true;
                    itemImage.alphaHitTestMinimumThreshold = 0.1f;
                    itemImage.rectTransform.localRotation = Quaternion.Euler(0, 0, 180);
                }
                 else
                 {
                     itemImage.color = new Color(0.5f, 0.5f, 0.5f, 1f);
                 }
             }
        }


#region Drag Handlers
        public void OnBeginDrag(PointerEventData eventData)
        {
            // FIX: Add null checks before using these
            if (rootCanvas == null || inventoryRect == null)
            {
                return;
            }

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
            // FIX: Add null checks
            if (rootCanvas == null || inventoryRect == null)
            {
                return;
            }

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
                // Try to stock on a shelf first; fall back to world drop
                if (!inventoryUI.TryDropItemOnShelf(Entry))
                {
                    inventoryUI.DropItemToWorld(Entry);
                }
                Destroy(gameObject);
            }
        }
#endregion


#region Hover Handlers
        public void OnPointerEnter(PointerEventData eventData)
        {
            if (Entry.itemInstance != null && ItemHoverDisplay.Instance != null)
            {
                ItemHoverDisplay.Instance.ShowLabel(Entry.itemInstance, showDetails: true);
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (ItemHoverDisplay.Instance != null)
            {
                ItemHoverDisplay.Instance.HideLabel();
            }
        }

        private bool IsWithinInventoryBounds(Vector2 localPos)
        {
            if (containerBounds == Rect.zero)
            {
                return true;
            }

            // Check if position is within container bounds
            bool isInside = containerBounds.Contains(localPos);
            
            return isInside;
        }
#endregion
    }
}