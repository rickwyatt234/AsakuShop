using UnityEngine;
using AsakuShop.Items;
using AsakuShop.Core;
using System;
using System.Collections.Generic;

namespace AsakuShop.Storage
{
    public class ShelfInteraction : MonoBehaviour, IStorageUnit, IShelfLayout
    {
        // This class handles interactions with shelves, allowing items to be placed on them.
        private ShelfComponent shelfComponent;


/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
#region Unity Lifecycle
        private void OnEnable()
        {
            shelfComponent = GetComponent<ShelfComponent>();
        }
#endregion

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
#region Slot Helpers
        /// <summary>
        /// Returns the number of shelf slots a given item occupies based on its StockingSize.
        /// Small = 1 slot, Medium = 2 slots, Large = 4 slots.
        /// </summary>
        private int GetSlotSize(ItemInstance item)
        {
            return item.Definition.StockingSize switch
            {
                StockingSize.Small  => 1,
                StockingSize.Medium => 2,
                StockingSize.Large  => 4,
                _ => 1
            };
        }

        /// <summary>
        /// Searches for the first contiguous run of <paramref name="size"/> free slot indices.
        /// Returns the anchor (first) slot index, or -1 if no run exists.
        /// </summary>
        private int FindAnchorSlot(int size)
        {
            int maxSlots = shelfComponent.slotColumns * shelfComponent.slotRows;

            // Guard: if the requested run exceeds total capacity, it can never fit.
            if (size > maxSlots) return -1;

            // Build a set of all currently occupied slot indices, accounting for multi-slot items.
            HashSet<int> occupiedSlots = new HashSet<int>();
            foreach (var kvp in shelfComponent.itemToSlotIndex)
            {
                int s = GetSlotSize(kvp.Key);
                for (int i = 0; i < s; i++)
                    occupiedSlots.Add(kvp.Value + i);
            }

            // Scan for the first run of `size` consecutive free slots.
            for (int anchor = 0; anchor <= maxSlots - size; anchor++)
            {
                bool fits = true;
                for (int offset = 0; offset < size; offset++)
                {
                    if (occupiedSlots.Contains(anchor + offset))
                    {
                        fits = false;
                        break;
                    }
                }
                if (fits) return anchor;
            }

            return -1; // No contiguous run found.
        }

        /// <summary>
        /// Converts a flat slot index into a local-space Vector3 position on the shelf grid.
        /// </summary>
        private Vector3 SlotIndexToLocalPosition(int slotIndex)
        {
            int column = slotIndex % shelfComponent.slotColumns;
            int row    = slotIndex / shelfComponent.slotColumns;

            float xOffset = (column - (shelfComponent.slotColumns - 1) / 2f) * shelfComponent.slotSpacingX;
            float yOffset = (row    - (shelfComponent.slotRows    - 1) / 2f) * shelfComponent.slotSpacingY;

            return new Vector3(xOffset, yOffset, 0f);
        }
#endregion

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
#region IStorageUnit Implementation
        public bool CanAddItem(ItemInstance item)
        {
            if (shelfComponent == null || item == null)
                return false;

            // Item's StockingSize must be in the shelf's allowed sizes.
            if (!Array.Exists(shelfComponent.allowedStockingSizes, size => size == item.Definition.StockingSize))
                return false;

            // There must be a contiguous run of free slots large enough for the item.
            return FindAnchorSlot(GetSlotSize(item)) >= 0;
        }

        public bool TryAddItem(ItemInstance item)
        {
            if (!CanAddItem(item))
                return false;

            int anchor = FindAnchorSlot(GetSlotSize(item));
            if (anchor < 0)
                return false;

            shelfComponent.Items.Add(item);
            shelfComponent.itemToSlotIndex[item] = anchor; // Store anchor (first) slot index.
            return true;
        }

        public bool TryRemoveItem(ItemInstance item)
        {
            if (!shelfComponent.Items.Remove(item))
                return false;

            shelfComponent.itemToSlotIndex.Remove(item);
            return true;
        }

        public List<ItemInstance> GetAllItems() => new List<ItemInstance>(shelfComponent.Items);
        public int GetCapacity() => shelfComponent.slotColumns * shelfComponent.slotRows;
        public int GetCurrentCount() => shelfComponent.Items.Count;

        public float GetCurrentWeight()
        {
            float totalWeight = 0f;
            foreach (var item in shelfComponent.Items)
            {
                if (item?.Definition != null)
                    totalWeight += item.Definition.WeightKg;
            }
            return totalWeight;
        }

        /// <summary>
        /// Clears all items from the shelf's data collections.
        /// Called when a shelf is picked up so its stocked items can be ejected.
        /// </summary>
        public void ClearAllItems()
        {
            shelfComponent.Items.Clear();
            shelfComponent.itemToSlotIndex.Clear();
        }
#endregion

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
#region IShelfLayout Implementation
        /// <summary>
        /// Returns all slot indices occupied by <paramref name="item"/>,
        /// i.e. anchor through anchor + size - 1.
        /// </summary>
        public List<int> GetOccupiedSlots(ItemInstance item)
        {
            var slots = new List<int>();
            if (!shelfComponent.itemToSlotIndex.TryGetValue(item, out int anchor))
                return slots;

            int size = GetSlotSize(item);
            for (int i = 0; i < size; i++)
                slots.Add(anchor + i);

            return slots;
        }

        /// <summary>
        /// Returns the local-space centre position of the run of slots occupied by <paramref name="item"/>.
        /// For a single-slot item this equals that slot's position; for multi-slot items it is
        /// the midpoint between the anchor slot and the last occupied slot.
        /// </summary>
        public Vector3 GetSlotPosition(ItemInstance item)
        {
            if (!shelfComponent.itemToSlotIndex.TryGetValue(item, out int anchor))
                return Vector3.zero;

            int size     = GetSlotSize(item);
            int lastSlot = anchor + size - 1;

            Vector3 anchorPos = SlotIndexToLocalPosition(anchor);
            Vector3 lastPos   = SlotIndexToLocalPosition(lastSlot);

            // Return the midpoint so the visual appears centred across all reserved slots.
            return (anchorPos + lastPos) / 2f;
        }

        public Vector3 GetStockingOffset()
        {
            return shelfComponent != null ? shelfComponent.GetStockingOffset() : Vector3.zero;
        }

        public Vector3 GetStockingRotation()
        {
            return shelfComponent != null ? shelfComponent.GetStockingRotation() : Vector3.zero;
        }
#endregion
    }
}
