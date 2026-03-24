using UnityEngine;
using AsakuShop.Core;

namespace AsakuShop.Items
{
    public class ItemPickup : MonoBehaviour, IInteractable
    {
        public ItemInstance ItemInstance { get; private set; }
        private IPickupTarget pickupTarget;

        public void Initialize(ItemInstance itemInstance)
        {
            ItemInstance = itemInstance;

            //Create rigidbody and collider for interaction
            Rigidbody rb = gameObject.AddComponent<Rigidbody>();
            rb.useGravity = true;
            MeshCollider collider = gameObject.AddComponent<MeshCollider>();
            collider.convex = true; // Convex is required for MeshColliders on non-static objects
        }

        private void Start()
        {
            pickupTarget = PlayerService.PickupTarget;
            if (pickupTarget == null)
                Debug.LogError("[ItemPickup] No IPickupTarget registered in PlayerService. Make sure PlayerHands is in the scene.");
        }

#region IInteractable implementation
        public void OnInteract()
        {
            if (ItemInstance != null && pickupTarget != null)
                pickupTarget.TryPickupInteractable(gameObject);
        }

        public void OnExamine()
        {
            if (ItemInstance != null)
                CoreEvents.RaiseExamineRequested(ItemInstance);
    
            if (ItemInstance != null && ItemInstance.IsOnAShelf)
            {
                // Set price from the shelf
            }
        }
#endregion
    }
}

