using UnityEngine;
using System;
using System.Collections.Generic;
using AsakuShop.Items;

namespace AsakuShop.Storage
{
    /// <summary>
    /// An individual shelf board inside a <see cref="ShelfContainer"/>.
    /// Handles slot-based item stocking. Requires its own BoxCollider for player interaction.
    /// </summary>
    [RequireComponent(typeof(BoxCollider))]
    public class Shelf : MonoBehaviour, IStorageUnit, IShelfLayout
    {
        public StockingSize[] allowedStockingSizes = new StockingSize[]
            { StockingSize.Small, StockingSize.Medium, StockingSize.Large };

        public int slotColumns = 4;
        public int slotRows = 1;
        public float slotSpacingX = 0.3f;
        public float slotSpacingY = 0.3f;

        [SerializeField] private Vector3 stockingRotation = Vector3.zero;
        [SerializeField] private Vector3 stockingOffset = new Vector3(0, 0.05f, 0);

        /// <summary>The <see cref="ShelfContainer"/> that owns this shelf. Set by <see cref="ShelfContainer"/> on Awake.</summary>
        public ShelfContainer ShelfContainer { get; set; }

        private BoxCollider boxCollider;
        private readonly List<ItemInstance> items = new();
        private readonly Dictionary<ItemInstance, int> itemToSlotIndex = new();

        /// <summary>Read-only view of all currently stocked items.</summary>
        public List<ItemInstance> Items => items;

        private void Awake()
        {
            boxCollider = GetComponent<BoxCollider>();
            Debug.Log($"[Shelf] '{name}' awake — {slotColumns} col × {slotRows} row = {slotColumns * slotRows} slots. Parent container: '{(ShelfContainer != null ? ShelfContainer.name : "none (set later)")}').");
        }

        /// <summary>Enables or disables this shelf's collider (e.g. disabled while the parent is being carried).</summary>
        public void ToggleInteraction(bool enabled)
        {
            boxCollider.enabled = enabled;
            Debug.Log($"[Shelf] '{name}' interaction {(enabled ? "ENABLED" : "DISABLED")}.");
        }

#region Slot Helpers
        private int GetSlotSize(ItemInstance item) =>
            item.Definition.StockingSize switch
            {
                StockingSize.Small  => 1,
                StockingSize.Medium => 2,
                StockingSize.Large  => 4,
                _                  => 1
            };

        /// <summary>
        /// Searches for the first contiguous run of <paramref name="size"/> free slot indices.
        /// Returns the anchor (first) slot index, or -1 if no run exists.
        /// </summary>
        private int FindAnchorSlot(int size)
        {
            int maxSlots = slotColumns * slotRows;
            if (size > maxSlots) return -1;

            HashSet<int> occupied = new();
            foreach (var kvp in itemToSlotIndex)
            {
                int s = GetSlotSize(kvp.Key);
                for (int i = 0; i < s; i++)
                    occupied.Add(kvp.Value + i);
            }

            for (int anchor = 0; anchor <= maxSlots - size; anchor++)
            {
                bool fits = true;
                for (int offset = 0; offset < size; offset++)
                {
                    if (occupied.Contains(anchor + offset)) { fits = false; break; }
                }
                if (fits) return anchor;
            }
            return -1;
        }

        /// <summary>Converts a flat slot index to a local-space Vector3 position on the shelf grid.</summary>
        private Vector3 SlotIndexToLocalPosition(int slotIndex)
        {
            int column = slotIndex % slotColumns;
            int row    = slotIndex / slotColumns;
            float x    = (column - (slotColumns - 1) / 2f) * slotSpacingX;
            float y    = (row    - (slotRows    - 1) / 2f) * slotSpacingY;
            return new Vector3(x, y, 0f);
        }
#endregion

#region IStorageUnit
        public bool CanAddItem(ItemInstance item)
        {
            if (item == null)
            {
                Debug.LogWarning($"[Shelf] '{name}' CanAddItem — item is null.");
                return false;
            }
            if (!Array.Exists(allowedStockingSizes, s => s == item.Definition.StockingSize))
            {
                Debug.Log($"[Shelf] '{name}' CanAddItem — '{item.Definition.DisplayName}' size {item.Definition.StockingSize} not in allowed sizes.");
                return false;
            }
            int slotSize = GetSlotSize(item);
            int anchor = FindAnchorSlot(slotSize);
            bool result = anchor >= 0;
            Debug.Log($"[Shelf] '{name}' CanAddItem '{item.Definition.DisplayName}' (size={slotSize}) — anchor={anchor}, result={result}. [{items.Count}/{GetCapacity()} stocked]");
            return result;
        }

