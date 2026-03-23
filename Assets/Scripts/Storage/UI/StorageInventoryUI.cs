using UnityEngine;
using UnityEngine.UI;
using AsakuShop.Items;
using AsakuShop.Core;
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
            //($"Found {items.Count} items in inventory");
            
            foreach (var entry in items)
            {
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
                // Don't force size - let the prefab define it
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
            Vector3 dropPos = containerPos + Vector3.up * 2f + (playerCamera.position - containerPos).normalized * 1f; // Drop in front of player

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

        public void CloseInventory()
        {
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