using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using AsakuShop.Items;
using AsakuShop.Input;
using AsakuShop.Store;
using AsakuShop.Core;
using AsakuShop.Storage;
using AsakuShop.UI;

namespace AsakuShop.Player
{
    //Player Interactivity with world. Handles raycasting, item pickup, and item placement.
    //Current held item instance. One item can be held at a time, and is visible in the player's hands.
    public class PlayerHands : MonoBehaviour
    {
#region Fields
        public Transform playerCamera;
        public ItemInstance heldItem;
        private bool previousInteractState = false;
        private bool previousExamineState = false;
        private Vector3 previewRotation = Vector3.zero; // Track current rotation

            [HideInInspector] public IInputManager input;
            [HideInInspector] public GameObject heldItemVisual;
            [HideInInspector] public Transform heldItemVisualTransform;
            [HideInInspector] public Vector3 placementPreviewPosition;
            [HideInInspector] public GameObject placementPreviewVisual;
            [HideInInspector] public bool validPlacement;
#endregion


#region Unity methods
        private void Awake()
        {
            playerCamera = Camera.main.transform;
            input = GetComponent<IInputManager>();
        }
        private void Update()
        {
            HandleInteraction();
            HandleExaminationOrStorage();
            HandlePlacementPreview();
            HandlePreviewRotation();
        }

        private void OnEnable()
        {
            CoreEvents.OnPhaseChanged += HandlePhaseChanged;
        }

        private void OnDisable()
        {
            CoreEvents.OnPhaseChanged -= HandlePhaseChanged;
        }
#endregion


#region Interaction
        private void HandleInteraction()
        {
            if (GameStateController.Instance.CurrentPhase != GamePhase.Playing)
                return;

            // Don't allow interaction if inventory is open
            if (InventoryState.IsOpen)
                return;

            bool currentInteractState = input.interact;
            bool interactPressed = currentInteractState && !previousInteractState;
            previousInteractState = currentInteractState;

        if (interactPressed)
        {
            bool holdingItem = heldItem != null;
            bool holdingContainer = heldItemVisual != null && heldItemVisual.GetComponent<StorageContainer>() != null;
            bool holdingShelf = heldItemVisual != null && heldItemVisual.GetComponent<ShelfComponent>() != null;

            if (!holdingItem && !holdingContainer && !holdingShelf)
            {
                TryPickupItem(); 
            }
            else if (holdingItem)
            {
                // Try to stock item on shelf first
                Ray ray = new Ray(playerCamera.position, playerCamera.forward);
                if (Physics.Raycast(ray, out RaycastHit hit, 3f))
                {
                    ShelfComponent shelf = hit.collider.GetComponent<ShelfComponent>();
                    if (shelf != null && shelf.CanAddItem(heldItem))
                    {
                        TryStockItemOnShelf(shelf);
                        return;
                    }
                }
                
                // Otherwise place the item
                TryPlaceItem();
            }
            else
            {
                TryPlaceItem();
            }
        }

            UpdateContextHint();
        }