        public bool TryAddItem(ItemInstance item)
        {
            if (!CanAddItem(item))
            {
                Debug.Log($"[Shelf] '{name}' TryAddItem '{item?.Definition?.DisplayName}' — CanAddItem returned false.");
                return false;
            }
            int anchor = FindAnchorSlot(GetSlotSize(item));
            if (anchor < 0)
            {
                Debug.LogWarning($"[Shelf] '{name}' TryAddItem — FindAnchorSlot returned -1 after CanAddItem passed (unexpected).");
                return false;
            }
            items.Add(item);
            itemToSlotIndex[item] = anchor;
            Debug.Log($"[Shelf] '{name}' TryAddItem — added '{item.Definition.DisplayName}' at anchor slot {anchor}. [{items.Count}/{GetCapacity()} stocked]");
            return true;
        }

        public bool TryRemoveItem(ItemInstance item)
        {
            if (!items.Remove(item))
            {
                Debug.LogWarning($"[Shelf] '{name}' TryRemoveItem — '{item?.Definition?.DisplayName}' was not found in items list.");
                return false;
            }
            itemToSlotIndex.Remove(item);
            Debug.Log($"[Shelf] '{name}' TryRemoveItem — removed '{item.Definition.DisplayName}'. [{items.Count}/{GetCapacity()} remaining]");
            return true;
        }

        public List<ItemInstance> GetAllItems()   => new(items);
        public int GetCapacity()                  => slotColumns * slotRows;
        public int GetCurrentCount()              => items.Count;

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
            int count = items.Count;
            items.Clear();
            itemToSlotIndex.Clear();
            Debug.Log($"[Shelf] '{name}' ClearAllItems — cleared {count} item(s).");
        }
#endregion

#region IShelfLayout
        /// <summary>
        /// Returns the local-space centre position of the slots occupied by <paramref name="item"/>.
        /// </summary>
        public Vector3 GetSlotPosition(ItemInstance item)
        {
            if (!itemToSlotIndex.TryGetValue(item, out int anchor))
                return Vector3.zero;
            int size     = GetSlotSize(item);
            int lastSlot = anchor + size - 1;
            return (SlotIndexToLocalPosition(anchor) + SlotIndexToLocalPosition(lastSlot)) / 2f;
        }

        public Vector3 GetStockingOffset()   => stockingOffset;
        public Vector3 GetStockingRotation() => stockingRotation;
#endregion

#region Customer AI Helpers
        /// <summary>Returns the first stocked item without removing it, or null if empty.</summary>
        public ItemInstance PeekItem() => items.Count > 0 ? items[0] : null;

        /// <summary>Removes the first item and returns it together with its world pickup object.</summary>
        public ShelfTakeResult TakeItem()
        {
            if (items.Count == 0)
            {
                Debug.Log($"[Shelf] '{name}' TakeItem — shelf is empty.");
                return default;
            }
            ItemInstance item   = items[0];
            ItemPickup   pickup = FindPickupForItem(item);
            TryRemoveItem(item);
            Debug.Log($"[Shelf] '{name}' TakeItem — took '{item.Definition.DisplayName}'. Pickup found={pickup != null}.");
            return new ShelfTakeResult(item, pickup);
        }

        private ItemPickup FindPickupForItem(ItemInstance item)
        {
            if (item == null) return null;
            foreach (ItemPickup p in GetComponentsInChildren<ItemPickup>(true))
                if (p != null && ReferenceEquals(p.ItemInstance, item))
                    return p;
            Debug.LogWarning($"[Shelf] '{name}' FindPickupForItem — no matching ItemPickup found for '{item.Definition.DisplayName}'.");
            return null;
        }
#endregion
    }
}
