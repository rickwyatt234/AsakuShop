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
            if (pickupTarget == null) return;
            if (InventoryState.IsOpen || !IsShelfWallMounted(this))
                return;

            pickupTarget.TryPickupInteractable(gameObject);

            List<ItemInstance> stockedItems = shelfInteraction.GetAllItems();
            if (stockedItems.Count > 0)
            {
                foreach (var item in stockedItems)
                {
                    if (item != null && item.Definition.WorldPrefab != null)
                    {
                        GameObject itemVisual = Instantiate(item.Definition.WorldPrefab, transform);
                        itemVisual.transform.localPosition = shelfInteraction.GetSlotPosition(item);
                        itemVisual.transform.localRotation = Quaternion.Euler(GetStockingRotation());
                    }
                }
            }
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
    }
}
