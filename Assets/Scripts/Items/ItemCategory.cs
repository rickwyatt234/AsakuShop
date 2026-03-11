namespace AsakuShop.Items
{
    // Broad category that describes what kind of item this is.
    // Used for filtering, display grouping, theft-risk calculations, and spoilage rules.
    public enum ItemCategory
    {
        // Shelf-stable packaged goods: drinks, snacks, instant ramen, toiletries, and other pre-packaged consumables.
        DryGood,

        // Raw market purchases: fresh produce, meat, fish, rice, oil, and other unprocessed ingredients used in crafting.
        Ingredient,

        // All crafted output: onigiri, sandwiches, bento boxes, karaage, fruit cups, and any other player-made goods.
        Crafted,

        // Stationery and collectibles: pens, notebooks, stickers, and gachapon prizes. High theft risk and low spoilage.
        Stationery,

        // Catch-all for miscellaneous items not covered by other categories. Expandable in future updates.
        Consumable,
    }
}