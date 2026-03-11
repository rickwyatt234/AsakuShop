using System;
using System.Collections.Generic;
using UnityEngine;

namespace AsakuShop.Items
{
    // Authored definition of a crafting recipe. Describes which ingredients are consumed, what item is produced, 
    // how many units are created per batch, and how long the craft takes. The output quality is computed
    // dynamically from ingredient grades at craft time.
    [CreateAssetMenu(fileName = "NewRecipe", menuName = "AsakuShop/Recipe Definition")]
    public class RecipeDefinition : ScriptableObject
    {
        // A single ingredient requirement within a recipe: which item is needed and how many units are consumed per batch.
        [Serializable]
        public struct RecipeIngredient
        {
            [Tooltip("ItemId of the ingredient (must match an ItemDefinition.ItemId in the ItemRegistry).")]
            public string ItemId;
            [Tooltip("Number of units consumed per batch.")]
            public int Quantity;
        }

        [Tooltip("Globally unique identifier for this recipe (example: recipe_onigiri_salmon).")]
        public string RecipeId;

        [Tooltip("Display name shown in the crafting menu.")]
        public string DisplayName;

        [Tooltip("Ingredients consumed per batch. Each entry needs an ItemId and a Quantity.")]
        public RecipeIngredient[] Ingredients;

        [Tooltip("ItemId of the item produced (must match an ItemDefinition.ItemId in the ItemRegistry).")]
        public string OutputItemId;

        [Tooltip("Number of output items produced per batch (minimum 1).")]
        public int BatchSize = 1;

        [Tooltip("Real-time duration of the craft in seconds (typical range: 3 to 5).")]
        public float CraftTimeSeconds = 3f;

        // Computes the output grade for a crafted item based on the grades of
        // the ingredients used. The output grade is the arithmetic average of
        // all input ingredient grades, rounded to the nearest tier and clamped
        // to the valid range. This is the canonical quality calculation for all
        // crafted items.
        public ItemGrade ComputeOutputGrade(IEnumerable<ItemGrade> ingredientGrades)
        {
            return ItemGradeExtensions.Average(ingredientGrades);
        }

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