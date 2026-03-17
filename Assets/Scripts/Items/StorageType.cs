namespace AsakuShop.Items
{
    // Describes where an item must be stored to remain in good condition. Determines which physical 
    // storage fixtures can hold this item and whether the overnight spoilage pass applies.
    public enum StorageType
    {
        // Shelf-stable item stored at room temperature. No special storage
        // fixture required and no passive quality decay from temperature.
        Dry,

        // Requires a refrigerated case or fridge. Covers fresh produce,
        // dairy, pre-made sandwiches, and similar perishables.
        Refrigerated,

        // Requires a freezer. Covers frozen meals, ice cream, and any item
        // that must remain frozen to maintain grade.
        Frozen,

        FreeForm // For storage containers that can hold multiple types of items, like shelves and cabinets
    }
}