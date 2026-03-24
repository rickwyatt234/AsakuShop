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

            // Only add a Rigidbody if one doesn't already exist on this GameObject.
            if (!gameObject.TryGetComponent<Rigidbody>(out _))
            {
                Rigidbody rb = gameObject.AddComponent<Rigidbody>();
                rb.useGravity = true;
            }

            // Only add a MeshCollider if no Collider of any type already exists.
            if (!gameObject.TryGetComponent<Collider>(out _))
            {
                MeshCollider collider = gameObject.AddComponent<MeshCollider>();
                collider.convex = true; // Convex is required for MeshColliders on non-static objects
            }
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
            // Examination of loose or shelved items via the world raycast is not allowed.
            // The examine UI is only opened by PlayerHands when the item is actively held.
        }
#endregion
    }
}

