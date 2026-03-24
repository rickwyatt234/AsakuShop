using UnityEngine;
using System;
using System.Collections.Generic;
using AsakuShop.Items;
using AsakuShop.Core;

namespace AsakuShop.Storage
{
    public class StorageInventory : IStorageUnit
    {
            public event Action OnInventoryChanged;
            private List<StorageItemEntry> items = new();
            private Vector2 inventorySize; // Width and height of inventory window
            public int Count => items.Count;

            public StorageInventory(Vector2 containerSize)
            {
                inventorySize = containerSize;
            }


/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
#region IStorageUnit Implementation
            public bool CanAddItem(ItemInstance item)
            {
                if (item == null || (GetCurrentWeight() + item.Definition.WeightKg) > float.MaxValue)
                {
                    Debug.LogWarning($"[STORAGE INVENTORY] Cannot add item {item?.Definition.DisplayName ?? "null"} - weight limit exceeded or item is null");
                    return false;
                }
                else
                {
                    return true;
                }
             }

             public bool TryAddItem(ItemInstance item)
             {
                 if (!CanAddItem(item))
                     return false;

                 StorageItemEntry entry = new StorageItemEntry
                 {
                     itemInstance = item,
                     uiPosition = new Vector2(UnityEngine.Random.Range(0, inventorySize.x), 
                        UnityEngine.Random.Range(0, inventorySize.y)) // Random position for now
                 };

                 items.Add(entry);
                 OnInventoryChanged?.Invoke();
                 return true;
             }

            // Remove by item instance reference. Useful for when we only have the item and not the full 
            // entry (e.g. from UI drag-and-drop)
             public bool TryRemoveItem(ItemInstance item)
             {
                 if (item == null)
                     return false;

                 StorageItemEntry entry = items.Find(e => e.itemInstance == item);
                 if (entry != null)
                 {
                     items.Remove(entry);
                     OnInventoryChanged?.Invoke();
                     return true;
                 }
                 return false;
             }

            // Overload to remove by entry reference (used by UI drag-and-drop)
            // This is more efficient when we already have the entry (e.g. from UI) since it avoids 
            // searching by item instance
             public bool TryRemoveItem(StorageItemEntry entry)
             {
                 if (entry == null || entry.itemInstance == null)
                     return false;

                 if (items.Remove(entry))
                 {
                     OnInventoryChanged?.Invoke();
                     return true;
                 }
                 return false;
             }
#endregion





/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
#region Public Getters for Inventory Data
            public List<ItemInstance> GetAllItems()
            {
                List<ItemInstance> items = new();
                foreach (var entry in this.items)
                {
                    if (entry.itemInstance != null)
                        items.Add(entry.itemInstance);
                }
                return items;
            }
            public StorageItemEntry GetEntryByInstance(ItemInstance instance)
            {
                return this.items.Find(e => e.itemInstance == instance);
            }

            public float GetCurrentWeight()
            {
                float totalWeight = 0f;
                foreach (var entry in this.items)
                {
                    if (entry.itemInstance?.Definition != null)
                        totalWeight += entry.itemInstance.Definition.WeightKg;
                }
                return totalWeight;
            }
            public int GetCapacity() => int.MaxValue; // No fixed capacity, but could be limited
            public int GetCurrentCount() => items.Count;
#endregion     


            public void UpdateItemPosition(StorageItemEntry entry, Vector2 newPos)
            {
                if (entry != null)
                    entry.uiPosition = newPos;
            }
            public void Clear()
            {
                items.Clear();
                OnInventoryChanged?.Invoke();
            }

            /// <summary>
            /// Implements <see cref="IStorageUnit.ClearAllItems"/>.
            /// Removes all items from the inventory and raises the change event.
            /// </summary>
            public void ClearAllItems() => Clear();

    }
    
}
