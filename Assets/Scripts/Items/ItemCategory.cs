namespace AsakuShop.Items
{
    /// <summary>
    /// Broad category that describes what kind of item this is.
    /// Used for filtering, display grouping, theft-risk calculations,
    /// and spoilage rules.
    /// </summary>
    public enum ItemCategory
    {
        /// <summary>
        /// Shelf-stable packaged goods: drinks, snacks, instant ramen,
        /// toiletries, and other pre-packaged consumables.
        /// </summary>
        DryGood,

        /// <summary>
        /// Raw market purchases: fresh produce, meat, fish, rice, oil,
        /// and other unprocessed ingredients used in crafting.
        /// </summary>
        Ingredient,

        /// <summary>
        /// All crafted output: onigiri, sandwiches, bento boxes, karaage,
        /// fruit cups, and any other player-made goods.
        /// </summary>
        Crafted,

        /// <summary>
        /// Stationery and collectibles: pens, notebooks, stickers, and
        /// gachapon prizes. High theft risk and low spoilage.
        /// </summary>
        Stationery,

        /// <summary>
        /// Catch-all for miscellaneous items not covered by other categories.
        /// Expandable in future updates.
        /// </summary>
        Consumable,
    }
}
