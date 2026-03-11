using System.Collections.Generic;
using UnityEngine;

namespace AsakuShop.Items
{
    // Singleton runtime registry that provides fast lookup of 
    // ItemDefinition ScriptableObjects by their string ItemDefinition.ItemId. Persists across scene loads.
    // Populate AllItems by dragging all item ScriptableObjects into the Inspector array, or let the registry 
    // auto-populate at startup by loading all assets from a <c>Resources/Items</c> folder when the array is empty.

    [DefaultExecutionOrder(-800)]
    public class ItemRegistry : MonoBehaviour
    {
        public static ItemRegistry Instance { get; private set; }

        // All ItemDefinition assets known to the registry. If left empty in the Inspector the registry 
        // will attempt to load all definitions from Resources/Items/ at runtime.
        [Tooltip("All ItemDefinition assets. Leave empty to auto-load from Resources/Items/ at runtime.")]
        public ItemDefinition[] AllItems;

        private Dictionary<string, ItemDefinition> _registry;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogError("[ItemRegistry] Duplicate instance detected — destroying self.");
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            BuildRegistry();
        }

#region Public Methods
        // Returns the ItemDefinition with the given itemId, or null if no match is found.
        // Logs a warning when the ID is not registered.
        // itemId: The unique item identifier to look up.
        // The matching ItemDefinition, or null if not found.
        public ItemDefinition Get(string itemId)
        {
            if (_registry.TryGetValue(itemId, out ItemDefinition definition))
                return definition;

            Debug.LogWarning($"[ItemRegistry] Item not found: '{itemId}'.");
            return null;
        }

        // Attempts to retrieve the ItemDefinition with the given itemId without logging a warning on miss.
        // definition: When this method returns true, contains the matching definition; otherwise null.
        // Returns true if the item was found; false otherwise.
        public bool TryGet(string itemId, out ItemDefinition definition)
        {
            return _registry.TryGetValue(itemId, out definition);
        }

        // Returns a read-only list of all ItemDefinition objects whose Category matches the specified category.
        // category: The category to filter by. (e.g. ItemCategory.Ingredient, ItemCategory.Crafted, etc.)
        public IReadOnlyList<ItemDefinition> GetAllByCategory(ItemCategory category)
        {
            var result = new List<ItemDefinition>();
            foreach (var def in _registry.Values)
            {
                if (def.Category == category)
                    result.Add(def);
            }
            return result.AsReadOnly();
        }
#endregion

        private void BuildRegistry()
        {
            _registry = new Dictionary<string, ItemDefinition>();

            // Fall back to Resources if the inspector array wasn't populated.
            if (AllItems == null || AllItems.Length == 0)
            {
                AllItems = Resources.LoadAll<ItemDefinition>("Items");
                if (AllItems == null || AllItems.Length == 0)
                {
                    Debug.LogWarning("[ItemRegistry] No ItemDefinition assets found. Populate AllItems in the Inspector or place assets in Resources/Items/.");
                    return;
                }
            }

            foreach (var def in AllItems)
            {
                if (def == null)
                {
                    Debug.LogWarning("[ItemRegistry] Null entry found in AllItems — skipping.");
                    continue;
                }

                if (string.IsNullOrEmpty(def.ItemId))
                {
                    Debug.LogWarning($"[ItemRegistry] ItemDefinition '{def.name}' has an empty ItemId — skipping.");
                    continue;
                }

                if (_registry.ContainsKey(def.ItemId))
                {
                    Debug.LogWarning($"[ItemRegistry] Duplicate ItemId '{def.ItemId}' found on '{def.name}' — skipping duplicate.");
                    continue;
                }

                _registry[def.ItemId] = def;
            }
        }
    }
}