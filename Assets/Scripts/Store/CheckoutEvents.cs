using UnityEngine;
using AsakuShop.Items;
using System;

namespace AsakuShop.Store
{
    public static class CheckoutEvents
    {
        public delegate void ItemScannedDelegate(ItemInstance item);
        public static event ItemScannedDelegate OnItemScanned;

        public static void FireItemScanned(ItemInstance item)
        {
            OnItemScanned?.Invoke(item);
        }
    }
}