        private void UpdateContextHint()
        {
            if (InventoryState.IsOpen)
            {
                ContextHintDisplay.Instance?.HideContext();
                return;
            }

            Ray ray = new Ray(playerCamera.position, playerCamera.forward);
            string contextHint = "";
            bool hitSomething = false;
            bool foundStorageUnit = false;

            string interactKey = input.GetInteractKeyName();
            string examineKey = input.GetItemExamineKeyName();
            
            // Get rotation key names
            string rotateKey = input.GetRotatePreviewKeyName();
            string rotateModifierKey = input.GetRotatePreviewModifierKeyName();
            
            // Build rotation hint based on device type
            string rotationHint;
            if (input.IsGamepadActive)
            {
                rotationHint = $"[{rotateModifierKey} + {rotateKey}] Rotate Horizontally\n[{rotateKey}] Rotate Vertically";
            }
            else
            {
                rotationHint = $"[{rotateKey}] Rotate Vertically\n[{rotateModifierKey} + {rotateKey}] Rotate Horizontally";
            }

            // When holding item and not looking at anything, show rotation
            if (heldItem != null || heldItemVisual != null)
            {
                contextHint = $"[{interactKey}] Place\n{rotationHint}";
            }

            if (Physics.Raycast(ray, out RaycastHit hit, 3f))
            {
                StorageContainer container = hit.collider.GetComponent<StorageContainer>();
                if (container != null)
                {
                    foundStorageUnit = true;
                    hitSomething = true;
                    ItemHoverDisplay.Instance?.ShowLabelForContainer(container.containerName);
                    
                    if (heldItem != null)
                        contextHint = $"[{interactKey}] Drop\n[{examineKey}] Store Item";
                    else if (heldItemVisual != null)
                        contextHint = $"[{interactKey}] Place\n{rotationHint}";
                    else
                        contextHint = $"[{interactKey}] Pick Up\n[{examineKey}] Open";
                }
                else
                {
                    ShelfComponent shelf = hit.collider.GetComponent<ShelfComponent>();
                    if (shelf != null)
                    {
                        foundStorageUnit = true;
                        hitSomething = true;
                        ItemHoverDisplay.Instance?.ShowLabelForContainer(shelf.shelfName);
                        
                        bool isWallMounted = IsShelfWallMounted(shelf);
                        
                        if (heldItem != null)
                        {
                            // Check if we can stock this item on the shelf
                            if (shelf.CanAddItem(heldItem))
                                contextHint = $"[{interactKey}] Stock";
                            else
                                contextHint = $"[{interactKey}] Drop"; // Shelf can't accept this item
                        }
                        else if (heldItemVisual != null)
                            contextHint = $"[{interactKey}] Place\n{rotationHint}";
                        else if (isWallMounted)
                            contextHint = $"[{examineKey}] Pick Up";
                        else
                            contextHint = $"[{interactKey}] Pick Up";
                    }
                    else
                    {
                        ItemPickup pickup = hit.collider.GetComponent<ItemPickup>();
                        if (pickup != null && pickup.itemInstance != null)
                        {
                            hitSomething = true;
                            
                            if (heldItem != null)
                                contextHint = $"[{interactKey}] Drop\n[{examineKey}] Examine Held";
                            else if (heldItemVisual != null)
                                contextHint = $"[{interactKey}] Place\n{rotationHint}";
                            else
                                contextHint = $"[{interactKey}] Pick Up";
                        }
                    }
                }
            }

            // If we didn't find a storage unit, clear the storage label
            if (!foundStorageUnit)
            {
                ItemHoverDisplay.Instance?.ResetDisplay();
            }

            if (!hitSomething && heldItem != null)
                contextHint = $"[{interactKey}] Place\n{rotationHint}\n[{examineKey}] Examine Held";

            if (ContextHintDisplay.Instance != null)
            {
                if (string.IsNullOrEmpty(contextHint))
                    ContextHintDisplay.Instance.HideContext();
                else
                    ContextHintDisplay.Instance.SetContext(contextHint);
            }
        }

        private void HandleExaminationOrStorage()
        {
            if (GameStateController.Instance.CurrentPhase != GamePhase.Playing)
                return;
            if (StorageInventoryUI.IsInventoryOpen)
                return;

            bool currentExamineState = input.itemExamine;
            bool examinePressed = currentExamineState && !previousExamineState;
            previousExamineState = currentExamineState;

            if (examinePressed)
            {
                Ray ray = new Ray(playerCamera.position, playerCamera.forward);
                
                RaycastHit[] hits = Physics.RaycastAll(ray, 3f, -1, QueryTriggerInteraction.Collide);
                
                foreach (RaycastHit hit in hits)
                {
                    if (hit.collider.CompareTag("Player") || hit.transform.root.gameObject == gameObject)
                        continue;

                    // Check for wall-mounted shelf first
                    ShelfComponent wallShelf = hit.collider.GetComponent<ShelfComponent>();
                    if (wallShelf != null && IsShelfWallMounted(wallShelf))
                    {
                        PickupShelf(wallShelf);
                        return;
                    }

                    StorageContainer container = hit.collider.GetComponent<StorageContainer>();
                    if (container != null)
                    {
                        // If holding an item, try to store it
                        if (heldItem != null)
                        {
                            Debug.Log($"Tried to store {heldItem.Definition.DisplayName} in {container.name}");
                            TryStoreItem(container);
                            return;
                        }
                        
                        // Otherwise, open the container inventory
                        container.OpenInventory();
                        return;
                    }
                }

                // Then check for held item examination
                if (heldItem != null)
                {
                    ItemExaminer.Instance.StartExamination(heldItem);
                }
            }
        }

