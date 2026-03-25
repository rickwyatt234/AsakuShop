using UnityEngine;
using AsakuShop.Items;
using System.Collections.Generic;
using AsakuShop.Core;

namespace AsakuShop.Storage
{
    public class ShelfComponent : MonoBehaviour, IInteractable, IShelfHoldable
    {
        [SerializeField] private string shelfID = "Shelf001";
        public string DisplayName = "Shelf";

        [SerializeField] private Vector3 heldOffset = new Vector3(0, 0.5f, 0);
        [SerializeField] private Quaternion heldRotation = Quaternion.Euler(90, 0, 0);

        [SerializeField] private PreferredStorageType storageType = PreferredStorageType.Dry;
        public PreferredStorageType StorageType => storageType;

        public StockingSize[] allowedStockingSizes = new StockingSize[]
                               { StockingSize.Small, StockingSize.Medium, StockingSize.Large };

        public int slotColumns = 4;
        public int slotRows = 3;
        public float slotSpacingX = 0.3f;
        public float slotSpacingY = 0.3f;
        [SerializeField] private Vector3 stockingRotation = new Vector3(90, 0, 0);
        [SerializeField] private Vector3 stockingOffset = new Vector3(0, 0.5f, 0);
        public Vector3 GetStockingRotation() => stockingRotation;
        public Vector3 GetStockingOffset() => stockingOffset;

        public float mountOffsetDistance = 0.5f;
        public float browsingDistance = 2f;

        [SerializeField, Tooltip("Optional override: explicit world-space point a customer walks to before browsing this shelf. " +
                                        "If null, the point is auto-calculated as 'browsingDistance' in front of the shelf.")]
        private Transform customerApproachPoint;
        
        [SerializeField] private Vector3 slotStartOffset = new Vector3(-0.9f, 0.9f, 0);
        [SerializeField] private Vector3 rotationOffset = Vector3.zero;

        // IHoldable / IShelfHoldable implementation
        string IHoldable.DisplayName => DisplayName;
        Vector3 IHoldable.HeldOffset => heldOffset;
        Quaternion IHoldable.HeldRotation => heldRotation;
        GameObject IHoldable.GameObject => gameObject;
        Vector3 IShelfHoldable.RotationOffset => rotationOffset;
        float IShelfHoldable.MountOffsetDistance => mountOffsetDistance;

        // Keep public accessor for code within Storage namespace that uses RotationOffset directly
        public Vector3 RotationOffset => rotationOffset;

        private List<ItemInstance> items = new();
        public List<ItemInstance> Items => items;
        public Dictionary<ItemInstance, int> itemToSlotIndex = new();

        private IPickupTarget pickupTarget;
        private ShelfInteraction shelfInteraction;
        private bool IsMounted => IsShelfWallMounted(this);


/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
#region Unity Lifecycle
        private void Awake()
        {
            shelfInteraction = GetComponent<ShelfInteraction>();
        }

        private void Start()
        {
            pickupTarget = PlayerService.PickupTarget;
            if (pickupTarget == null)
                Debug.LogError("[ShelfComponent] No IPickupTarget registered in PlayerService. Make sure PlayerHands is in the scene.");
        }
#endregion

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
#region IInteractable Implementation
        public void OnInteract()
        {
            if (pickupTarget == null) return;
            if (InventoryState.IsOpen || IsShelfWallMounted(this) || pickupTarget.IsHoldingInteractable)
                return;

            pickupTarget.TryPickupInteractable(gameObject);
        }

        public void OnExamine()
        {
            // Pressing Examine on any shelf (including wall-mounted) with empty hands picks it up.
            // Stocked items are ejected inside TryPickupInteractable before the shelf is held.
            if (pickupTarget == null) return;
            if (InventoryState.IsOpen || pickupTarget.IsHoldingInteractable)
                return;

            pickupTarget.TryPickupInteractable(gameObject);
        }
#endregion

        public bool IsShelfWallMounted(ShelfComponent shelf)
        {
            if (shelf == null) return false;

            Rigidbody rb = shelf.GetComponent<Rigidbody>();
            if (rb == null || !rb.isKinematic) return false;

            Vector3[] directions = {
                -shelf.transform.forward,
                shelf.transform.forward,
                shelf.transform.right,
                -shelf.transform.right,
                shelf.transform.up,
                -shelf.transform.up
            };

            foreach (Vector3 direction in directions)
            {
                Ray wallCheckRay = new Ray(shelf.transform.position, direction);
                if (Physics.Raycast(wallCheckRay, out RaycastHit hit, 2f) &&
                    hit.collider.gameObject.layer == LayerMask.NameToLayer("Wall"))
                    return true;
            }
            return false;
        }

        public bool TryAddItem(ItemInstance item) => shelfInteraction != null && shelfInteraction.TryAddItem(item);
        public bool TryRemoveItem(ItemInstance item) => shelfInteraction != null && shelfInteraction.TryRemoveItem(item);
        public Vector3 GetSlotPosition(ItemInstance item) => shelfInteraction != null ? shelfInteraction.GetSlotPosition(item) : Vector3.zero;

        // Unparents every child GameObject that has an ItemPickup component and
        // enables physics on it (kinematic = false, useGravity = true, colliders re-enabled)
        // so the items fall to the floor when the shelf is picked up.
        public void EjectAllStockedItems()
        {
            // Collect all child ItemPickup references first to avoid modifying the hierarchy during iteration.
            List<ItemPickup> pickups = new List<ItemPickup>(GetComponentsInChildren<ItemPickup>());
            foreach (ItemPickup pickup in pickups)
            {
                pickup.transform.SetParent(null);

                if (pickup.TryGetComponent(out Rigidbody rb))
                {
                    rb.isKinematic = false;
                    rb.useGravity  = true;
                }

                foreach (Collider col in pickup.GetComponentsInChildren<Collider>())
                    col.enabled = true;
            }
        }

        // Returns the world-space position a customer's NavMeshAgent should navigate to
        // before picking an item from this shelf.
        // Uses the explicit customerApproachPoint override if assigned;
        // otherwise projects browsingDistance units in front of the shelf's
        // forward direction from the shelf's centre.
        public Vector3 GetCustomerApproachPoint()
        {
            if (customerApproachPoint != null)
                return customerApproachPoint.position;
        
            // Default: stand in front of the shelf face
            return transform.position + transform.forward * browsingDistance;
        }

        // Called by PlayerHands after a successful wall-mount placement.
        // Actual StoreManager registration is handled by PlayerHands (which references
        // both AsakuShop.Storage and AsakuShop.Store), keeping Storage free of a Store dependency.
        public void NotifyMounted()
        {
            // Hook: override in editor or subclass if needed.
        }

        // Called by PlayerHands before the shelf is picked up off the wall.
        // Actual StoreManager unregistration is handled by PlayerHands.
        public void NotifyPickedUp()
        {
            // Hook: override in editor or subclass if needed.
        }
    }
}
