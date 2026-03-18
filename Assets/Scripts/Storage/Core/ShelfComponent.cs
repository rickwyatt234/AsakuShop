using UnityEngine;
using AsakuShop.Items;
using System.Collections.Generic;

namespace AsakuShop.Storage
{
    public class ShelfComponent : MonoBehaviour, IStorageUnit
    {
        [SerializeField] private StorageType storageType = StorageType.Dry;
        [SerializeField] private StockingSize minItemSize = StockingSize.Small;
        [SerializeField] private StockingSize maxItemSize = StockingSize.Large;
        [SerializeField] private int slotColumns = 4;
        [SerializeField] private int slotRows = 3;
        [SerializeField] private float slotSpacingX = 0.3f;
        [SerializeField] private float slotSpacingY = 0.3f;
        [SerializeField] private Vector3 stockingRotation = new Vector3(90, 0, 0);
        [SerializeField] private Vector3 stockingOffset = new Vector3(0, 0.5f, 0);
        public float mountOffsetDistance = 0.5f;
        [SerializeField] private Vector3 slotStartOffset = new Vector3(-0.9f, 0.9f, 0);
        [SerializeField] private string shelfID = "Shelf001";
        public string shelfName = "Shelf";
        [SerializeField] private Vector3 rotationOffset = Vector3.zero;
        [SerializeField] private float browsingDistance = 1.5f;  // NEW: How far in front of shelf customer stands

        private List<ItemInstance> items = new();
        public Vector3 RotationOffset => rotationOffset;
        private Dictionary<ItemInstance, int> itemToSlotIndex = new();

        public StorageType StorageType => storageType;
        public Vector3 GetStockingRotation() => stockingRotation;
        public Vector3 GetStockingOffset() => stockingOffset;
        public float GetBrowsingDistance() => browsingDistance;  // NEW: Getter for browsing offset

        public bool CanAddItem(ItemInstance item)
        {
            if (item == null || items.Count >= (slotColumns * slotRows))
                return false;

            StockingSize itemSize = item.Definition.StockingSize;
            
            return item.Definition.StorageType == storageType 
                && itemSize >= minItemSize
                && itemSize <= maxItemSize;
        }

        public bool TryAddItem(ItemInstance item)
        {
            if (!CanAddItem(item))
                return false;

            items.Add(item);
            
            int maxSlots = slotColumns * slotRows;
            int availableSlot = 0;
            
            for (int i = 0; i < maxSlots; i++)
            {
                bool slotOccupied = false;
                foreach (var kvp in itemToSlotIndex)
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
            
            itemToSlotIndex[item] = availableSlot;
            return true;
        }

        public bool TryRemoveItem(ItemInstance item)
        {
            if (!items.Remove(item))
                return false;

            itemToSlotIndex.Remove(item);
            return true;
        }

        public Vector3 GetSlotPosition(int slotIndex)
        {
            int column = slotIndex % slotColumns;
            int row = slotIndex / slotColumns;

            Vector3 slotPos = slotStartOffset;
            slotPos.x += column * slotSpacingX;
            slotPos.y -= row * slotSpacingY;

            return transform.TransformPoint(slotPos);
        }

        public Vector3 GetSlotPosition(ItemInstance item)
        {
            if (itemToSlotIndex.TryGetValue(item, out int slotIndex))
                return GetSlotPosition(slotIndex);
            return transform.position;
        }

        public List<ItemInstance> GetAllItems() => new List<ItemInstance>(items);
        public int GetCapacity() => slotColumns * slotRows;
        public int GetCurrentCount() => items.Count;
        public bool IsFull() => items.Count >= (slotColumns * slotRows);

        public float GetSellPrice(ItemInstance item)
        {
            return item?.Definition?.BaseSellPrice ?? 0f;
        }
    }
}