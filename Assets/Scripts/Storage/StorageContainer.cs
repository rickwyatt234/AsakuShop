using UnityEngine;
using System.Collections.Generic;
using AsakuShop.Items;
using AsakuShop.Core;

namespace AsakuShop.Storage
{
    public class StorageContainer : MonoBehaviour, IInteractable, IHoldable, IStorageUnit
    {
        [SerializeField] private string containerID = "Container001";
        public string DisplayName = "Container";

        [SerializeField] private PreferredStorageType preferredStorageType = PreferredStorageType.Dry;
        public PreferredStorageType PreferredStorageType => preferredStorageType;

        private StorageInventory inventory;
        [SerializeField] private Vector2 inventorySize = new Vector2(500, 400);
        public Vector2 InventorySize => inventorySize;
        [SerializeField] private float maxWeightCapacity = 50f;
        public float MaxWeightCapacity => maxWeightCapacity;

        [SerializeField] private Vector3 heldOffset = new Vector3(0, -0.5f, 1f);
        [SerializeField] private Quaternion heldRotation = Quaternion.Euler(0, 180, 0);

        // IHoldable implementation
        string IHoldable.DisplayName => DisplayName;
        Vector3 IHoldable.HeldOffset => heldOffset;
        Quaternion IHoldable.HeldRotation => heldRotation;
        GameObject IHoldable.GameObject => gameObject;

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
#region Unity Lifecycle
        private void Awake()
        {
            inventory = new StorageInventory(inventorySize);
        }
#endregion


/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
#region IInteractable Implementation
        public void OnInteract()
        {
            IPickupTarget pickupTarget = PlayerService.PickupTarget;
            if (pickupTarget != null)
            {
                if (!pickupTarget.IsHoldingInteractable)
                    pickupTarget.TryPickupInteractable(gameObject);
                else
                    Debug.Log("[STORAGE CONTAINER] Player is holding something, cannot pickup container");
            }
            else
            {
                Debug.LogWarning("[StorageContainer] No IPickupTarget registered in PlayerService.");
            }
        }

        public void OnExamine()
        {
            if (!InventoryState.IsOpen)
                OpenInventory();
        }
#endregion


/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
#region Public Methods
        public void OpenInventory()
        {
            CoreEvents.RaiseInventoryOpenRequested(this);
        }

        public bool TryAddItem(ItemInstance item) => inventory.TryAddItem(item);
        public StorageInventory Inventory => inventory;
        public float GetCurrentWeight() => inventory.GetCurrentWeight();
        public bool CanAddItem(ItemInstance item) => inventory.CanAddItem(item);
        public List<ItemInstance> GetAllItems() => inventory.GetAllItems();
        public int GetCapacity() => inventory.GetCapacity();
        public int GetCurrentCount() => inventory.GetCurrentCount();
        public bool TryRemoveItem(ItemInstance item) => inventory.TryRemoveItem(item);

        /// <summary>
        /// Implements <see cref="IStorageUnit.ClearAllItems"/>.
        /// Delegates to <see cref="StorageInventory.ClearAllItems"/>.
        /// </summary>
        public void ClearAllItems() => inventory.ClearAllItems();
#endregion


    }
}
