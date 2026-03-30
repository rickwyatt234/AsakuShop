using UnityEngine;
using UnityEngine.UI;
using AsakuShop.Items;
using AsakuShop.Core;
using DG.Tweening;
using System.Linq;
using System.Collections.Generic;
using TMPro;

namespace AsakuShop.Storage
{
    public class StorageInventoryUI : MonoBehaviour
    {
        public static StorageInventoryUI Instance { get; private set; }

        [SerializeField] private Canvas inventoryCanvas;
        [SerializeField] private RectTransform itemContainer;
        [SerializeField] private GameObject itemViewPrefab;
        [SerializeField] private TextMeshProUGUI weightCapacityText;
        [SerializeField] private TextMeshProUGUI containerNameText;
        [SerializeField] private Button closeButton;
        [SerializeField] private CanvasGroup crosshairCanvasGroup;

        private StorageContainer currentContainer;
        private Transform playerCamera;

        // Shelf highlight state
        private Shelf highlightedShelf;
        private Dictionary<Renderer, Color> originalColors = new();
        private static readonly Color shelfHighlightColor = new Color(0.3f, 1f, 0.3f, 1f);

        private void Awake()
        {
            Instance = this;
            
            playerCamera = Camera.main.transform;

            if (closeButton != null)
                closeButton.onClick.AddListener(CloseInventory);

            if (inventoryCanvas != null)
                inventoryCanvas.gameObject.SetActive(false);

            CoreEvents.OnInventoryOpenRequested    += HandleInventoryOpenRequested;
            CoreEvents.OnStorageUIRefreshRequested += HandleStorageUIRefreshRequested;
        }

        private void OnDestroy()
        {
            CoreEvents.OnInventoryOpenRequested    -= HandleInventoryOpenRequested;
            CoreEvents.OnStorageUIRefreshRequested -= HandleStorageUIRefreshRequested;
        }

        private void HandleInventoryOpenRequested(object payload)
        {
            if (payload is StorageContainer container)
                OpenContainer(container);
        }

        private void HandleStorageUIRefreshRequested(object payload)
        {
            if (payload is GameObject go)
            {
                StorageContainer container = go.GetComponent<StorageContainer>();
                if (container != null)
                    RefreshUI(container);
                else
                    Debug.LogWarning("[StorageInventoryUI] Refresh requested but no StorageContainer found on payload GameObject.");
            }
        }

        public void OpenContainer(StorageContainer container)
        {
            if (container == null)
                return;

            currentContainer = container;
            InventoryState.IsOpen = true;
            
            // Show mouse cursor
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;

            if (inventoryCanvas != null)
            {
                inventoryCanvas.gameObject.SetActive(true);
            }

            if (crosshairCanvasGroup != null)
            {
                crosshairCanvasGroup.alpha = 0f; // Hide crosshair when inventory is open
            }

            if (containerNameText != null)
                containerNameText.text = container.name;

            RefreshUI();
            UpdateWeightDisplay();
        }

        public void RefreshUI(StorageContainer container = null)
        {
            
            if (itemContainer != null)
            {
                foreach (Transform child in itemContainer)
                    Destroy(child.gameObject);
            }

            // Use provided container or fall back to currentContainer
            StorageContainer targetContainer = container ?? currentContainer;
            
            if (targetContainer == null)
            {
                return;
            }

            var items = targetContainer.Inventory.GetAllItems();
            
            foreach (var itemInstance in items)
            {
                StorageItemEntry entry = targetContainer.Inventory.GetEntryByInstance(itemInstance);
                if (entry.itemInstance?.Definition != null)
                {
                    CreateItemView(entry);
                }
            }

            UpdateWeightDisplay();
        }

