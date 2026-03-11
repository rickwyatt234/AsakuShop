namespace AsakuShop.Items
{
    public enum StockingSize
    {
        /// Small footprint — fits on any shelf size (small, medium, or large).
        /// Examples: individual drink cans, single onigiri, pens, small snack bags.
        Small,

        /// Medium footprint — requires a medium or large shelf.
        /// Examples: 12-packs of cans, bento boxes, 1 kg bags of rice, large bottles.
        Medium,

        /// Large footprint — requires a large shelf or dedicated storage area only.
        /// Examples: wholesale cases, bulk ingredient sacks, oversized display items.
        Large
    }
}