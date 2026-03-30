using AsakuShop.Core;
using UnityEngine;

namespace AsakuShop.Items
{
    // Data class for furniture items that don't have any special functionality
    // beyond being picked up and placed. This is separate from ItemInstance because
    // furniture items don't have grades, prices, or other properties that normal items have.

    [CreateAssetMenu(fileName = "NewFurnitureDefinition", menuName = "AsakuShop/Furniture Definition")]
    public class FurnitureDefinition : ScriptableObject
    {
        public string ItemId;
        public string DisplayName;
        public int BasePrice;
        public GameObject WorldPrefab;
        private void OnValidate()
        {
            if (string.IsNullOrEmpty(ItemId))
                Debug.LogWarning($"[ItemDefinition] '{name}' has an empty ItemId. Set a unique snake_case identifier.", this);
        }
    }
}
