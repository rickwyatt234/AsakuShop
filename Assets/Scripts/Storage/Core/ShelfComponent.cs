using UnityEngine;
using AsakuShop.Items;
using AsakuShop.UI;
using System.Collections.Generic;
using AsakuShop.Core;
using AsakuShop.Player;

namespace AsakuShop.Storage
{
    public class ShelfComponent : MonoBehaviour, IInteractable
    {
        // Unique identifier for this shelf (could be used for saving/loading)
        [SerializeField] private string shelfID = "Shelf001";
        public string shelfName = "Shelf";


        // Held item position and rotation configuration
        public Vector3 heldOffset = new Vector3(0, 0.5f, 0);
        public Quaternion heldRotation = Quaternion.Euler(90, 0, 0);


        // Preferred Storage Type for food spoilage purposes (e.g. Dry, Refrigerated, Frozen)
        [SerializeField] private PreferredStorageType storageType = PreferredStorageType.Dry;
        public PreferredStorageType StorageType => storageType;


        // Stocking constraints
        public StockingSize[] allowedStockingSizes = new StockingSize[] 
                               { StockingSize.Small, StockingSize.Medium, StockingSize.Large };


        // Slot configuration
        public int slotColumns = 4;
        public int slotRows = 3;
        public float slotSpacingX = 0.3f;
        public float slotSpacingY = 0.3f;
        [SerializeField] private Vector3 stockingRotation = new Vector3(90, 0, 0);
        [SerializeField] private Vector3 stockingOffset = new Vector3(0, 0.5f, 0);
        public Vector3 GetStockingRotation() => stockingRotation;
        public Vector3 GetStockingOffset() => stockingOffset;


        // Mounting and browsing configuration
        public float mountOffsetDistance = 0.5f; // How far from the wall the shelf should be when mounted
        public float browsingDistance = 2f; // Max distance for NPCs to interact with shelf
        [SerializeField] private Vector3 slotStartOffset = new Vector3(-0.9f, 0.9f, 0);
        [SerializeField] private Vector3 rotationOffset = Vector3.zero;
        public Vector3 RotationOffset => rotationOffset;


        //Items currently on the shelf and their slot assignments
        private List<ItemInstance> items = new();
        public List<ItemInstance> Items => items;
        public Dictionary<ItemInstance, int> itemToSlotIndex = new();

        //References
        private PlayerHands playerHands;
        private ShelfInteraction shelfInteraction;


/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
#region Unity Lifecycle
        private void Awake()
        {
            playerHands = FindFirstObjectByType<PlayerHands>();
            shelfInteraction = GetComponent<ShelfInteraction>();
        }
#endregion

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
#region IInteractable Implementation
        public void OnInteract()
        {
            // If shelf is wall-mounted, the player is holding something already, or 
            // the inventory UI is already open, do not allow interaction
            if (InventoryState.IsOpen || IsShelfWallMounted(this) || playerHands.IsHoldingInteractable)
                return;

            else
            {
                playerHands.heldShelf = this;
                playerHands.TryPickupInteractable(playerHands.heldShelf.gameObject);
            }
        }
        public void OnExamine()
        {
            if (InventoryState.IsOpen || !IsShelfWallMounted(this))
                return;

            //Pick up shelf
            //This is to prevent the normal interaction button from picking up the shelf accidentally
            playerHands.heldShelf = this;
            playerHands.TryPickupInteractable(playerHands.heldShelf.gameObject);

            //Keep items visually on the shelf while it's being held by parenting them to the shelf object
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
                {
                    return true;
                }
            }

            return false;
        }
    }
}