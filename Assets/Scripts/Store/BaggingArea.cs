using UnityEngine;
using AsakuShop.Items;

namespace AsakuShop.Store
{
    public class BaggingArea : MonoBehaviour
    {
        private void OnTriggerEnter(Collider other)
        {
            ItemInstance pickup = other.GetComponent<ItemInstance>();
            if (pickup != null && pickup.Instance != null)
            {
                // Remove from drop zone tracking
                CustomerItemDropZone dropZone = GetComponentInParent<CustomerItemDropZone>();
                if (dropZone != null)
                {
                    dropZone.RemoveItemFromCounter(pickup.Instance.gameObject);
                }

                Debug.Log($"[BAGGING] {pickup.Instance.Definition.DisplayName} bagged and removed");
                Destroy(pickup.Instance.gameObject);
            }
        }
    }
}