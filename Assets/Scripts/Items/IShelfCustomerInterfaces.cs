using UnityEngine;

namespace AsakuShop.Items
{
    // Implemented by ShelfComponent so Customer AI can navigate to the correct
    // standing position in front of the shelf without AsakuShop.Customers needing
    // a direct reference to AsakuShop.Storage.
    public interface IShelfFrontPoint
    {
        /// <summary>World-space position a customer's NavMeshAgent should walk to before browsing.</summary>
        Vector3 FrontPoint { get; }
    }

    // Implemented by ShelfComponent so Customer AI can peek at and take items
    // without AsakuShop.Customers needing a direct reference to AsakuShop.Storage.
    public interface IShelfItemProvider
    {
        /// <summary>Returns the first available item without removing it. Returns null if empty.</summary>
        ItemInstance PeekItem();

        /// <summary>Removes and returns the first available item. Returns null if empty.</summary>
        ItemInstance TakeItem();
    }
}