        private void HandlePhaseChanged(GamePhase previousPhase, GamePhase newPhase)
        {
            // When entering ItemExamination, hide the held item visual and preview item visual if they exist
            if (newPhase == GamePhase.ItemExamination)
            {
                if (heldItemVisual != null)
                    heldItemVisual.SetActive(false);

                if (placementPreviewVisual != null)                
                {
                    Destroy(placementPreviewVisual);
                    placementPreviewVisual = null;
                }
            }


            // When leaving ItemExamination back to Playing, show the held item visual
            if (previousPhase == GamePhase.ItemExamination && newPhase == GamePhase.Playing)
            {
                heldItemVisual.SetActive(true);
            }
        }
#endregion


#region Item Pickup and Placement
        private void TryPickupItem()
        {
            if (InventoryState.IsOpen)
                return;
            Ray ray = new Ray(playerCamera.position, playerCamera.forward);
            if (Physics.Raycast(ray, out RaycastHit hit, 3f))
            {
                // First, check if it's an interactable (like storage containers to open)
                if (input.itemExamine)
                {
                    IInteractable interactable = hit.collider.GetComponent<IInteractable>();
                    if (interactable != null)
                    {
                        interactable.OnInteract();
                        return;
                    }
                }

                // Try to pick up storage container
                StorageContainer storageContainer = hit.collider.GetComponent<StorageContainer>();
                if (storageContainer != null)
                {
                    PickupStorageContainer(storageContainer);
                    return;
                }

                ShelfComponent shelf = hit.collider.GetComponent<ShelfComponent>();
                if (shelf != null)
                {
                    // Can only pick up ground/air shelves with interact
                    if (!IsShelfWallMounted(shelf))
                    {
                        PickupShelf(shelf);
                        return;
                    }
                }

                // Try to pick up normal item
                ItemPickup pickup = hit.collider.GetComponent<ItemPickup>();
                if (pickup != null && pickup.itemInstance != null)
                {
                    heldItem = pickup.itemInstance;
                    
                    // Check if this item is on a shelf and remove it from the shelf
                    ShelfComponent[] allShelves = FindObjectsByType<ShelfComponent>(FindObjectsSortMode.None);
                    foreach (ShelfComponent shelfComponent in allShelves)
                    {
                        if (shelfComponent.GetAllItems().Contains(pickup.itemInstance))
                        {
                            shelfComponent.TryRemoveItem(pickup.itemInstance);
                            break;
                        }
                    }
                    
                    Destroy(pickup.gameObject);
                    InstantiateHeldItemVisual();
                }
                else
                {
                    Debug.Log("No ItemPickup or StorageContainer found on hit object.");
                }
            }
        }

        private void PickupStorageContainer(StorageContainer container)
        {
            if (InventoryState.IsOpen)
                return;

            heldItemVisual = container.gameObject;
            heldItemVisualTransform = heldItemVisual.transform;
            heldItemVisualTransform.SetParent(playerCamera);
            heldItemVisualTransform.localPosition = new Vector3(1f, -1f, 1f);
            heldItemVisualTransform.localRotation = Quaternion.Euler(0, 90f, 0);
            
            // Disable physics while holding (use kinematic instead of disabling collider)
            Rigidbody rb = container.GetComponent<Rigidbody>();
            if (rb != null)
                rb.isKinematic = true;
            
            foreach (Collider col in container.GetComponentsInChildren<Collider>())
            {
                col.enabled = false; // Disable colliders on held container to prevent unintended interactions
            }
            Debug.Log($"Picked up storage container: {container.name}");
        }

