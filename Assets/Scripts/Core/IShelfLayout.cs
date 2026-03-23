using UnityEngine;
using AsakuShop.Items;

namespace AsakuShop.Core
{
    // Provides shelf slot positioning data to callers (like PlayerHands) without
    // requiring them to import AsakuShop.Storage.
    // Implemented by ShelfComponent.
    public interface IShelfLayout
    {
        Vector3 GetSlotPosition(ItemInstance item);
        Vector3 GetStockingOffset();
        Vector3 GetStockingRotation();
    }
}
