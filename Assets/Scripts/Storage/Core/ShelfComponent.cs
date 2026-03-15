using UnityEngine;
using AsakuShop.Items;
using System.Collections.Generic;

namespace AsakuShop.Storage
{
    // Display shelf for selling items. Uses fixed slots instead of free-form.
    // Items cannot be queried for crafting.
    public class ShelfComponent : MonoBehaviour, IStorageUnit
    {
        [SerializeField] private StorageType storageType = StorageType.Dry;
        [SerializeField] private StockingSize requiredSize = StockingSize.Small;
        [SerializeField] private int slotCount = 12;
        [SerializeField] private string shelfName = "Shelf";

        private List<ItemInstance> items = new();

        public StorageType StorageType => storageType;
        public StockingSize RequiredSize => requiredSize;

        public bool CanAddItem(ItemInstance item)
        {
            if (item == null || items.Count >= slotCount)
                return false;

            return item.Definition.StorageType == storageType 
                && item.Definition.StockingSize <= requiredSize;
        }

        public bool TryAddItem(ItemInstance item)
        {
            if (!CanAddItem(item))
                return false;

            items.Add(item);
            return true;
        }

        public bool TryRemoveItem(ItemInstance item)
        {
            return items.Remove(item);
        }

        public List<ItemInstance> GetAllItems() => new List<ItemInstance>(items);
        public int GetCapacity() => slotCount;
        public int GetCurrentCount() => items.Count;

        // Get the sell price for an item on this shelf.
        public float GetSellPrice(ItemInstance item)
        {
            return item?.Definition?.BaseSellPrice ?? 0f;
        }
    }
}