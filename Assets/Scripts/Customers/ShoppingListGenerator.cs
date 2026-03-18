using UnityEngine;
using System.Collections.Generic;
using AsakuShop.Items;
using AsakuShop.Core;

namespace AsakuShop.Customers
{
    public static class ShoppingListGenerator
    {
        /// Generates a shopping list with 1-6 items, weighted towards lower quantities
        public static List<ItemInstance> GenerateShoppingList(List<ItemDefinition> availableItems)
        {
            if (availableItems == null || availableItems.Count == 0)
                return new List<ItemInstance>();

            // Weighted random count (1-6 items, biased to lower end)
            int itemCount = GetWeightedRandomCount();
            List<ItemInstance> list = new();

            for (int i = 0; i < itemCount; i++)
            {
                ItemDefinition randomDef = availableItems[Random.Range(0, availableItems.Count)];
                // Create a new ItemInstance from the definition
                ItemInstance item = new ItemInstance(randomDef, GameClock.Instance.CurrentTime);
                list.Add(item);
            }

            return list;
        }

        private static int GetWeightedRandomCount()
        {
            float roll = Random.value; // 0-1

            // Weighted distribution: biased to lower end
            if (roll < 0.40f) return 1;      // 40% chance
            if (roll < 0.70f) return 2;      // 30% chance
            if (roll < 0.90f) return 3;      // 20% chance
            if (roll < 0.95f) return 4;      // 5% chance
            if (roll < 0.98f) return 5;      // 3% chance
            return 6;                         // 2% chance
        }
    }
}