        private void PickupShelf(ShelfComponent shelf)
        {
            if (InventoryState.IsOpen)
                return;

            // Parent all stocked items to the shelf so they move with it
            List<ItemInstance> stockedItems = shelf.GetAllItems();
            if (stockedItems.Count > 0)
            {
                ItemPickup[] allPickups = FindObjectsByType<ItemPickup>(FindObjectsSortMode.None);
                
                foreach (ItemInstance item in stockedItems)
                {
                    foreach (ItemPickup pickup in allPickups)
                    {
                        if (pickup.itemInstance == item)
                        {
                            pickup.transform.SetParent(shelf.transform);
                            break;
                        }
                    }
                }
            }

            heldItemVisual = shelf.gameObject;
            heldItemVisualTransform = heldItemVisual.transform;
            heldItemVisualTransform.SetParent(playerCamera);
            heldItemVisualTransform.localPosition = new Vector3(1f, -1f, 1f);
            heldItemVisualTransform.localRotation = Quaternion.Euler(0, 90f, 0);
            
            Rigidbody rb = shelf.GetComponent<Rigidbody>();
            if (rb != null)
                rb.isKinematic = true;
            
            foreach (Collider col in shelf.GetComponentsInChildren<Collider>())
            {
                col.enabled = false;
            }
            
            Debug.Log($"Picked up shelf: {shelf.name}");
        }
        private void TryPlaceItem()
        {
            if (InventoryState.IsOpen)
                return;

            if(validPlacement)
            {
                // Check if holding a shelf
                ShelfComponent heldShelf = heldItemVisual?.GetComponent<ShelfComponent>();
                if (heldShelf != null)
                {
                    heldItemVisualTransform.SetParent(null);
                    heldItemVisualTransform.position = placementPreviewPosition;
                    
                    // Check if we're mounting on a wall
                    Ray wallRay = new Ray(playerCamera.position, playerCamera.forward);
                    bool isWallMount = Physics.Raycast(wallRay, out RaycastHit hit, 5f) && 
                        hit.collider.gameObject.layer == LayerMask.NameToLayer("Wall");
                    
                    if (isWallMount)
                    {
                        // Wall mounting - use LookRotation
                        Quaternion wallRotation = Quaternion.LookRotation(hit.normal, Vector3.up);
                        wallRotation *= Quaternion.Euler(heldShelf.RotationOffset);
                        heldItemVisualTransform.rotation = wallRotation;
                    }
                    else
                    {
                        // Ground placement - previewRotation
                        heldItemVisualTransform.rotation = Quaternion.Euler(previewRotation);
                    }
                    
                    // Unparent stocked items - collect them first to avoid iteration issues
                    List<Transform> itemsToUnparent = new List<Transform>();
                    foreach (Transform child in heldItemVisualTransform)
                    {
                        ItemPickup itemPickup = child.GetComponent<ItemPickup>();
                        if (itemPickup != null)
                        {
                            itemsToUnparent.Add(child);
                        }
                    }

                    foreach (Transform item in itemsToUnparent)
                    {
                        item.SetParent(null);
                        
                        // If placing on ground (not wall), enable physics on the stocked items so they fall
                        if (!isWallMount)
                        {
                            Rigidbody itemRigidbody = item.GetComponent<Rigidbody>();
                            if (itemRigidbody != null)
                            {
                                itemRigidbody.isKinematic = false;
                                itemRigidbody.useGravity = true;
                            }
                            
                            // Re-enable colliders so items can collide with floor
                            foreach (Collider col in item.GetComponentsInChildren<Collider>())
                            {
                                col.enabled = true;
                            }
                        }
                    }
                    // Re-enable colliders
                    foreach (Collider col in heldShelf.GetComponentsInChildren<Collider>())
                    {
                        col.enabled = true;
                    }
                    
                    // Setup Rigidbody
                    Rigidbody rb = heldShelf.GetComponent<Rigidbody>();
                    if (rb != null)
                    {
                        rb.isKinematic = isWallMount; // Kinematic on wall, dynamic on ground
                        if (!isWallMount)
                            rb.useGravity = true;
                    }
                    
                    if (placementPreviewVisual != null)
                    {
                        Destroy(placementPreviewVisual);
                        placementPreviewVisual = null;
                    }
                    
                    heldItemVisual = null;
                    heldItemVisualTransform = null;
                    previewRotation = Vector3.zero;
                    return;
                }

                // Check if holding a storage container
                StorageContainer heldContainer = heldItemVisual?.GetComponent<StorageContainer>();
                if (heldContainer != null)
                {
                    heldItemVisualTransform.SetParent(null);
                    heldItemVisualTransform.position = placementPreviewPosition;
                    heldItemVisualTransform.rotation = Quaternion.Euler(previewRotation);
                    
                    // Re-enable colliders
                    foreach (Collider col in heldContainer.GetComponentsInChildren<Collider>())
                    {
                        col.enabled = true;
                    }
                    
                    // Setup Rigidbody
                    Rigidbody rb = heldContainer.GetComponent<Rigidbody>();
                    if (rb != null)
                    {
                        rb.isKinematic = false;
                        rb.useGravity = true;
                    }
                    
                    if (placementPreviewVisual != null)
                    {
                        Destroy(placementPreviewVisual);
                        placementPreviewVisual = null;
                    }
                    
                    heldItemVisual = null;
                    heldItemVisualTransform = null;
                    previewRotation = Vector3.zero;
                    return;
                }

                // Normal item placement with gravity
                ItemInstance itemToPlace = heldItem;
                heldItem = null;

                GameObject placedItem = Instantiate(itemToPlace.Definition.WorldPrefab, placementPreviewPosition, Quaternion.Euler(previewRotation));
                ItemPickup pickup = placedItem.AddComponent<ItemPickup>();
                pickup.itemInstance = itemToPlace;

                Rigidbody itemRb = placedItem.GetComponent<Rigidbody>();
                if (itemRb == null)
                    itemRb = placedItem.AddComponent<Rigidbody>();
                itemRb.isKinematic = false;
                itemRb.useGravity = true;

                MeshCollider collider2 = placedItem.AddComponent<MeshCollider>();
                collider2.convex = true;

                if (heldItemVisual != null)
                {
                    Destroy(heldItemVisual);
                    heldItemVisual = null;
                }
                
                if (placementPreviewVisual != null)
                {
                    Destroy(placementPreviewVisual);
                    placementPreviewVisual = null;            
                }

                previewRotation = Vector3.zero;
            }
        }

