using UnityEngine;
using AsakuShop.Items;

namespace AsakuShop.Storage
{
    public class ShelfInteraction : MonoBehaviour
    {
        private ShelfComponent shelfComponent;

        private void OnEnable()
        {
            shelfComponent = GetComponent<ShelfComponent>();
        }

        public bool TryPlaceItem(ItemInstance item)
        {
            if (shelfComponent == null || item == null)
                return false;

            // Don't allow storage containers on shelves
            if (item.Definition.StorageType == StorageType.FreeForm)
                return false;

            if (shelfComponent.TryAddItem(item))
            {
                return true;
            }

            return false;
        }

        public Vector3 GetItemPositionOnShelf(ItemInstance item)
        {
            if (shelfComponent != null)
                return shelfComponent.GetSlotPosition(item);
            return transform.position;
        }
    }
}