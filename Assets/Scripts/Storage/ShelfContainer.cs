using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using AsakuShop.Core;
using AsakuShop.Items;

namespace AsakuShop.Storage
{
    /// <summary>
    /// Top-level component for any shelving object (wall-mounted or floor-standing).
    /// Attach to the shelf mesh root alongside a BoxCollider for holding/placement.
    /// Each child shelf board should carry a <see cref="Shelf"/> component and its own
    /// BoxCollider for per-shelf stocking interaction.
    /// </summary>
    [RequireComponent(typeof(BoxCollider))]
    public class ShelfContainer : MonoBehaviour, IInteractable, IShelfHoldable, IMountableHoldable, IShelfItemProvider
    {
        [SerializeField] private string displayName = "Shelf";
        [SerializeField] private Vector3 heldOffset = new Vector3(0, 0.5f, 0);
        [SerializeField] private Quaternion heldRotation = Quaternion.Euler(90, 0, 0);
        [SerializeField] private Vector3 rotationOffset = Vector3.zero;
        [SerializeField] private float mountOffsetDistance = 0.5f;

        [SerializeField] private MountMode mountMode = MountMode.Either;
        [SerializeField] private MountSurfaceMask allowedSurfaces = MountSurfaceMask.Any;
        [SerializeField] private bool alignToSurfaceNormal = true;
        [SerializeField] private bool allowManualYawRotation = true;

        /// <summary>True once this unit has been placed on a surface (wall or floor).</summary>
        public bool IsMounted { get; private set; } = false;

        /// <summary>True while the unit is being carried by the player.</summary>
        public bool IsMoving { get; set; } = false;

        /// <summary>True when the unit is accessible. Always true for standard shelves; override for fridges.</summary>
        public virtual bool IsOpen { get; protected set; } = true;

        /// <summary>World-space point in front of the unit where customers stand to browse.</summary>
        public Vector3 Front => transform.TransformPoint(Vector3.forward);

        /// <summary>All child <see cref="Shelf"/> components, cached on Awake.</summary>
        public List<Shelf> Shelves { get; private set; } = new();

        // IHoldable
        string IHoldable.DisplayName => displayName;
        Vector3 IHoldable.HeldOffset => heldOffset;
        Quaternion IHoldable.HeldRotation => heldRotation;
        GameObject IHoldable.GameObject => gameObject;

        // IShelfHoldable
        Vector3 IShelfHoldable.RotationOffset => rotationOffset;
        float IShelfHoldable.MountOffsetDistance => mountOffsetDistance;

        // IMountableHoldable
        MountMode IMountableHoldable.MountMode => mountMode;
        MountSurfaceMask IMountableHoldable.AllowedSurfaces => allowedSurfaces;
        bool IMountableHoldable.AlignToSurfaceNormal => alignToSurfaceNormal;
        bool IMountableHoldable.AllowManualYawRotation => allowManualYawRotation;
        void IMountableHoldable.NotifyMounted() => NotifyMounted();
        void IMountableHoldable.NotifyPickedUp() => NotifyPickedUp();

#region Unity Lifecycle
        protected virtual void Awake()
        {
            Shelves = GetComponentsInChildren<Shelf>(true).ToList();
            foreach (var shelf in Shelves)
                shelf.ShelfContainer = this;
            Debug.Log($"[ShelfContainer] '{name}' awake — found {Shelves.Count} child Shelf(s).");
        }
#endregion

#region Mounting
        public virtual void NotifyMounted()
        {
            IsMounted = true;
            IsMoving = false;
            Shelves.ForEach(s => s.ToggleInteraction(true));
            Debug.Log($"[ShelfContainer] '{name}' mounted at {transform.position}. Shelf colliders enabled.");
        }

        public virtual void NotifyPickedUp()
        {
            IsMounted = false;
            IsMoving = true;
            Shelves.ForEach(s => s.ToggleInteraction(false));
            Debug.Log($"[ShelfContainer] '{name}' picked up. Shelf colliders disabled.");
        }

        /// <summary>Override in subclasses (e.g. Fridge) to open the unit door.</summary>
        public virtual void Open(bool forced, bool playSFX)
        {
            IsOpen = true;
            Debug.Log($"[ShelfContainer] '{name}' opened (forced={forced}).");
        }