        private void TryStoreItem(StorageContainer container)
        {
            if (heldItem == null || container == null)
                return;
            
            // Try to add item to container
            if (container.TryAddItem(heldItem))
            {
                Debug.Log($"Stored {heldItem.Definition.DisplayName} in {container.name}");

                // FIX: Destroy visuals IMMEDIATELY and completely
                if (heldItemVisual != null)
                {
                    heldItemVisual.SetActive(false);
                    Destroy(heldItemVisual);
                    heldItemVisual = null;
                    heldItemVisualTransform = null;
                }
                
                // FIX: MUST destroy placement preview or it will persist
                if (placementPreviewVisual != null)
                {
                    placementPreviewVisual.SetActive(false);
                    Destroy(placementPreviewVisual);
                    placementPreviewVisual = null;
                }

                // Clear the held item
                heldItem = null;
                previewRotation = Vector3.zero;

                // Refresh UI with the container AFTER cleanup
                if (StorageInventoryUI.Instance != null)
                {
                    StorageInventoryUI.Instance.RefreshUI(container);
                }
            }
            else
            {
                Debug.Log($"Cannot store {heldItem.Definition.DisplayName} in {container.name} - wrong storage type");
                if (placementPreviewVisual != null)
                {
                    Destroy(placementPreviewVisual);
                    placementPreviewVisual = null;
                }
            }
        }

