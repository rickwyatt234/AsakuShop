using System.Collections.Generic;
using UnityEngine;

namespace AsakuShop.Items
{
    /// <summary>
    /// Singleton runtime registry that provides fast lookup of
    /// <see cref="ItemDefinition"/> ScriptableObjects by their string
    /// <see cref="ItemDefinition.ItemId"/>. Persists across scene loads.
    /// </summary>
    /// <remarks>
    /// Populate <see cref="AllItems"/> by dragging all item ScriptableObjects
    /// into the Inspector array, or let the registry auto-populate at startup
    /// by loading all assets from a <c>Resources/Items</c> folder when the
    /// array is empty.
    /// </remarks>
    [DefaultExecutionOrder(-800)]
    public class ItemRegistry : MonoBehaviour
    {
        // ── Singleton ────────────────────────────────────────────────────────────

        /// <summary>Singleton instance of the <see cref="ItemRegistry"/>.</summary>
        public static ItemRegistry Instance { get; private set; }

        // ── Serialised fields ─────────────────────────────────────────────────────

        /// <summary>
        /// All <see cref="ItemDefinition"/> assets known to the registry. If
        /// left empty in the Inspector the registry will attempt to load all
        /// definitions from <c>Resources/Items/</c> at runtime.
        /// </summary>
        [Tooltip("All ItemDefinition assets. Leave empty to auto-load from Resources/Items/ at runtime.")]
        public ItemDefinition[] AllItems;

        // ── Private state ─────────────────────────────────────────────────────────

        private Dictionary<string, ItemDefinition> _registry;

        // ── Unity lifecycle ──────────────────────────────────────────────────────

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

        // ── Public API ───────────────────────────────────────────────────────────

        /// <summary>
        /// Returns the <see cref="ItemDefinition"/> with the given
        /// <paramref name="itemId"/>, or <c>null</c> if no match is found.
        /// Logs a warning when the ID is not registered.
        /// </summary>
        /// <param name="itemId">The unique item identifier to look up.</param>
        /// <returns>
        /// The matching <see cref="ItemDefinition"/>, or <c>null</c> if not
        /// found.
        /// </returns>
        public ItemDefinition Get(string itemId)
        {
            if (_registry.TryGetValue(itemId, out ItemDefinition definition))
                return definition;

            Debug.LogWarning($"[ItemRegistry] Item not found: '{itemId}'.");
            return null;
        }

        /// <summary>
        /// Attempts to retrieve the <see cref="ItemDefinition"/> with the given
        /// <paramref name="itemId"/> without logging a warning on miss.
        /// </summary>
        /// <param name="itemId">The unique item identifier to look up.</param>
        /// <param name="definition">
        /// When this method returns <c>true</c>, contains the matching
        /// definition; otherwise <c>null</c>.
        /// </param>
        /// <returns>
        /// <c>true</c> if the item was found; <c>false</c> otherwise.
        /// </returns>
        public bool TryGet(string itemId, out ItemDefinition definition)
        {
            return _registry.TryGetValue(itemId, out definition);
        }

        /// <summary>
        /// Returns a read-only list of all <see cref="ItemDefinition"/> objects
        /// whose <see cref="ItemDefinition.Category"/> matches
        /// <paramref name="category"/>.
        /// </summary>
        /// <param name="category">The category to filter by.</param>
        /// <returns>A read-only list, possibly empty, of matching definitions.</returns>
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

        // ── Private helpers ───────────────────────────────────────────────────────

        /// <summary>
        /// Builds the internal dictionary from <see cref="AllItems"/>. If the
        /// array is null or empty, attempts a fallback load from
        /// <c>Resources/Items/</c>.
        /// </summary>
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
