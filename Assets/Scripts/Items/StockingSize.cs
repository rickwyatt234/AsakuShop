namespace AsakuShop.Items
{
    /// <summary>
    /// Indicates the physical footprint tier of an item, which determines the
    /// minimum shelf or fixture size required to stock it on the shop floor.
    /// Shelves and storage fixtures declare which sizes they accept; the
    /// <c>AsakuShop.Storage</c> system uses this value to validate placements.
    /// </summary>
    /// <remarks>
    /// Think of the tiers as T-shirt sizes for shelf slots:
    /// <list type="bullet">
    ///   <item><description><see cref="Small"/> — individual units (a single can of soda, one pen, one onigiri)</description></item>
    ///   <item><description><see cref="Medium"/> — multi-packs or mid-size goods (a 12-pack of cans, a bento set, a bag of rice)</description></item>
    ///   <item><description><see cref="Large"/> — bulk or oversized goods (a wholesale case, a storage crate, a jumbo snack bag)</description></item>
    /// </list>
    /// A <see cref="Small"/> item fits on any shelf. A <see cref="Large"/> item
    /// requires a shelf rated for Large.
    /// </remarks>
    public enum StockingSize
    {
        /// <summary>
        /// Small footprint — fits on any shelf size (small, medium, or large).
        /// Examples: individual drink cans, single onigiri, pens, small snack bags.
        /// </summary>
        Small,

        /// <summary>
        /// Medium footprint — requires a medium or large shelf.
        /// Examples: 12-packs of cans, bento boxes, 1 kg bags of rice, large bottles.
        /// </summary>
        Medium,

        /// <summary>
        /// Large footprint — requires a large shelf or dedicated storage area only.
        /// Examples: wholesale cases, bulk ingredient sacks, oversized display items.
        /// </summary>
        Large
    }
}
