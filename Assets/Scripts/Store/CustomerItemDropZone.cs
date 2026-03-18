using UnityEngine;
using AsakuShop.Items;
using System.Collections.Generic;

namespace AsakuShop.Store
{
    public class CustomerItemDropZone : MonoBehaviour
    {
        private List<GameObject> itemsOnCounter = new();

        private void OnTriggerEnter(Collider other)
        {
            ItemInstance pickup = other.GetComponent<ItemInstance>();
            if (pickup != null && !itemsOnCounter.Contains(other.gameObject))
            {
                itemsOnCounter.Add(other.gameObject);
                Debug.Log($"[DROP ZONE] {pickup.Instance.Definition.DisplayName} placed on counter");
            }
        }

        private void OnTriggerExit(Collider other)
        {
            ItemInstance pickup = other.GetComponent<ItemInstance>();
            if (pickup != null && itemsOnCounter.Contains(pickup.Instance.gameObject))
            {
                itemsOnCounter.Remove(pickup.Instance.gameObject);
                Debug.LogWarning("[DROP ZONE] Item fell off counter");
            }
        }

        public void RemoveItemFromCounter(GameObject item)
        {
            if (itemsOnCounter.Contains(item))
            {
                itemsOnCounter.Remove(item);
                Debug.Log("[DROP ZONE] Item removed from counter (bagged)");
            }
        }

        public List<GameObject> GetItemsOnCounter() => new(itemsOnCounter);
    }
}