using AsakuShop.Items;
using UnityEngine;

namespace AsakuShop.Storage
{
    [System.Serializable]
    public class StorageItemEntry
    {
        public ItemInstance itemInstance;
        public Vector2 uiPosition; // Local position inside inventory window
    }
}