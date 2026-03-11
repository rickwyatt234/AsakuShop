using UnityEngine;

namespace AsakuShop.Items
{
    /// <summary>
    /// The static, authored definition of an item type. One
    /// <see cref="ItemDefinition"/> ScriptableObject exists per distinct item in
    /// the game (e.g. "Salmon Onigiri", "Grade-A Rice", "Blue Pen"). Runtime
    /// inventory is managed via <see cref="ItemInstance"/> objects that each
    /// hold a reference to their definition.
    /// </summary>
    [CreateAssetMenu(fileName = "NewItem", menuName = "AsakuShop/Item Definition")]
    public class ItemDefinition : ScriptableObject
    {
        // ── Identity ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Globally unique string identifier used as the lookup key in
        /// <see cref="ItemRegistry"/>. Use snake_case, e.g.
        /// <c>"item_onigiri_salmon"</c>.
        /// </summary>
        [Tooltip("Globally unique string identifier (snake_case). Used as the key in ItemRegistry.")]
        public string ItemId;

        /// <summary>Human-readable name shown in all UI elements.</summary>
        [Tooltip("Human-readable display name shown in menus and HUD.")]
        public string DisplayName;

        /// <summary>
        /// Short flavour / inspect text shown when the player examines the item.
        /// </summary>
        [Tooltip("Short flavour text shown on the item inspect screen.")]
        public string Description;

        // ── Classification ────────────────────────────────────────────────────────

        /// <summary>Broad category that groups this item for filtering and rules.</summary>
        [Tooltip("Broad category: DryGood, Ingredient, Crafted, Stationery, or Consumable.")]
        public ItemCategory Category;

        /// <summary>Storage requirement that determines which fixtures can hold this item.</summary>
        [Tooltip("Storage requirement: Dry, Refrigerated, or Frozen.")]
        public StorageType StorageType;

        /// <summary>
        /// Physical footprint tier of this item. Determines the minimum shelf or
        /// fixture size required to stock it on the shop floor. Small items fit
        /// anywhere; Large items require large shelving only.
        /// </summary>
        [Tooltip("Minimum shelf size needed to stock this item. Small fits any shelf; Large requires a large shelf.")]
        public StockingSize StockingSize;

        /// <summary>
        /// The quality tier this item starts at when first created or purchased.
        /// For dry goods this is a fixed authored value; for ingredients it acts
        /// as the baseline for the per-crate quality roll.
        /// </summary>
        [Tooltip("Starting grade for new instances of this item.")]
        public ItemGrade BaseGrade;

        // ── Pricing ───────────────────────────────────────────────────────────────

        /// <summary>
        /// Default wholesale cost in yen paid when purchasing this item from a
        /// supplier or market stall.
        /// </summary>
        [Tooltip("Default wholesale cost in yen (what the player pays to acquire this item).")]
        public float BaseBuyPrice;

        /// <summary>Default retail price in yen charged to customers at the counter.</summary>
        [Tooltip("Default retail sell price in yen (what the player charges customers).")]
        public float BaseSellPrice;

        // ── Demand ────────────────────────────────────────────────────────────────

        /// <summary>
        /// Declares which external systems may influence demand for this item at
        /// runtime. The actual calculation is performed by
        /// <c>AsakuShop.Economy</c> / <c>AsakuShop.Markets</c> — these flags
        /// only indicate <em>eligibility</em>.
        /// </summary>
        [Tooltip("Flags that declare which external systems (weather, trends, events, etc.) can influence demand for this item.")]
        public DemandFactorFlags DemandFactors;

        // ── Crafting / purchasing flags ───────────────────────────────────────────

        /// <summary>
        /// When <c>true</c> this item is typically purchased in crates; each
        /// unit within a crate receives an individually rolled grade on purchase.
        /// </summary>
        [Tooltip("If true, this item is purchased in crates and each unit is individually graded on purchase.")]
        public bool IsCrateItem;

        /// <summary>
        /// <c>true</c> if this item is produced by a crafting recipe and is not
        /// purchased directly from suppliers.
        /// </summary>
        [Tooltip("If true, this item is produced by a recipe (not purchased directly).")]
        public bool IsCraftingOutput;

        /// <summary>
        /// <c>true</c> if this item can be consumed as an input ingredient in a
        /// crafting recipe.
        /// </summary>
        [Tooltip("If true, this item can be used as an ingredient in crafting recipes.")]
        public bool IsCraftingIngredient;

        // ── Visuals ───────────────────────────────────────────────────────────────

        /// <summary>
        /// UI icon for this item. May be <c>null</c> during early development;
        /// the UI should fall back to a placeholder sprite in that case.
        /// </summary>
        [Tooltip("UI icon sprite. Can be null in early development — UI should show a placeholder.")]
        public Sprite Icon;

        // ── Derived properties ────────────────────────────────────────────────────

        /// <summary>
        /// Returns <c>true</c> when this item has a temperature-controlled
        /// storage requirement (i.e. <see cref="StorageType"/> is not
        /// <see cref="AsakuShop.Items.StorageType.Dry"/>). Used by storage
        /// placement and overnight spoilage systems to determine whether grade
        /// decay applies.
        /// </summary>
        public bool IsPerishable => StorageType != AsakuShop.Items.StorageType.Dry;

        // ── Editor validation ─────────────────────────────────────────────────────

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
