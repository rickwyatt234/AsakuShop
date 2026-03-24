using AsakuShop.Items;
using UnityEngine;

namespace AsakuShop.Storage
{
    // This class represents an individual item entry in the storage inventory, 
    // including its position in the UI.
    [System.Serializable]
    public class StorageItemEntry
    {
        public ItemInstance itemInstance;
        public Vector2 uiPosition; // Local position inside inventory window
    }
}