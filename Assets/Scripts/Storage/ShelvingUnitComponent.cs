using UnityEngine;
using System.Collections.Generic;
using AsakuShop.Core;
using AsakuShop.Items;

namespace AsakuShop.Storage
{
    public class ShelvingUnitComponent : MonoBehaviour, IInteractable, IMountableHoldable, IShelfHoldable, IShelfItemProvider
    {
        [SerializeField] private string displayName = "Shelving Unit";
        [SerializeField] private Vector3 heldOffset = new Vector3(0, 0.5f, 0);
        [SerializeField] private Quaternion heldRotation = Quaternion.Euler(0, 0, 0);
        [SerializeField] private Vector3 rotationOffset = Vector3.zero;
        [SerializeField] private float mountOffsetDistance = 0.5f;

        [SerializeField] private MountMode mountMode = MountMode.Floor;
        [SerializeField] private MountSurfaceMask allowedSurfaces = MountSurfaceMask.Ground;
        [SerializeField] private bool alignToSurfaceNormal = false;
        [SerializeField] private bool allowManualYawRotation = true;

        private List<ShelvingUnitSurface> surfaces = new();
        private bool isMounted = false;

        public string DisplayName => displayName;
        public Vector3 HeldOffset => heldOffset;
        public Quaternion HeldRotation => heldRotation;
        public GameObject GameObject => gameObject;
        public Vector3 RotationOffset => rotationOffset;
        public float MountOffsetDistance => mountOffsetDistance;

        /// <summary>True once the unit has been placed on the floor; cleared when picked back up.</summary>
        public bool IsMounted => isMounted;

        MountMode IMountableHoldable.MountMode => mountMode;
        MountSurfaceMask IMountableHoldable.AllowedSurfaces => allowedSurfaces;
        bool IMountableHoldable.AlignToSurfaceNormal => alignToSurfaceNormal;
        bool IMountableHoldable.AllowManualYawRotation => allowManualYawRotation;
        void IMountableHoldable.NotifyMounted() => NotifyMounted();
        void IMountableHoldable.NotifyPickedUp() => NotifyPickedUp();

        private void Awake()
        {
            CacheSurfaces();
        }

        private void OnValidate()
        {
            CacheSurfaces();
        }

        private void CacheSurfaces()
        {
            surfaces.Clear();
            surfaces.AddRange(GetComponentsInChildren<ShelvingUnitSurface>());
        }

        public List<ShelvingUnitSurface> GetSurfaces() => surfaces;

        public virtual void NotifyMounted() { isMounted = true; }
        public virtual void NotifyPickedUp() { isMounted = false; }

        /// <summary>
        /// Unparents every child ItemPickup and restores physics on it so items fall when
        /// the unit is picked back up — mirrors ShelfComponent.EjectAllStockedItems().
        /// </summary>
        public void EjectAllStockedItems()
        {
            foreach (ItemPickup pickup in GetComponentsInChildren<ItemPickup>())
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

        public void OnInteract()
        {
            var pickupTarget = PlayerService.PickupTarget;
            if (pickupTarget == null) return;
            // A mounted (placed) shelving unit must be retrieved via Examine, not Interact,
            // matching the same convention used by wall-mounted ShelfComponent.
            if (InventoryState.IsOpen || isMounted || pickupTarget.IsHoldingInteractable)
                return;
            pickupTarget.TryPickupInteractable(gameObject);
        }

        public void OnExamine()
        {
            var pickupTarget = PlayerService.PickupTarget;
            if (pickupTarget == null) return;
            if (InventoryState.IsOpen || pickupTarget.IsHoldingInteractable)
                return;
            pickupTarget.TryPickupInteractable(gameObject);
        }

        // IShelfItemProvider
        public ItemInstance PeekItem()
        {
            foreach (var surface in surfaces)
            {
                var item = surface.PeekItem();
                if (item != null) return item;
            }
            return null;
        }

        public ShelfTakeResult TakeItem()
        {
            foreach (var surface in surfaces)
            {
                if (surface.GetCurrentCount() > 0)
                {
                    var item = surface.PeekItem();
                    if (item != null)
                    {
                        surface.TryRemoveItem(item);
                        return new ShelfTakeResult(item, null); // Optionally find ItemPickup if needed
                    }
                }
            }
            return default;
        }
    }
}