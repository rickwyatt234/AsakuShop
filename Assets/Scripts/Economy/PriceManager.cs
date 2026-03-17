using UnityEngine;
using AsakuShop.Items;
using System.Collections.Generic;

namespace AsakuShop.Economy
{
    //Item pricing system that allows for base prices and dynamic overrides
    //This can be expanded in the future to include demand/supply adjustments, discounts, etc.
    public class PriceManager : MonoBehaviour
    {
        private static PriceManager _instance;
        public static PriceManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    GameObject go = new GameObject("[PriceManager]");
                    _instance = go.AddComponent<PriceManager>();
                }
                return _instance;
            }
        }

        [SerializeField] private List<ItemPricingData> pricingData = new();

        private Dictionary<string, float> itemPrices = new(); // ItemId -> SellPrice

        [System.Serializable]
        public struct ItemPricingData
        {
            public ItemDefinition itemDefinition;
            public float overrideSellPrice; // 0 = use BaseSellPrice
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);

            InitializePrices();
        }

        private void InitializePrices()
        {
            itemPrices.Clear();

            // Load prices from inspector data
            foreach (var data in pricingData)
            {
                if (data.itemDefinition != null)
                {
                    float price = data.overrideSellPrice > 0 
                        ? data.overrideSellPrice 
                        : data.itemDefinition.BaseSellPrice;
                    
                    itemPrices[data.itemDefinition.ItemId] = price;
                }
            }
        }

        public float GetSellPrice(ItemDefinition item)
        {
            if (item == null)
                return 0f;

            if (itemPrices.TryGetValue(item.ItemId, out float price))
                return price;

            // Fallback to BaseSellPrice if not in override table
            return item.BaseSellPrice;
        }

        public float GetSellPrice(ItemInstance item)
        {
            return item?.Definition != null ? GetSellPrice(item.Definition) : 0f;
        }

        // Dynamic price adjustment (for future demand/supply)
        public void SetSellPrice(string itemId, float newPrice)
        {
            itemPrices[itemId] = newPrice;
            Debug.Log($"Price updated: {itemId} = ¥{newPrice}");
        }
    }
}