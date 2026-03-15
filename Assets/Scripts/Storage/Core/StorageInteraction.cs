using UnityEngine;
using AsakuShop.Core;

namespace AsakuShop.Storage
{
    public class StorageInteraction : MonoBehaviour, IInteractable
    {
        private StorageContainer container;

        private void Start()
        {
            container = GetComponent<StorageContainer>();
        }

        public void OnInteract()
        {
            if (container != null)
            {
                container.OpenInventory();
            }
        }
    }
}