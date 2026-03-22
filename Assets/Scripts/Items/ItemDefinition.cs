using UnityEngine;

namespace AsakuShop.Items
{
    [CreateAssetMenu(fileName = "NewItem", menuName = "AsakuShop/Item Definition")]
    public class ItemDefinition : ScriptableObject
    {
        // Use snake_case, e.g. "item_onigiri_salmon".
        [Tooltip("Globally unique string identifier (snake_case). Used as the key in ItemRegistry.")]
        public string ItemId;

        [Tooltip("Human-readable display name shown in menus and HUD.")]
        public string DisplayName;

        [Tooltip("Short flavor text shown on the item inspect screen.")]
        public string Description;

        [Tooltip("Broad category: DryGood, Ingredient, Crafted, Stationery, or Consumable.")]
        public ItemCategory Category;

        [Tooltip("Storage requirement: Dry, Refrigerated, or Frozen.")]
        public PreferredStorageType PreferredStorageType;

        [Tooltip("Minimum shelf size needed to stock this item. Small fits any shelf; Large requires a large shelf.")]
        public StockingSize StockingSize;

        [Tooltip("How much this item weighs in kilograms. For backpack and other storage")]
        public float WeightKg;

        [Tooltip("Starting grade for new instances of this item.")]
        public ItemGrade BaseGrade;

        [Tooltip("Default wholesale cost in yen (what the player pays to acquire this item).")]
        public float BasePrice;

        [Tooltip("Flags that declare which external systems (weather, trends, events, etc.) can influence demand for this item.")]
        public DemandFactorFlags DemandFactors;

        [Tooltip("If true, this item is able to be purchased in crates as well as loose and each unit is individually graded on purchase.")]
        public bool IsCrateItem;

        [Tooltip("If true, this item is produced by a recipe (not purchased directly).")]
        public bool IsCraftingOutput;

        [Tooltip("If true, this item can be used as an ingredient in crafting recipes.")]
        public bool IsCraftingIngredient;

        [Tooltip("How this item looks in the world")]
        public GameObject WorldPrefab;

        public bool IsPerishable => PreferredStorageType != PreferredStorageType.Dry;


        private void OnValidate()
        {
            if (string.IsNullOrEmpty(ItemId))
                Debug.LogWarning($"[ItemDefinition] '{name}' has an empty ItemId. Set a unique snake_case identifier.", this);
        }
    }
}