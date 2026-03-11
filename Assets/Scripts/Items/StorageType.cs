namespace AsakuShop.Items
{
    /// <summary>
    /// Describes where an item must be stored to remain in good condition.
    /// Determines which physical storage fixtures can hold this item and
    /// whether the overnight spoilage pass applies.
    /// </summary>
    public enum StorageType
    {
        /// <summary>
        /// Shelf-stable item stored at room temperature. No special storage
        /// fixture required and no passive quality decay from temperature.
        /// </summary>
        Dry,

        /// <summary>
        /// Requires a refrigerated case or fridge. Covers fresh produce,
        /// dairy, pre-made sandwiches, and similar perishables.
        /// </summary>
        Refrigerated,

        /// <summary>
        /// Requires a freezer. Covers frozen meals, ice cream, and any item
        /// that must remain frozen to maintain grade.
        /// </summary>
        Frozen,
    }
}
