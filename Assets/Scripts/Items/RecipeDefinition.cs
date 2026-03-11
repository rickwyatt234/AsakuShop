using System;
using System.Collections.Generic;
using UnityEngine;

namespace AsakuShop.Items
{
    /// <summary>
    /// Authored definition of a crafting recipe. Describes which ingredients
    /// are consumed, what item is produced, how many units are created per
    /// batch, and how long the craft takes. The output quality is computed
    /// dynamically from ingredient grades at craft time.
    /// </summary>
    [CreateAssetMenu(fileName = "NewRecipe", menuName = "AsakuShop/Recipe Definition")]
    public class RecipeDefinition : ScriptableObject
    {
        // ── Nested types ──────────────────────────────────────────────────────────

        /// <summary>
        /// A single ingredient requirement within a recipe: which item is needed
        /// and how many units are consumed per batch.
        /// </summary>
        [Serializable]
        public struct RecipeIngredient
        {
            /// <summary>
            /// The <see cref="ItemDefinition.ItemId"/> of the required ingredient.
            /// Resolved at runtime via <see cref="ItemRegistry"/>.
            /// </summary>
            [Tooltip("ItemId of the ingredient (must match an ItemDefinition.ItemId in the ItemRegistry).")]
            public string ItemId;

            /// <summary>Number of units of this ingredient consumed per crafting batch.</summary>
            [Tooltip("Number of units consumed per batch.")]
            public int Quantity;
        }

        // ── Identity ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Globally unique identifier for this recipe, e.g.
        /// <c>"recipe_onigiri_salmon"</c>.
        /// </summary>
        [Tooltip("Globally unique identifier for this recipe (snake_case).")]
        public string RecipeId;

        /// <summary>Human-readable name shown in the crafting menu UI.</summary>
        [Tooltip("Display name shown in the crafting menu.")]
        public string DisplayName;

        // ── Ingredients ───────────────────────────────────────────────────────────

        /// <summary>
        /// The list of ingredients consumed per crafting batch. Each entry
        /// specifies an item ID and the quantity required.
        /// </summary>
        [Tooltip("Ingredients consumed per batch. Each entry needs an ItemId and a Quantity.")]
        public RecipeIngredient[] Ingredients;

        // ── Output ────────────────────────────────────────────────────────────────

        /// <summary>
        /// The <see cref="ItemDefinition.ItemId"/> of the item produced by this
        /// recipe. Resolved at runtime via <see cref="ItemRegistry"/>.
        /// </summary>
        [Tooltip("ItemId of the item produced (must match an ItemDefinition.ItemId in the ItemRegistry).")]
        public string OutputItemId;

        /// <summary>
        /// Number of output items created per successful craft. For example, a
        /// batch of onigiri might produce 6 units.
        /// </summary>
        [Tooltip("Number of output items produced per batch (minimum 1).")]
        public int BatchSize = 1;

        // ── Timing ────────────────────────────────────────────────────────────────

        /// <summary>
        /// How long this craft takes in real-world seconds. Typical range per
        /// GDD: 3–5 seconds per batch.
        /// </summary>
        [Tooltip("Real-time duration of the craft in seconds (typical range: 3–5).")]
        public float CraftTimeSeconds = 3f;

        // ── Public methods ────────────────────────────────────────────────────────

        /// <summary>
        /// Computes the output grade for a crafted item based on the grades of
        /// the ingredients used. The output grade is the arithmetic average of
        /// all input ingredient grades, rounded to the nearest tier and clamped
        /// to the valid range. This is the canonical quality calculation for all
        /// crafted items.
        /// </summary>
        /// <param name="ingredientGrades">
        /// The grades of the ingredients consumed in this craft. Must not be
        /// <c>null</c>.
        /// </param>
        /// <returns>
        /// The <see cref="ItemGrade"/> of the crafted output.
        /// </returns>
        public ItemGrade ComputeOutputGrade(IEnumerable<ItemGrade> ingredientGrades)
        {
            return ItemGradeExtensions.Average(ingredientGrades);
        }

        // ── Editor validation ─────────────────────────────────────────────────────

        private void OnValidate()
        {
            if (string.IsNullOrEmpty(RecipeId))
                Debug.LogWarning($"[RecipeDefinition] '{name}' has an empty RecipeId.", this);

            if (Ingredients == null || Ingredients.Length == 0)
                Debug.LogWarning($"[RecipeDefinition] '{name}' ({RecipeId}): Ingredients list is null or empty.", this);

            if (string.IsNullOrEmpty(OutputItemId))
                Debug.LogWarning($"[RecipeDefinition] '{name}' ({RecipeId}): OutputItemId is empty.", this);

            if (BatchSize < 1)
                Debug.LogWarning($"[RecipeDefinition] '{name}' ({RecipeId}): BatchSize ({BatchSize}) must be at least 1.", this);

            if (CraftTimeSeconds < 1f || CraftTimeSeconds > 10f)
                Debug.LogWarning($"[RecipeDefinition] '{name}' ({RecipeId}): CraftTimeSeconds ({CraftTimeSeconds}) is outside the expected 1–10 second range.", this);
        }
    }
}
