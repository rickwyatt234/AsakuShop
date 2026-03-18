using UnityEngine;
using AsakuShop.Items;

namespace AsakuShop.Store
{
    public class BaggingArea : MonoBehaviour
    {
        private void OnTriggerEnter(Collider other)
        {
            ItemPickup pickup = other.GetComponent<ItemPickup>();
            if (pickup != null && pickup.itemInstance != null)
            {
                // Remove from drop zone tracking
                CustomerItemDropZone dropZone = GetComponentInParent<CustomerItemDropZone>();
                if (dropZone != null)
                {
                    dropZone.RemoveItemFromCounter(other.gameObject);
                }

                Debug.Log($"[BAGGING] {pickup.itemInstance.Definition.DisplayName} bagged and removed");
                Destroy(other.gameObject);
            }
        }
    }
}