        private void UpdateWeightDisplay()
        {
            if (weightCapacityText != null && currentContainer != null)
            {
                float currentWeight = currentContainer.GetCurrentWeight();
                float maxWeight = currentContainer.MaxWeightCapacity;
                weightCapacityText.text = $"{currentWeight:F1}kg / {maxWeight:F1}kg";
            }
        }
        private void CreateItemView(StorageItemEntry entry)
        {
            
            if (itemViewPrefab == null)
            {
                return;
            }
            
            if (itemContainer == null)
            {
                return;
            }

            if (inventoryCanvas == null)
            {
                return;
            }

            Rect containerRect = itemContainer.rect;

            GameObject viewGo = Instantiate(itemViewPrefab);
            
            RectTransform viewRect = viewGo.GetComponent<RectTransform>();
            if (viewRect != null)
            {
                viewRect.SetParent(itemContainer, false);
                viewRect.anchoredPosition = entry.uiPosition;
            }
            else
            {
                Destroy(viewGo);
                return;
            }

            StorageItemView view = viewGo.GetComponent<StorageItemView>();

            if (view != null)
            {
                view.InitializeWithCanvas(entry, this, inventoryCanvas, containerRect);
            }
            else
            {
                Destroy(viewGo);
            }
        }

        public void UpdateItemPosition(StorageItemEntry entry, Vector2 newPos)
        {
            if (currentContainer != null)
                currentContainer.Inventory.UpdateItemPosition(entry, newPos);
        }

        public void DropItemToWorld(StorageItemEntry entry)
        {
            if (currentContainer == null || entry.itemInstance == null)
                return;

            // Remove from container
            currentContainer.TryRemoveItem(entry.itemInstance);

            // Refresh UI to update weight display
            RefreshUI();

            // FIX: Drop offset away from container to prevent intersection
            Vector3 containerPos = currentContainer.transform.position;
            Vector3 dropPos = containerPos + Vector3.up * 2f + (playerCamera.position - containerPos).normalized * 1f;

            // Spawn in world near container
            GameObject worldItem = Instantiate(
                entry.itemInstance.Definition.WorldPrefab,
                dropPos,
                Quaternion.identity
            );

            ItemPickup pickup = worldItem.AddComponent<ItemPickup>();
            pickup.Initialize(entry.itemInstance);

            Rigidbody rb = worldItem.GetComponent<Rigidbody>();
            if (rb == null)
                rb = worldItem.AddComponent<Rigidbody>();
            rb.isKinematic = false;
            rb.useGravity = true;
            rb.linearVelocity = Vector3.zero;

            SphereCollider collider = worldItem.AddComponent<SphereCollider>();
            collider.radius = 0.3f;
        }

#region Shelf Drop & Highlight
        /// <summary>
        /// Raycasts from the current mouse position into the 3D world.
        /// If a mounted shelf with space is found, highlights it.
        /// Called every frame during drag from <see cref="StorageItemView.OnDrag"/>.
        /// </summary>
        public void UpdateShelfHighlight(ItemInstance item)
        {
            Shelf shelf = RaycastForShelf(item);

            // Same shelf — nothing to do
            if (shelf == highlightedShelf)
                return;

            ClearShelfHighlight();

            if (shelf != null)
                ApplyHighlight(shelf);
        }

        /// <summary>
        /// Clears any active shelf highlight, restoring original material colors.
        /// </summary>
        public void ClearShelfHighlight()
        {
            if (highlightedShelf == null)
                return;

            foreach (var kvp in originalColors)
            {
                if (kvp.Key != null)
                    kvp.Key.material.color = kvp.Value;
            }

            originalColors.Clear();
            highlightedShelf = null;
        }

