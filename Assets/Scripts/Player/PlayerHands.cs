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
    public class PlayerHands : MonoBehaviour, ICheckoutPlayer
    {
#region Fields
        public Transform playerCamera;
        public ItemInstance heldItem;
        private bool previousInteractState = false;
        private bool previousExamineState = false;
        private Vector3 previewRotation = Vector3.zero;

        [HideInInspector] public IInputManager input;
        [HideInInspector] public GameObject heldItemVisual;
        [HideInInspector] public Transform heldItemVisualTransform;
        [HideInInspector] public Vector3 placementPreviewPosition;
        [HideInInspector] public GameObject placementPreviewVisual;
        [HideInInspector] public bool validPlacement;

        private Transform checkoutPositionTarget;
        private bool movementLocked = false;
        private CheckoutZone activeCheckoutZone;
#endregion

#region Unity Methods
        private void Awake()
        {
            playerCamera = Camera.main.transform;
            input = GetComponent<IInputManager>();
            Debug.Log("[PLAYER] PlayerHands initialized");
        }

        private void Update()
        {
            if (UnityEngine.Input.GetKeyDown(KeyCode.Escape) && activeCheckoutZone != null && activeCheckoutZone.IsPlayerCheckingOut())
            {
                Debug.Log("[CHECKOUT DEBUG] ESC pressed, ending checkout");
                activeCheckoutZone.TryEndCheckout();
                activeCheckoutZone = null;
            }

            if (movementLocked)
            {
                input.DisableMovementInput();
            }

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

                // Try to start checkout if not already checking out
                if (activeCheckoutZone == null || !activeCheckoutZone.IsPlayerCheckingOut())
                {
                    Debug.Log("[CHECKOUT DEBUG] Trying to start checkout...");
                    TryStartCheckout();
                }

                // If checking out, try to scan items
                if (activeCheckoutZone != null && activeCheckoutZone.IsPlayerCheckingOut())
                {
                    Debug.Log("[CHECKOUT DEBUG] Already at checkout, trying to scan item");
                    TryScanItemAtTerminal(activeCheckoutZone.GetTerminal());
                }

                // Normal item interactions (only if not at checkout)
                if (activeCheckoutZone == null || !activeCheckoutZone.IsPlayerCheckingOut())
                {
                    if (!holdingItem && !holdingContainer && !holdingShelf)
                    {
                        TryPickupItem();
                    }
                    else if (holdingItem)
                    {
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
                        TryPlaceItem();
                    }
                    else
                    {
                        TryPlaceItem();
                    }
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
            string rotateKey = input.GetRotatePreviewKeyName();
            string rotateModifierKey = input.GetRotatePreviewModifierKeyName();

            string rotationHint;
            if (input.IsGamepadActive)
            {
                rotationHint = $"[{rotateModifierKey} + {rotateKey}] Rotate Horizontally\n[{rotateKey}] Rotate Vertically";
            }
            else
            {
                rotationHint = $"[{rotateKey}] Rotate Vertically\n[{rotateModifierKey} + {rotateKey}] Rotate Horizontally";
            }

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
                            if (shelf.CanAddItem(heldItem))
                                contextHint = $"[{interactKey}] Stock";
                            else
                                contextHint = $"[{interactKey}] Drop";
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

                    ShelfComponent wallShelf = hit.collider.GetComponent<ShelfComponent>();
                    if (wallShelf != null && IsShelfWallMounted(wallShelf))
                    {
                        PickupShelf(wallShelf);
                        return;
                    }

                    StorageContainer container = hit.collider.GetComponent<StorageContainer>();
                    if (container != null)
                    {
                        if (heldItem != null)
                        {
                            Debug.Log($"Tried to store {heldItem.Definition.DisplayName} in {container.name}");
                            TryStoreItem(container);
                            return;
                        }

                        container.OpenInventory();
                        return;
                    }
                }

                if (heldItem != null)
                {
                    ItemExaminer.Instance.StartExamination(heldItem);
                }
            }
        }

        private void HandlePhaseChanged(GamePhase previousPhase, GamePhase newPhase)
        {
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

            if (previousPhase == GamePhase.ItemExamination && newPhase == GamePhase.Playing)
            {
                if (heldItemVisual != null)
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
                if (input.itemExamine)
                {
                    IInteractable interactable = hit.collider.GetComponent<IInteractable>();
                    if (interactable != null)
                    {
                        interactable.OnInteract();
                        return;
                    }
                }

                StorageContainer storageContainer = hit.collider.GetComponent<StorageContainer>();
                if (storageContainer != null)
                {
                    PickupStorageContainer(storageContainer);
                    return;
                }

                ShelfComponent shelf = hit.collider.GetComponent<ShelfComponent>();
                if (shelf != null)
                {
                    if (!IsShelfWallMounted(shelf))
                    {
                        PickupShelf(shelf);
                        return;
                    }
                }

                ItemPickup pickup = hit.collider.GetComponent<ItemPickup>();
                if (pickup != null && pickup.itemInstance != null)
                {
                    heldItem = pickup.itemInstance;

                    // If this item was on a shelf, remove it from the shelf
                    ShelfComponent[] allShelves = FindObjectsByType<ShelfComponent>(FindObjectsSortMode.None);
                    foreach (ShelfComponent shelfComponent in allShelves)
                    {
                        if (shelfComponent.GetAllItems().Contains(pickup.itemInstance))
                        {
                            shelfComponent.TryRemoveItem(pickup.itemInstance);
                            break;
                        }
                    }

                    // Pick up the actual item GameObject (like we do with shelves/containers)
                    heldItemVisual = pickup.gameObject;
                    heldItemVisualTransform = heldItemVisual.transform;
                    heldItemVisualTransform.SetParent(playerCamera);
                    heldItemVisualTransform.localPosition = new Vector3(0.5f, -0.5f, 1f);
                    heldItemVisualTransform.localRotation = Quaternion.Euler(0, 90f, 0);

                    // Disable physics while carrying
                    Rigidbody itemRb = heldItemVisual.GetComponent<Rigidbody>();
                    if (itemRb != null)
                    {
                        itemRb.isKinematic = true;
                    }

                    // Disable colliders while carrying
                    foreach (Collider col in heldItemVisual.GetComponentsInChildren<Collider>())
                    {
                        col.enabled = false;
                    }

                    Debug.Log($"[PLAYER] Picked up counter item: {pickup.itemInstance.Definition.DisplayName}");
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

            Rigidbody rb = container.GetComponent<Rigidbody>();
            if (rb != null)
                rb.isKinematic = true;

            foreach (Collider col in container.GetComponentsInChildren<Collider>())
            {
                col.enabled = false;
            }
            Debug.Log($"Picked up storage container: {container.name}");
        }

        private void PickupShelf(ShelfComponent shelf)
        {
            if (InventoryState.IsOpen)
                return;

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

            if (validPlacement)
            {
                ShelfComponent heldShelf = heldItemVisual?.GetComponent<ShelfComponent>();
                if (heldShelf != null)
                {
                    heldItemVisualTransform.SetParent(null);
                    heldItemVisualTransform.position = placementPreviewPosition;

                    Ray wallRay = new Ray(playerCamera.position, playerCamera.forward);
                    bool isWallMount = Physics.Raycast(wallRay, out RaycastHit hit, 5f) &&
                        hit.collider.gameObject.layer == LayerMask.NameToLayer("Wall");

                    if (isWallMount)
                    {
                        Quaternion wallRotation = Quaternion.LookRotation(hit.normal, Vector3.up);
                        wallRotation *= Quaternion.Euler(heldShelf.RotationOffset);
                        heldItemVisualTransform.rotation = wallRotation;
                    }
                    else
                    {
                        heldItemVisualTransform.rotation = Quaternion.Euler(previewRotation);
                    }

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

                        if (!isWallMount)
                        {
                            Rigidbody itemRigidbody = item.GetComponent<Rigidbody>();
                            if (itemRigidbody != null)
                            {
                                itemRigidbody.isKinematic = false;
                                itemRigidbody.useGravity = true;
                            }

                            foreach (Collider col in item.GetComponentsInChildren<Collider>())
                            {
                                col.enabled = true;
                            }
                        }
                    }

                    foreach (Collider col in heldShelf.GetComponentsInChildren<Collider>())
                    {
                        col.enabled = true;
                    }

                    Rigidbody rb = heldShelf.GetComponent<Rigidbody>();
                    if (rb != null)
                    {
                        rb.isKinematic = isWallMount;
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

                StorageContainer heldContainer = heldItemVisual?.GetComponent<StorageContainer>();
                if (heldContainer != null)
                {
                    heldItemVisualTransform.SetParent(null);
                    heldItemVisualTransform.position = placementPreviewPosition;
                    heldItemVisualTransform.rotation = Quaternion.Euler(previewRotation);

                    foreach (Collider col in heldContainer.GetComponentsInChildren<Collider>())
                    {
                        col.enabled = true;
                    }

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

            if (container.TryAddItem(heldItem))
            {
                Debug.Log($"Stored {heldItem.Definition.DisplayName} in {container.name}");

                if (heldItemVisual != null)
                {
                    heldItemVisual.SetActive(false);
                    Destroy(heldItemVisual);
                    heldItemVisual = null;
                    heldItemVisualTransform = null;
                }

                if (placementPreviewVisual != null)
                {
                    placementPreviewVisual.SetActive(false);
                    Destroy(placementPreviewVisual);
                    placementPreviewVisual = null;
                }

                heldItem = null;
                previewRotation = Vector3.zero;

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
                Quaternion stockRotation = Quaternion.Euler(shelf.GetStockingRotation());

                GameObject stockedItemVisual = Instantiate(itemToStock.Definition.WorldPrefab, slotWorldPosition + shelf.GetStockingOffset(), stockRotation);

                stockedItemVisual.layer = LayerMask.NameToLayer("Item");
                foreach (Transform child in stockedItemVisual.GetComponentsInChildren<Transform>())
                {
                    child.gameObject.layer = LayerMask.NameToLayer("Item");
                }

                ItemPickup pickup = stockedItemVisual.AddComponent<ItemPickup>();
                pickup.itemInstance = itemToStock;

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

            heldItemVisual = Instantiate(heldItem.Definition.WorldPrefab);
            heldItemVisualTransform = heldItemVisual.transform;
            heldItemVisualTransform.SetParent(playerCamera);
            heldItemVisualTransform.localPosition = new Vector3(0.5f, -0.5f, 1f);
            heldItemVisualTransform.localRotation = Quaternion.Euler(0, 90f, 0);

            Rigidbody rb = heldItemVisual.GetComponent<Rigidbody>();
            if (rb != null)
            {
                Destroy(rb);
            }

            foreach (Collider col in heldItemVisual.GetComponentsInChildren<Collider>())
            {
                col.enabled = false;
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

                Ray forwardRay = new Ray(playerCamera.position, playerCamera.forward);
                validPlacement = false;
                previewPos = playerCamera.position + playerCamera.forward * 2f;

                if (Physics.Raycast(forwardRay, out RaycastHit hit, 5f))
                {
                    if (hit.collider.gameObject.layer == LayerMask.NameToLayer("Wall"))
                    {
                        validPlacement = true;

                        ShelfComponent shelf = heldItemVisual.GetComponent<ShelfComponent>();
                        float offset = (shelf != null) ? shelf.mountOffsetDistance : 0.5f;
                        previewPos = hit.point + hit.normal * offset;

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
                    else if (hit.collider.gameObject.layer == LayerMask.NameToLayer("Ground"))
                    {
                        validPlacement = true;
                        previewPos = hit.point;
                    }
                }

                validPlacement = true;
                placementPreviewVisual.transform.position = previewPos;
                placementPreviewVisual.transform.rotation = Quaternion.Euler(previewRotation);
                placementPreviewPosition = previewPos;

                return;
            }

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

            if (holdingShelf)
            {
                Ray wallCheckRay = new Ray(playerCamera.position, playerCamera.forward);
                if (Physics.Raycast(wallCheckRay, out RaycastHit hit, 5f) &&
                    hit.collider.gameObject.layer == LayerMask.NameToLayer("Wall"))
                {
                    previewRotation = Vector3.zero;
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

                Ray downRay = new Ray(previewPos, Vector3.down);
                float floorHeight = 0f;

                if (Physics.Raycast(downRay, out RaycastHit hitDown, 100f))
                {
                    if (hitDown.collider.gameObject.layer == LayerMask.NameToLayer("Ground"))
                    {
                        floorHeight = hitDown.point.y;
                    }
                }

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
#endregion

#region Checkout System
        public void SnapToCheckoutPosition(Transform position)
        {
            Debug.Log($"[CHECKOUT DEBUG] SnapToCheckoutPosition called");
            CharacterController controller = GetComponent<CharacterController>();
            if (controller != null) controller.enabled = false;
            transform.position = position.position;
            Quaternion newRotation = position.rotation * Quaternion.Euler(0, 180f, 0);
            transform.rotation = newRotation;
            if (controller != null) controller.enabled = true;
            Debug.Log("[CHECKOUT DEBUG] ✓ Snap complete!");
        }

        public void LockMovement(bool locked)
        {
            movementLocked = locked;
            if (locked)
            {
                Debug.Log("[CHECKOUT DEBUG] Movement locked");
            }
            else
            {
                Debug.Log("[CHECKOUT DEBUG] Movement unlocked");
            }
        }

        private void TryScanItemAtTerminal(CheckoutTerminal terminal)
        {
            if (terminal == null)
                return;

            if (heldItem == null)
            {
                Debug.Log("[CHECKOUT] No item to scan");
                return;
            }

            if (terminal.TryScanItem(heldItem))
            {
                Debug.Log("[CHECKOUT] Item scanned!");

                if (heldItemVisual != null)
                {
                    Destroy(heldItemVisual);
                    heldItemVisual = null;
                }
                heldItem = null;
            }
            else
            {
                Debug.Log("[CHECKOUT] Failed to scan item");
            }
        }

        private void TryStartCheckout()
        {
            Debug.Log("[CHECKOUT DEBUG] TryStartCheckout called");
            Ray ray = new Ray(playerCamera.position, playerCamera.forward);

            if (Physics.Raycast(ray, out RaycastHit hit, 3f))
            {
                Debug.Log($"[CHECKOUT DEBUG] Raycast hit: {hit.collider.gameObject.name}");
                CheckoutZone zone = hit.collider.GetComponent<CheckoutZone>();

                if (zone == null)
                {
                    Debug.Log("[CHECKOUT DEBUG] No CheckoutZone in raycast hit");
                }

                if (zone != null)
                {
                    Debug.Log("[CHECKOUT DEBUG] Found CheckoutZone");

                    if (!zone.IsPlayerCheckingOut())
                    {
                        Debug.Log("[CHECKOUT DEBUG] Starting checkout...");
                        zone.TryStartCheckout(this);
                        activeCheckoutZone = zone;
                    }
                    else
                    {
                        Debug.Log("[CHECKOUT DEBUG] Already checking out");
                    }
                }
                else
                {
                    Debug.Log("[CHECKOUT DEBUG] No zone found");
                }
            }
            else
            {
                Debug.Log("[CHECKOUT DEBUG] Raycast hit nothing");
            }
        }
#endregion

#region Debugging
        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(10, 10, 300, 200));
            if (heldItem != null)
            {
                GUILayout.Label($"Holding: {heldItem.Definition.DisplayName}");
            }
            else if (heldItemVisual != null)
            {
                StorageContainer container = heldItemVisual.GetComponent<StorageContainer>();
                if (container != null)
                {
                    GUILayout.Label($"Holding: {container.gameObject.name}");
                }
                else
                {
                    ShelfComponent shelf = heldItemVisual.GetComponent<ShelfComponent>();
                    if (shelf != null)
                        GUILayout.Label($"Holding: {shelf.gameObject.name}");
                }
            }
            GUILayout.EndArea();
        }
#endregion
    }
}