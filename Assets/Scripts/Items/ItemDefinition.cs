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
        public StorageType StorageType;

        [Tooltip("Minimum shelf size needed to stock this item. Small fits any shelf; Large requires a large shelf.")]
        public StockingSize StockingSize;

        [Tooltip("How much this item weighs in kilograms. For backpack and other storage")]
        public float WeightKg;

        [Tooltip("Starting grade for new instances of this item.")]
        public ItemGrade BaseGrade;

        [Tooltip("Default wholesale cost in yen (what the player pays to acquire this item).")]
        public float BaseBuyPrice;

        [Tooltip("Default retail sell price in yen (what the player charges customers).")]
        public float BaseSellPrice;

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

        [Tooltip("UI icon sprite. Can be null in early development — UI should show a placeholder.")]
        public Sprite Icon;

        public bool IsPerishable => StorageType != StorageType.Dry;

        // Returns true if this item is currently in optimal storage conditions 
        // (e.g. refrigerated items are currently being stored in a refrigerator).
        // Spoilage timer doubled when true.

        // public bool IsInOptimalStorageConditions => StorageType switch
        // {
        //     StorageType.Dry => true, // Always optimal
        //     StorageType.Refrigerated => StorageManager.Instance.IsRefrigerated, 
        //     StorageType.Frozen => StorageManager.Instance.IsFrozen,
        //     _ => true
        // };


        private void OnValidate()
        {
            if (string.IsNullOrEmpty(ItemId))
                Debug.LogWarning($"[ItemDefinition] '{name}' has an empty ItemId. Set a unique snake_case identifier.", this);

            if (BaseSellPrice < BaseBuyPrice)
                Debug.LogWarning(
                    $"[ItemDefinition] '{name}' ({ItemId}): BaseSellPrice ({BaseSellPrice}¥) is less than BaseBuyPrice ({BaseBuyPrice}¥). Negative margin — is this intentional?",
                    this);
        }
    }
}