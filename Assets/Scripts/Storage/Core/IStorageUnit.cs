using AsakuShop.Items;
using System.Collections.Generic;

namespace AsakuShop.Storage
{
    public interface IStorageUnit
    {
        bool CanAddItem(ItemInstance item);
        bool TryAddItem(ItemInstance item);
        bool TryRemoveItem(ItemInstance item);
        List<ItemInstance> GetAllItems();
        int GetCapacity();
        int GetCurrentCount();
    }
}