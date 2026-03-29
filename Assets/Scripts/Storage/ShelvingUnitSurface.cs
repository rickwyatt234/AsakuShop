using UnityEngine;
using System.Collections.Generic;
using AsakuShop.Items;
using AsakuShop.Core;

namespace AsakuShop.Storage
{
    public class ShelvingUnitSurface : MonoBehaviour, IStorageUnit, IShelfLayout
    {
        public StockingSize[] allowedStockingSizes = new StockingSize[] { StockingSize.Small, StockingSize.Medium, StockingSize.Large };
        public int slotColumns = 4;
        public int slotRows = 1;
        public float slotSpacingX = 0.3f;
        public float slotSpacingY = 0.3f;
        [SerializeField] private Vector3 stockingRotation = new Vector3(0, 0, 0);
        [SerializeField] private Vector3 stockingOffset = new Vector3(0, 0.5f, 0);
        [SerializeField] private Vector3 slotStartOffset = new Vector3(-0.9f, 0.9f, 0);

        private List<ItemInstance> items = new();
        private Dictionary<ItemInstance, int> itemToSlotIndex = new();

        private int GetSlotSize(ItemInstance item)
        {
            return item.Definition.StockingSize switch
            {
                StockingSize.Small => 1,
                StockingSize.Medium => 2,
                StockingSize.Large => 4,
                _ => 1
            };
        }

        private int FindAnchorSlot(int size)
        {
            int maxSlots = slotColumns * slotRows;
            if (size > maxSlots) return -1;
            HashSet<int> occupiedSlots = new();
            foreach (var kvp in itemToSlotIndex)
            {
                int s = GetSlotSize(kvp.Key);
                for (int i = 0; i < s; i++)
                    occupiedSlots.Add(kvp.Value + i);
            }
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
            return -1;
        }

        private Vector3 SlotIndexToLocalPosition(int slotIndex)
        {
            int column = slotIndex % slotColumns;
            int row = slotIndex / slotColumns;
            float xOffset = (column - (slotColumns - 1) / 2f) * slotSpacingX;
            float yOffset = (row - (slotRows - 1) / 2f) * slotSpacingY;
            return new Vector3(xOffset, yOffset, 0f);
        }

        public bool CanAddItem(ItemInstance item)
        {
            if (item == null) return false;
            if (!System.Array.Exists(allowedStockingSizes, size => size == item.Definition.StockingSize))
                return false;
            return FindAnchorSlot(GetSlotSize(item)) >= 0;
        }

        public bool TryAddItem(ItemInstance item)
        {
            if (!CanAddItem(item)) return false;
            int anchor = FindAnchorSlot(GetSlotSize(item));
            if (anchor < 0) return false;
            items.Add(item);
            itemToSlotIndex[item] = anchor;
            return true;
        }

        public bool TryRemoveItem(ItemInstance item)
        {
            if (!items.Remove(item)) return false;
            itemToSlotIndex.Remove(item);
            return true;
        }

        public List<ItemInstance> GetAllItems() => new(items);
        public int GetCapacity() => slotColumns * slotRows;
        public int GetCurrentCount() => items.Count;

        public float GetCurrentWeight()
        {
            float total = 0f;
            foreach (var item in items)
                if (item?.Definition != null)
                    total += item.Definition.WeightKg;
            return total;
        }

        public void ClearAllItems()
        {
            items.Clear();
            itemToSlotIndex.Clear();
        }

        public List<int> GetOccupiedSlots(ItemInstance item)
        {
            var slots = new List<int>();
            if (!itemToSlotIndex.TryGetValue(item, out int anchor))
                return slots;
            int size = GetSlotSize(item);
            for (int i = 0; i < size; i++)
                slots.Add(anchor + i);
            return slots;
        }

        public Vector3 GetSlotPosition(ItemInstance item)
        {
            if (!itemToSlotIndex.TryGetValue(item, out int anchor))
                return Vector3.zero;
            int size = GetSlotSize(item);
            int lastSlot = anchor + size - 1;
            Vector3 anchorPos = SlotIndexToLocalPosition(anchor);
            Vector3 lastPos = SlotIndexToLocalPosition(lastSlot);
            return (anchorPos + lastPos) / 2f;
        }

        public Vector3 GetStockingOffset() => stockingOffset;
        public Vector3 GetStockingRotation() => stockingRotation;

        // For ShelvingUnitComponent delegation
        public ItemInstance PeekItem() => items.Count > 0 ? items[0] : null;
        public ShelfTakeResult TakeItem()
        {
            if (items.Count == 0) return default;
            var item = items[0];
            TryRemoveItem(item);
            return new ShelfTakeResult(item, null);
        }
    }
}