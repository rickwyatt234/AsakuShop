using UnityEngine;
using UnityEngine.UI;
using AsakuShop.Items;
using AsakuShop.Core;
using TMPro;

namespace AsakuShop.Storage
{
    /// <summary>
    /// UI controller for the storage inventory window.
    /// Manages item views and drag/drop logic.
    /// </summary>
    public class StorageInventoryUI : MonoBehaviour
    {
        [SerializeField] private Canvas inventoryCanvas;
        [SerializeField] private RectTransform itemContainer;
        [SerializeField] private GameObject itemViewPrefab;
        [SerializeField] private TextMeshProUGUI containerNameText;
        [SerializeField] private Button closeButton;

        private StorageContainer currentContainer;
        private Transform playerCamera;

        private void Awake()
        {
            playerCamera = Camera.main.transform;

            if (closeButton != null)
                closeButton.onClick.AddListener(CloseInventory);

            if (inventoryCanvas != null)
                inventoryCanvas.gameObject.SetActive(false);
        }

        public void OpenContainer(StorageContainer container)
        {
            if (container == null)
                return;

            currentContainer = container;

            if (inventoryCanvas != null)
                inventoryCanvas.gameObject.SetActive(true);

            if (containerNameText != null)
                containerNameText.text = container.name;

            RefreshUI();
        }

        private void RefreshUI()
        {
            if (itemContainer != null)
            {
                // Clear existing items
                foreach (Transform child in itemContainer)
                    Destroy(child.gameObject);
            }

            if (currentContainer == null)
                return;

            // Create item views for all items in container
            foreach (var entry in currentContainer.Inventory.GetAllItems())
            {
                CreateItemView(entry);
            }
        }

        private void CreateItemView(StorageItemEntry entry)
        {
            if (itemViewPrefab == null || itemContainer == null)
                return;

            GameObject viewGo = Instantiate(itemViewPrefab, itemContainer);
            StorageItemView view = viewGo.GetComponent<StorageItemView>();

            if (view != null)
                view.Initialize(entry, this);
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

            // Spawn in world near player
            GameObject worldItem = Instantiate(
                entry.itemInstance.Definition.WorldPrefab,
                playerCamera.position + playerCamera.forward * 2f,
                Quaternion.identity
            );

            ItemPickup pickup = worldItem.AddComponent<ItemPickup>();
            pickup.itemInstance = entry.itemInstance;

            SphereCollider collider = worldItem.AddComponent<SphereCollider>();
            collider.radius = 0.3f;
        }

        public void CloseInventory()
        {
            if (inventoryCanvas != null)
                inventoryCanvas.gameObject.SetActive(false);

            currentContainer = null;
        }
    }
}