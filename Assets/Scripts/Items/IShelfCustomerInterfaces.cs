using UnityEngine;

namespace AsakuShop.Items
{
// Carries both logical item data and its current world pickup object.
    public readonly struct ShelfTakeResult
    {
        public readonly ItemInstance Item;
        public readonly ItemPickup Pickup;

        public bool HasItem => Item != null;

        public ShelfTakeResult(ItemInstance item, ItemPickup pickup)
        {
            Item = item;
            Pickup = pickup;
        }
    }

    // Implemented by ShelfContainer so Customer AI can navigate to the correct
    // standing position in front of the shelf without AsakuShop.Customers needing
    // a direct reference to AsakuShop.Storage.
    public interface IShelfFrontPoint
    {
        Vector3 FrontPoint { get; }
    }

    // Implemented by ShelfContainer so Customer AI can peek at and take items
    // without AsakuShop.Customers needing a direct reference to AsakuShop.Storage.
    public interface IShelfItemProvider
        {
            ItemInstance PeekItem();

            // Returns both the item data and its pickup visual object (if found).
            ShelfTakeResult TakeItem();
        }
}