        private void TryStockItemOnShelf(ShelfComponent shelf)
        {
            if (heldItem == null || shelf == null)
                return;
            
            if (shelf.TryAddItem(heldItem))
            {
                Debug.Log($"Stocked {heldItem.Definition.DisplayName} on {shelf.name}");
                
                ItemInstance itemToStock = heldItem;
                Vector3 slotWorldPosition = shelf.GetSlotPosition(itemToStock);
                
                // Get rotation from shelf, or use identity if not available
                Quaternion stockRotation = Quaternion.Euler(shelf.GetStockingRotation());
                
                // Instantiate with proper rotation
                GameObject stockedItemVisual = Instantiate(itemToStock.Definition.WorldPrefab, slotWorldPosition + shelf.GetStockingOffset(), stockRotation);

                // Set the Item layer so the hover display can detect it
                stockedItemVisual.layer = LayerMask.NameToLayer("Item");
                foreach (Transform child in stockedItemVisual.GetComponentsInChildren<Transform>())
                {
                    child.gameObject.layer = LayerMask.NameToLayer("Item");
                }

                ItemPickup pickup = stockedItemVisual.AddComponent<ItemPickup>();
                pickup.itemInstance = itemToStock;

                // Ensure collider exists for raycast picking
                if (stockedItemVisual.GetComponent<Collider>() == null)
                {
                    MeshCollider col = stockedItemVisual.AddComponent<MeshCollider>();
                    col.convex = true;
                }

                Rigidbody rb = stockedItemVisual.GetComponent<Rigidbody>();
                if (rb != null)
                    rb.isKinematic = true;
                else
                {
                    rb = stockedItemVisual.AddComponent<Rigidbody>();
                    rb.isKinematic = true;
                }
                
                if (heldItemVisual != null)
                {
                    Destroy(heldItemVisual);
                    heldItemVisual = null;
                    heldItemVisualTransform = null;
                }
                
                if (placementPreviewVisual != null)
                {
                    Destroy(placementPreviewVisual);
                    placementPreviewVisual = null;
                }
                
                heldItem = null;
                previewRotation = Vector3.zero;
            }
            else
            {
                Debug.Log($"Cannot stock {heldItem.Definition.DisplayName} on {shelf.name}");
            }
        }
#endregion


#region Preview Visuals
        private void InstantiateHeldItemVisual()
        {
            if (heldItemVisual != null)
                Destroy(heldItemVisual);

            // Instantiate a visual representation of the held item in the player's hands
            heldItemVisual = Instantiate(heldItem.Definition.WorldPrefab);
            heldItemVisualTransform = heldItemVisual.transform;
            heldItemVisualTransform.SetParent(playerCamera);
            heldItemVisualTransform.localPosition = new Vector3(0.5f, -0.5f, 1f); // Example position in front of camera
            heldItemVisualTransform.localRotation = Quaternion.Euler(0, 90f, 0); // Rotate to face player
            Rigidbody rb = heldItemVisual.GetComponent<Rigidbody>();
            if (rb != null)
            {
                Destroy(rb); // Remove rigidbody from held item visual to prevent physics interactions while held
            }

            foreach (Collider col in heldItemVisual.GetComponentsInChildren<Collider>())
            {
                col.enabled = false; // Disable colliders on held item visual to prevent unintended interactions
            }
        }

