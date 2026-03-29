using UnityEngine;
using AsakuShop.Items;
using AsakuShop.Core;

namespace AsakuShop.Storage
{
    // Listens for ItemPriceRegistry.OnPriceChanged and immediately updates
    // CurrentPrice on every live ItemInstance in the scene whose
    // Definition.ItemId matches the changed item type.
    //
    // Attach this component to any persistent GameObject in the scene
    // (e.g. a GameManager object). Only one instance should exist at a time.
    public class ShelfPriceSyncManager : MonoBehaviour
    {
        public static ShelfPriceSyncManager Instance { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void RegisterBootstrapper()
        {
            GameBootstrapper.RegisterBootstrapper(() =>
            {
                if (Instance != null) return;
                GameObject go = new GameObject("[ShelfPriceSyncManager]");
                go.AddComponent<ShelfPriceSyncManager>();
                // DontDestroyOnLoad is called inside ShelfPriceSyncManager.Awake().
            });
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnEnable()
        {
            ItemPriceRegistry.OnPriceChanged += HandlePriceChanged;
        }

        private void OnDisable()
        {
            ItemPriceRegistry.OnPriceChanged -= HandlePriceChanged;
        }

        // Iterates every ItemPickup currently in the scene and updates CurrentPrice
        // on instances whose ItemId matches the changed item type.
        private void HandlePriceChanged(string itemId, float newPrice)
        {
            ItemPickup[] pickups = FindObjectsByType<ItemPickup>(FindObjectsSortMode.None);
            foreach (ItemPickup pickup in pickups)
            {
                if (pickup.ItemInstance != null
                    && pickup.ItemInstance.Definition != null
                    && pickup.ItemInstance.Definition.ItemId == itemId)
                {
                    pickup.ItemInstance.CurrentPrice = newPrice;
                }
            }
        }
    }
}
