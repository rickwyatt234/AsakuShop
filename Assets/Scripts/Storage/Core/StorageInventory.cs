using System;
using System.Collections.Generic;
using AsakuShop.Items;
using UnityEngine;

namespace AsakuShop.Storage
{
    // Free-form inventory using item positions instead of fixed slots.
    // Items can be dragged anywhere within the inventory bounds.
    public class StorageInventory
    {
        public event Action OnInventoryChanged;

        private List<StorageItemEntry> items = new();
        private Vector2 inventorySize; // Width and height of inventory window

        public StorageInventory(Vector2 containerSize)
        {
            inventorySize = containerSize;
        }

        public bool TryAddItem(ItemInstance item, Vector2? preferredPos = null)
        {
            if (item == null)
                return false;

            StorageItemEntry entry = new StorageItemEntry
            {
                itemInstance = item,
                uiPosition = preferredPos ?? Vector2.zero
            };

            items.Add(entry);
            OnInventoryChanged?.Invoke();
            return true;
        }

        public bool TryRemoveItem(StorageItemEntry entry)
        {
            if (entry == null)
                return false;

            bool removed = items.Remove(entry);
            if (removed)
                OnInventoryChanged?.Invoke();

            return removed;
        }

        public void UpdateItemPosition(StorageItemEntry entry, Vector2 newPos)
        {
            if (entry != null)
                entry.uiPosition = newPos;
        }

        public List<StorageItemEntry> GetAllItems() => new List<StorageItemEntry>(items);

        public StorageItemEntry GetEntryByInstance(ItemInstance instance)
        {
            return items.Find(e => e.itemInstance == instance);
        }

        public void Clear()
        {
            items.Clear();
            OnInventoryChanged?.Invoke();
        }

        public int Count => items.Count;
    }
}