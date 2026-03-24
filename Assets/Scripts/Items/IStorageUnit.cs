using System.Collections.Generic;

namespace AsakuShop.Items
{

    public interface IStorageUnit
    {
        bool CanAddItem(ItemInstance item);
        bool TryAddItem(ItemInstance item);
        bool TryRemoveItem(ItemInstance item);
        List<ItemInstance> GetAllItems();
        int GetCapacity();
        int GetCurrentCount();
        float GetCurrentWeight();
        void ClearAllItems();
    }

}