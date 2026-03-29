using System;
using System.Collections.Generic;

namespace AsakuShop.Items
{
    // Stores the player-configured retail price for each item type, keyed by
    // ItemDefinition.ItemId. All future ItemInstance objects created for a given
    // type will start at the price stored here. Existing live instances are
    // updated by ShelfPriceSyncManager when OnPriceChanged fires.
    public static class ItemPriceRegistry
    {
        private static readonly Dictionary<string, float> _overrides = new();

        // Fired whenever a price entry is added or updated.
        // Parameters: itemId, newPrice.
        public static event Action<string, float> OnPriceChanged;

        // Records a player-set price for an item type, replacing any previous value.
        // Fires OnPriceChanged so that live instances can be updated by listeners.
        public static void SetPrice(string itemId, float newPrice)
        {
            if (string.IsNullOrEmpty(itemId)) return;

            _overrides[itemId] = newPrice;
            OnPriceChanged?.Invoke(itemId, newPrice);
        }

        // Returns the player-set price override for this definition if one exists,
        // otherwise falls back to ItemDefinition.BasePrice.
        public static float GetEffectivePrice(ItemDefinition definition)
        {
            if (definition == null) return 0f;

            return _overrides.TryGetValue(definition.ItemId, out float price)
                ? price
                : definition.BasePrice;
        }

        // Returns true and fills price if a player-set override exists for itemId.
        public static bool TryGetPrice(string itemId, out float price)
        {
            return _overrides.TryGetValue(itemId, out price);
        }

        // Exposes a read-only view of all current overrides — used by the save system.
        public static IReadOnlyDictionary<string, float> AllOverrides => _overrides;

        // Replaces the entire override table — used when loading a saved game.
        public static void LoadOverrides(Dictionary<string, float> overrides)
        {
            _overrides.Clear();
            if (overrides == null) return;

            foreach (var kvp in overrides)
                _overrides[kvp.Key] = kvp.Value;
        }

        // Removes all overrides — call on scene teardown to prevent stale data.
        public static void Clear() => _overrides.Clear();
    }
}
