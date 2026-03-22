using UnityEngine;
using AsakuShop.Items;
using AsakuShop.Core;
using AsakuShop.Player;
using AsakuShop.UI;

namespace AsakuShop.Storage
{
    public class StorageContainer : MonoBehaviour, IInteractable
    {
        // Unique identifier for this container (could be used for saving/loading)
        [SerializeField] private string containerID = "Container001";
        public string containerName = "Container";


        // Preferred Storage Type for food spoilage purposes (e.g. Dry, Refrigerated, Frozen)
        [SerializeField] private PreferredStorageType storageType = PreferredStorageType.Dry;
        public PreferredStorageType StorageType => storageType;


        // The actual inventory data for this container
        private StorageInventory inventory;
        [SerializeField] private Vector2 inventorySize = new Vector2(500, 400); // UI window size
        public Vector2 InventorySize => inventorySize;
        [SerializeField] private float maxWeightCapacity = 50f; // max weight in kg
        public float MaxWeightCapacity => maxWeightCapacity;


        // Offset and rotation for when the container is held by the player
        public Vector3 heldOffset = new Vector3(0, -0.5f, 1f);
        public Quaternion heldRotation = Quaternion.Euler(0, 180, 0);


/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
#region Unity Lifecycle
        private void Awake()
        {
            // Initialize inventory with specified size
            inventory = new StorageInventory(inventorySize);
            playerHands = FindFirstObjectByType<PlayerHands>();
        }
#endregion


/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
#region IInteractable Implementation

// Reference to PlayerHands for pickup logic
private PlayerHands playerHands;

        public void OnInteract()
        {
            if (playerHands != null)
            {
              if (!playerHands.IsHoldingInteractable)
                {
                  //pick up container
                    playerHands.heldContainer = this;
                    playerHands.TryPickupInteractable(playerHands.heldContainer.gameObject);  
                }
              else
                {
                    Debug.Log("[STORAGE CONTAINER] Player is holding something, cannot pickup container");
                }
            }
            else
            {
                Debug.LogWarning("PlayerHands reference is missing in StorageContainer");
            }
        }
        public void OnExamine()
        {
            OpenInventory();
        }
#endregion


/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
#region Public Methods for Storage Container Functionality
        // Open the inventory UI for this container
        public void OpenInventory()
        {
            if (StorageInventoryUI.Instance != null)
                StorageInventoryUI.Instance.OpenContainer(this);
        }
    }
#endregion
}   