        private void HandlePlacementPreview()
        {
            bool holdingItem = heldItem != null;
            bool holdingContainer = heldItemVisual != null && heldItemVisual.GetComponent<StorageContainer>() != null;
            bool holdingShelf = heldItemVisual != null && heldItemVisual.GetComponent<ShelfComponent>() != null;
            
            if (!holdingItem && !holdingContainer && !holdingShelf) 
                return;

            if (GameStateController.Instance.CurrentPhase != GamePhase.Playing)
                return;

            Vector3 previewPos = playerCamera.position + playerCamera.forward * 2f;
            validPlacement = true;

            // Create/update preview for held shelves
            if (holdingShelf && heldItemVisual != null)
            {
                if (placementPreviewVisual == null)
                {
                    placementPreviewVisual = Instantiate(heldItemVisual);
                    
                    foreach (Collider col in placementPreviewVisual.GetComponentsInChildren<Collider>())
                        col.enabled = false;
                    
                    Rigidbody rb = placementPreviewVisual.GetComponent<Rigidbody>();
                    if (rb != null) Destroy(rb);
                    
                    ShelfComponent shelfComponent = placementPreviewVisual.GetComponent<ShelfComponent>();
                    if (shelfComponent != null) Destroy(shelfComponent);
                    
                    foreach (Renderer renderer in placementPreviewVisual.GetComponentsInChildren<Renderer>())
                    {
                        foreach (Material mat in renderer.materials)
                        {
                            Color color = mat.color;
                            color.r *= 0.2f;
                            color.g *= 0.2f;
                            color.b *= 0.2f;
                            color.a = 0.5f;
                            mat.color = color;
                        }
                    }
                }

                // Raycast forward to find wall or ground
                Ray forwardRay = new Ray(playerCamera.position, playerCamera.forward);
                validPlacement = false;
                previewPos = playerCamera.position + playerCamera.forward * 2f; // Default position

                if (Physics.Raycast(forwardRay, out RaycastHit hit, 5f))
                {
                    // Try wall mounting first
                    if (hit.collider.gameObject.layer == LayerMask.NameToLayer("Wall"))
                    {
                        validPlacement = true;
                        
                        ShelfComponent shelf = heldItemVisual.GetComponent<ShelfComponent>();
                        float offset = (shelf != null) ? shelf.mountOffsetDistance : 0.5f;
                        previewPos = hit.point + hit.normal * offset;
                        
                        // Use LookRotation to avoid Z-rotation ambiguity
                        Quaternion wallRotation = Quaternion.LookRotation(hit.normal, Vector3.up);
                        if (shelf != null)
                        {
                            wallRotation *= Quaternion.Euler(shelf.RotationOffset);
                        }
                        
                        placementPreviewVisual.transform.position = previewPos;
                        placementPreviewVisual.transform.rotation = wallRotation;
                        placementPreviewPosition = previewPos;
                        return;
                    }
                    // Try ground placement as fallback
                    else if (hit.collider.gameObject.layer == LayerMask.NameToLayer("Ground"))
                    {
                        validPlacement = true;
                        previewPos = hit.point;
                    }
                }

                // Update preview even if no valid surface (allows placement in air)
                validPlacement = true;
                placementPreviewVisual.transform.position = previewPos;
                placementPreviewVisual.transform.rotation = Quaternion.Euler(previewRotation);
                placementPreviewPosition = previewPos;

                return;
            }

            // Create/update preview for items
            if (holdingItem && heldItem != null)
            {
                if (placementPreviewVisual == null)
                {
                    placementPreviewVisual = Instantiate(heldItem.Definition.WorldPrefab);
                    placementPreviewVisual.transform.position = previewPos;
                    
                    foreach (Renderer renderer in placementPreviewVisual.GetComponentsInChildren<Renderer>())
                    {
                        foreach (Material mat in renderer.materials)
                        {
                            Color color = mat.color;
                            color.r *= 0.2f;
                            color.g *= 0.2f;
                            color.b *= 0.2f;
                            color.a = 0.5f;
                            mat.color = color;
                        }
                    }

                    Rigidbody rb = placementPreviewVisual.GetComponent<Rigidbody>();
                    if (rb != null) Destroy(rb);
                }
                placementPreviewPosition = previewPos;
            }
            // Create/update preview for containers
            else if (holdingContainer && heldItemVisual != null)
            {
                if (placementPreviewVisual == null)
                {
                    placementPreviewVisual = Instantiate(heldItemVisual);
                    placementPreviewVisual.transform.position = previewPos;
                    
                    foreach (Collider col in placementPreviewVisual.GetComponentsInChildren<Collider>())
                        col.enabled = false;
                    
                    Rigidbody rb = placementPreviewVisual.GetComponent<Rigidbody>();
                    if (rb != null) Destroy(rb);
                    
                    StorageInteraction storageInteraction = placementPreviewVisual.GetComponent<StorageInteraction>();
                    if (storageInteraction != null) Destroy(storageInteraction);
                    
                    foreach (Renderer renderer in placementPreviewVisual.GetComponentsInChildren<Renderer>())
                    {
                        foreach (Material mat in renderer.materials)
                        {
                            Color color = mat.color;
                            color.r *= 0.2f;
                            color.g *= 0.2f;
                            color.b *= 0.2f;
                            color.a = 0.5f;
                            mat.color = color;
                        }
                    }
                    placementPreviewVisual.transform.rotation = Quaternion.Euler(previewRotation);
                }
                placementPreviewPosition = previewPos;
            }
        }

