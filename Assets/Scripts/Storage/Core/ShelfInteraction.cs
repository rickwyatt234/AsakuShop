using UnityEngine;
using AsakuShop.Items;
using System;
using System.Collections.Generic;

namespace AsakuShop.Storage
{
    public class ShelfInteraction : MonoBehaviour, IStorageUnit
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
#region IStorageUnit Implementation
        public bool CanAddItem(ItemInstance item)
        {
            if (shelfComponent == null || item == null || 
            shelfComponent.Items.Count >= shelfComponent.slotColumns * shelfComponent.slotRows ||
            !Array.Exists(shelfComponent.allowedStockingSizes, size => size == item.Definition.StockingSize))
                return false;

            return true;
        }

        public bool TryAddItem(ItemInstance item)
        {
            if (!CanAddItem(item))
                return false;

            shelfComponent.Items.Add(item);

            int maxSlots = shelfComponent.slotColumns * shelfComponent.slotRows;
            int availableSlot = 0;
            // Find the first available slot index
            // Prevents multiple items occupying the same slot index
            for (int i = 0; i < maxSlots; i++)
            {
                bool slotOccupied = false;
                foreach (var kvp in shelfComponent.itemToSlotIndex)
                {
                    if (kvp.Value == i)
                    {
                        slotOccupied = true;
                        break;
                    }
                }
                
                if (!slotOccupied)
                {
                    availableSlot = i;
                    break;
                }
            }
            
            shelfComponent.itemToSlotIndex[item] = availableSlot;
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
#endregion

        public Vector3 GetSlotPosition(ItemInstance item)
        {
            if (!shelfComponent.itemToSlotIndex.TryGetValue(item, out int slotIndex))
                return Vector3.zero; // Default position if item not found

            int column = slotIndex % shelfComponent.slotColumns;
            int row = slotIndex / shelfComponent.slotColumns;

            float xOffset = (column - (shelfComponent.slotColumns - 1) / 2f) * shelfComponent.slotSpacingX;
            float yOffset = (row - (shelfComponent.slotRows - 1) / 2f) * shelfComponent.slotSpacingY;

            return new Vector3(xOffset, yOffset, 0f);
        }
    }
}