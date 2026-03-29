using UnityEngine;

namespace AsakuShop.Items
{

    // Provides shelf slot positioning data to callers (like PlayerHands) without
    // requiring them to import AsakuShop.Storage.
    // Implemented by Shelf.
    public interface IShelfLayout
    {
        Vector3 GetSlotPosition(ItemInstance item);
        Vector3 GetStockingOffset();
        Vector3 GetStockingRotation();
    }


}
