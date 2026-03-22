namespace AsakuShop.Items
{
    // Enum representing preferred storage type for storage containers
    // containers can hold all types of items, but this indicates the ideal storage conditions for items placed inside
    public enum PreferredStorageType
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
    }
}