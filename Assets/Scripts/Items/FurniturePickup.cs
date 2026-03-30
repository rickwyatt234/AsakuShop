using UnityEngine;
using AsakuShop.Core;

namespace AsakuShop.Items
{
    // Added to furniture or miscellaneous item prefabs
    // that do not have a purpose other than picking up and placing.
    // Can include things like checkout counters
    public class FurniturePickup : MonoBehaviour, IInteractable
    {
        public FurnitureInstance FurnitureInstance { get; private set; }
        private IPickupTarget pickupTarget;

        public void Initialize(FurnitureInstance furnitureData)
        {
            FurnitureInstance = furnitureData;

            // Only add a Rigidbody if one doesn't already exist on this GameObject.
            if (!gameObject.TryGetComponent<Rigidbody>(out _))
            {
                Rigidbody rb = gameObject.AddComponent<Rigidbody>();
                rb.useGravity = true;
            }

            // Only add a Collider if no Collider of any type already exists.
            if (!gameObject.TryGetComponent<Collider>(out _))
            {
                BoxCollider collider = gameObject.AddComponent<BoxCollider>();
                collider.isTrigger = false;
            }
        }

        private void Start()
        {
            pickupTarget = PlayerService.PickupTarget;
            if (pickupTarget == null)
                Debug.LogError("[ItemPickup] No IPickupTarget registered in PlayerService. Make sure PlayerHands is in the scene.");
        }

#region IInteractable implementation
        public void OnInteract() { }

        public void OnExamine()
        {
            if (FurnitureInstance != null && pickupTarget != null)
                pickupTarget.TryPickupInteractable(gameObject);
        }
#endregion
    }
}