        private void HandlePreviewRotation()
        {
            bool holdingItem = heldItem != null;
            bool holdingContainer = heldItemVisual != null && heldItemVisual.GetComponent<StorageContainer>() != null;
            bool holdingShelf = heldItemVisual != null && heldItemVisual.GetComponent<ShelfComponent>() != null;
            
            if (!holdingItem && !holdingContainer && !holdingShelf)
            {
                previewRotation = Vector3.zero;
                return;
            }

            // Don't allow rotation for shelves snapped to walls
            if (holdingShelf)
            {
                Ray wallCheckRay = new Ray(playerCamera.position, playerCamera.forward);
                if (Physics.Raycast(wallCheckRay, out RaycastHit hit, 5f) && 
                    hit.collider.gameObject.layer == LayerMask.NameToLayer("Wall"))
                {
                    previewRotation = Vector3.zero; // Reset rotation, don't apply it
                    return;
                }
            }

            float scrollInput = Mouse.current.scroll.ReadValue().y / 20f;
            bool isShiftPressed = input.rotatePreviewModifier;

            if (scrollInput != 0)
            {
                if (isShiftPressed)
                    previewRotation.y += scrollInput * 90f;
                else
                    previewRotation.x += scrollInput * 90f;
            }

            float gamepadHorizontal = input.rotatePreviewHorizontal;
            float gamepadVertical = input.rotatePreviewVertical;

            previewRotation.x += gamepadVertical * 90f * Time.deltaTime;
            previewRotation.y += gamepadHorizontal * 90f * Time.deltaTime;

            previewRotation.x = previewRotation.x % 360f;
            previewRotation.y = previewRotation.y % 360f;

            if (placementPreviewVisual != null)
            {
                float previewDistance = 2f;
                Vector3 previewPos = playerCamera.position + playerCamera.forward * Mathf.Min(previewDistance, 2.5f);
                
                // Raycast forward from camera to avoid intersecting objects
                Ray forwardRay = new Ray(playerCamera.position, playerCamera.forward);
                
                if (Physics.Raycast(forwardRay, out RaycastHit hit, 10f))
                {
                    if (hit.collider.gameObject != placementPreviewVisual && 
                        hit.collider.gameObject != heldItemVisual)
                    {
                        previewDistance = Mathf.Clamp(hit.distance - 0.3f, 0.5f, 2.5f);
                    }
                }
                
                previewPos = playerCamera.position + playerCamera.forward * previewDistance;
                
                // Raycast downward to find floor and keep preview above it
                Ray downRay = new Ray(previewPos, Vector3.down);
                float floorHeight = 0f;
                
                if (Physics.Raycast(downRay, out RaycastHit hitDown, 100f))
                {
                    if (hitDown.collider.gameObject.layer == LayerMask.NameToLayer("Ground"))
                    {
                        floorHeight = hitDown.point.y;
                    }
                }
                
                // Clamp Y position to stay above floor
                previewPos.y = Mathf.Max(previewPos.y, floorHeight + 0.5f);
                
                placementPreviewVisual.transform.position = previewPos;
                placementPreviewVisual.transform.rotation = Quaternion.Euler(previewRotation);
                placementPreviewPosition = previewPos;
            }
        }

        private bool IsShelfWallMounted(ShelfComponent shelf)
        {
            if (shelf == null) return false;
            
            Rigidbody rb = shelf.GetComponent<Rigidbody>();
            if (rb == null || !rb.isKinematic) return false;
            
            // Check in multiple directions for a wall (backward is most likely after LookRotation placement)
            Vector3[] directions = {
                -shelf.transform.forward,  // Backwards (toward wall when placed)
                shelf.transform.forward,   // Forward
                shelf.transform.right,     // Right
                -shelf.transform.right,    // Left
                shelf.transform.up,        // Up
                -shelf.transform.up        // Down
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
#endregion
        public void TryScannItemAtTerminal()
        {
            if (heldItem == null)
            {
                Debug.Log("No item being held to scan");
                return;
            }

            Ray ray = new Ray(playerCamera.position, playerCamera.forward);
            if (Physics.Raycast(ray, out RaycastHit hit, 3f))
            {
                CheckoutTerminal terminal = hit.collider.GetComponent<CheckoutTerminal>();
                if (terminal != null)
                {
                    if (terminal.TryScanItem(heldItem))
                    {
                        // Item scanned successfully, destroy the visual
                        if (heldItemVisual != null)
                        {
                            Destroy(heldItemVisual);
                            heldItemVisual = null;
                        }

                        // Clear held item
                        heldItem = null;
                        previewRotation = Vector3.zero;
                    }
                }
            }
        }

#region Debugging
        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(0, 40, 400, 20));
            
            string heldName = "None";
            if (heldItem != null)
            {
                heldName = heldItem.Definition.DisplayName;
            }
            else if (heldItemVisual != null)
            {
                StorageContainer container = heldItemVisual.GetComponent<StorageContainer>();
                if (container != null)
                {
                    heldName = container.name;
                }
                else
                {
                    ShelfComponent shelf = heldItemVisual.GetComponent<ShelfComponent>();
                    if (shelf != null)
                        heldName = shelf.name;
                }
            }
            
            GUILayout.Label($"Held Item: {heldName}");
            GUILayout.EndArea();
        }
#endregion
    }
}