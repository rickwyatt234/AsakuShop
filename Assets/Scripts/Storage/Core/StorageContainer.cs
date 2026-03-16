using UnityEngine;
using AsakuShop.Items;
using AsakuShop.Core;
using System.Collections.Generic;

namespace AsakuShop.Storage
{
    public class StorageContainer : MonoBehaviour, IStorageUnit
    {
        [SerializeField] private StorageType storageType = StorageType.Dry;
        [SerializeField] private Vector2 inventorySize = new Vector2(500, 400); // UI window size
        [SerializeField] private string containerName = "Container";

        private StorageInventory inventory;

        public StorageType StorageType => storageType;
        public StorageInventory Inventory => inventory;
        public Vector2 InventorySize => inventorySize;

        private void Awake()
        {
            inventory = new StorageInventory(inventorySize);
        }

        public bool CanAddItem(ItemInstance item)
        {
            return item != null && item.Definition.StorageType == storageType;
        }

        public bool TryAddItem(ItemInstance item)
        {
            if (!CanAddItem(item))
                return false;

            return inventory.TryAddItem(item);
        }

        public bool TryRemoveItem(ItemInstance item)
        {
            StorageItemEntry entry = inventory.GetEntryByInstance(item);
            return inventory.TryRemoveItem(entry);
        }

        public List<ItemInstance> GetAllItems()
        {
            List<ItemInstance> items = new();
            foreach (var entry in inventory.GetAllItems())
            {
                if (entry.itemInstance != null)
                    items.Add(entry.itemInstance);
            }
            return items;
        }

        public int GetCapacity() => int.MaxValue; // No fixed capacity, but could be limited
        public int GetCurrentCount() => inventory.Count;

        // Open the inventory UI for this container
        public void OpenInventory()
        {
            if (StorageInventoryUI.Instance != null)
                StorageInventoryUI.Instance.OpenContainer(this);
            else
                Debug.LogError("StorageInventoryUI Instance not found!");
        }
    }
}