        /// <summary>Override in subclasses (e.g. Fridge) to close the unit door.</summary>
        public virtual void Close(bool forced, bool playSFX)
        {
            IsOpen = false;
            Debug.Log($"[ShelfContainer] '{name}' closed (forced={forced}).");
        }
#endregion

#region IInteractable
        public void OnInteract()
        {
            Debug.Log($"[ShelfContainer] OnInteract called on '{name}'. IsMounted={IsMounted}, IsOpen={IsOpen}.");
            var pickupTarget = PlayerService.PickupTarget;
            if (pickupTarget == null)
            {
                Debug.LogWarning($"[ShelfContainer] OnInteract — no IPickupTarget found in PlayerService.");
                return;
            }
            if (InventoryState.IsOpen)
            {
                Debug.Log($"[ShelfContainer] OnInteract blocked — inventory is open.");
                return;
            }
            if (IsMounted)
            {
                Debug.Log($"[ShelfContainer] OnInteract blocked — unit is mounted; use Examine to pick it up.");
                return;
            }
            if (pickupTarget.IsHoldingInteractable)
            {
                Debug.Log($"[ShelfContainer] OnInteract blocked — player is already holding something.");
                return;
            }
            Debug.Log($"[ShelfContainer] '{name}' forwarding to TryPickupInteractable.");
            pickupTarget.TryPickupInteractable(gameObject);
        }

        public void OnExamine()
        {
            Debug.Log($"[ShelfContainer] OnExamine called on '{name}'. IsMounted={IsMounted}.");
            var pickupTarget = PlayerService.PickupTarget;
            if (pickupTarget == null)
            {
                Debug.LogWarning($"[ShelfContainer] OnExamine — no IPickupTarget found in PlayerService.");
                return;
            }
            if (InventoryState.IsOpen)
            {
                Debug.Log($"[ShelfContainer] OnExamine blocked — inventory is open.");
                return;
            }
            if (pickupTarget.IsHoldingInteractable)
            {
                Debug.Log($"[ShelfContainer] OnExamine blocked — player is already holding something.");
                return;
            }
            Debug.Log($"[ShelfContainer] '{name}' forwarding to TryPickupInteractable via Examine.");
            pickupTarget.TryPickupInteractable(gameObject);
        }
#endregion

#region IShelfItemProvider
        public ItemInstance PeekItem()
        {
            foreach (var shelf in Shelves)
            {
                var item = shelf.PeekItem();
                if (item != null)
                    return item;
            }
            return null;
        }

        public ShelfTakeResult TakeItem()
        {
            foreach (var shelf in Shelves)
            {
                if (shelf.GetCurrentCount() > 0)
                {
                    ShelfTakeResult result = shelf.TakeItem();
                    Debug.Log($"[ShelfContainer] '{name}' — customer took '{result.Item?.Definition?.DisplayName}' from child Shelf '{shelf.name}'. Pickup found={result.Pickup != null}.");
                    return result;
                }
            }
            Debug.Log($"[ShelfContainer] '{name}' — TakeItem called but all child shelves are empty.");
            return default;
        }
#endregion

#region Pickup Helpers
        /// <summary>
        /// Unparents every child ItemPickup and re-enables physics so items fall when the unit is picked up.
        /// </summary>
        public void EjectAllStockedItems()
        {
            var pickups = GetComponentsInChildren<ItemPickup>();
            Debug.Log($"[ShelfContainer] '{name}' ejecting {pickups.Length} stocked item(s).");
            foreach (ItemPickup pickup in pickups)
            {
                Debug.Log($"[ShelfContainer]   Ejecting '{pickup.name}' ({pickup.ItemInstance?.Definition?.DisplayName}).");
                pickup.transform.SetParent(null);

                if (pickup.TryGetComponent(out Rigidbody rb))
                {
                    rb.isKinematic = false;
                    rb.useGravity = true;
                }

                foreach (Collider col in pickup.GetComponentsInChildren<Collider>())
                    col.enabled = true;
            }
        }

        /// <summary>Clears item data from every child <see cref="Shelf"/>.</summary>
        public void ClearAllShelves()
        {
            Debug.Log($"[ShelfContainer] '{name}' clearing item data from {Shelves.Count} child Shelf(s).");
            foreach (var shelf in Shelves)
                shelf.ClearAllItems();
        }
#endregion
    }
}