        /// <summary>
        /// Attempts to stock an item on the shelf the mouse is pointing at.
        /// Returns true if the item was successfully sent to a shelf, false to fall back to world drop.
        /// </summary>
        public bool TryDropItemOnShelf(StorageItemEntry entry)
        {
            if (currentContainer == null || entry?.itemInstance == null)
                return false;

            Shelf shelf = RaycastForShelf(entry.itemInstance);
            if (shelf == null)
                return false;

            // Register item in the shelf's slot system
            if (!shelf.TryAddItem(entry.itemInstance))
                return false;

            // Remove from storage container
            currentContainer.TryRemoveItem(entry.itemInstance);
            RefreshUI();

            // Get target position & rotation from shelf layout
            IShelfLayout layout = shelf as IShelfLayout;
            Vector3 localTarget   = layout.GetSlotPosition(entry.itemInstance) + layout.GetStockingOffset();
            Vector3 localRotation = layout.GetStockingRotation();

            // Spawn world prefab at storage container position
            Vector3 spawnPos = currentContainer.transform.position + Vector3.up * 1f;
            GameObject worldItem = Instantiate(
                entry.itemInstance.Definition.WorldPrefab,
                spawnPos,
                Quaternion.identity
            );

            // Parent to shelf so DOLocalJump animates in shelf-local space
            Transform stockParent = shelf.transform;
            worldItem.transform.SetParent(stockParent);

            // Disable colliders during the tween
            Collider[] colliders = worldItem.GetComponentsInChildren<Collider>();
            foreach (var col in colliders)
                col.enabled = false;

            // Disable physics so Rigidbody doesn't fight the tween
            Rigidbody rb = worldItem.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true;
                rb.useGravity = false;
            }

            // Cache for closure
            ItemInstance itemInstance = entry.itemInstance;

            // Arc animation to shelf slot
            worldItem.transform
                .DOLocalJump(localTarget, 1f, 1, 0.5f)
                .SetEase(Ease.OutQuad)
                .OnComplete(() =>
                {
                    // Snap to exact position to prevent floating
                    worldItem.transform.localPosition = localTarget;
                    worldItem.transform.localEulerAngles = localRotation;

                    ItemPickup pickup = worldItem.GetComponent<ItemPickup>()
                                    ?? worldItem.AddComponent<ItemPickup>();
                    pickup.Initialize(itemInstance);
                    itemInstance.IsOnAShelf = true;

                    foreach (var col in colliders)
                        col.enabled = true;
                });

            worldItem.transform.DOLocalRotate(localRotation, 0.5f);

            return true;
        }

        /// <summary>
        /// Raycasts from the mouse into the 3D world and returns the first valid Shelf
        /// that can accept the given item, or null.
        /// </summary>
        private Shelf RaycastForShelf(ItemInstance item)
        {
            Ray ray = Camera.main.ScreenPointToRay(UnityEngine.Input.mousePosition);
            if (!Physics.Raycast(ray, out RaycastHit hit, 50f))
                return null;

            // Direct hit on a shelf board
            Shelf shelf = hit.collider.GetComponent<Shelf>();
            if (shelf != null && shelf.CanAddItem(item))
                return shelf;

            // Hit the ShelfContainer body — find first child shelf with space
            var container = hit.collider.GetComponentInParent<ShelfContainer>();
            if (container != null && container.IsMounted)
                return container.Shelves.FirstOrDefault(s => s.CanAddItem(item));

            return null;
        }

        private void ApplyHighlight(Shelf shelf)
        {
            highlightedShelf = shelf;
            Renderer[] renderers = shelf.GetComponentsInChildren<Renderer>();
            foreach (var r in renderers)
            {
                if (!originalColors.ContainsKey(r))
                    originalColors[r] = r.material.color;
                r.material.color = Color.Lerp(r.material.color, shelfHighlightColor, 0.5f);
            }
        }
#endregion

        public void CloseInventory()
        {
            ClearShelfHighlight();

            if (inventoryCanvas != null)
                inventoryCanvas.gameObject.SetActive(false);

            InventoryState.IsOpen = false;
            
            // Hide mouse cursor
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;

            if (crosshairCanvasGroup != null)
            {
                crosshairCanvasGroup.alpha = 1f; // Show crosshair when inventory is closed
            }

            currentContainer = null;
        }
    }
}