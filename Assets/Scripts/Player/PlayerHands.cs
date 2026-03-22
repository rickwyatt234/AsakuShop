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
    public class PlayerHands : MonoBehaviour
    {
#region Fields
        public Transform playerCamera;

        // These fields track what the player is currently holding in their hands. Only one of these should be non-null at a time.
        // Or none at all if the player is empty-handed. These are set when picking up items/shelves/containers and cleared when placing them.
        // SET BY ITEMINSTANCE.CS, SHELFCOMPONENT.CS, STORAGECONTAINER.CS IN ONINTERACT() METHODS
        public ItemInstance heldItem;
        public StorageContainer heldContainer;
        public ShelfComponent heldShelf;
        public bool IsHoldingInteractable => heldItem != null || heldContainer != null || heldShelf != null;


        private bool previousInteractState = false;
        private bool previousExamineState = false;
        private Vector3 previewRotation = Vector3.zero;

        [HideInInspector] public IInputManager input;
        [HideInInspector] public GameObject heldItemVisual;
        [HideInInspector] public Transform heldItemVisualTransform;
        [HideInInspector] public Vector3 placementPreviewPosition;
        [HideInInspector] public GameObject placementPreviewVisual;
        [HideInInspector] public bool validPlacement;   
#endregion

#region Unity Methods
        private void Awake()
        {
            playerCamera = Camera.main.transform;
            input = GetComponent<IInputManager>();
        }

        private void Update()
        {
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
        private void HandleExaminationOrStorage()
        {
            if (GameStateController.Instance.CurrentPhase != GamePhase.Playing)
                return;
            if (StorageInventoryUI.IsInventoryOpen)
                return;

            bool currentExamineState = input.examine;
            bool examinePressed = currentExamineState && !previousExamineState;
            previousExamineState = currentExamineState;

            if (heldItem != null)
            {
                ItemExaminer.Instance.StartExamination(heldItem);
            }
            // Maybe examine logic for containers and shelves later
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
        public void TryPickupInteractable(GameObject interactableObject)
        {
            if (InventoryState.IsOpen)
                return;
            if (GameStateController.Instance.CurrentPhase != GamePhase.Playing)
                return;

            if (heldItem != null)
            {
                
            }
            else if (heldContainer != null)
            {
                //Held Item Visual
                heldItemVisual = heldContainer.gameObject;
                heldItemVisualTransform = heldItemVisual.transform;
                heldItemVisualTransform.SetParent(playerCamera);
                heldItemVisualTransform.localPosition = heldContainer.heldOffset;
                heldItemVisualTransform.localRotation = heldContainer.heldRotation;

                //Component and collider adjustments
                Rigidbody rb = heldContainer.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.isKinematic = true;
                }
                foreach (Collider col in heldContainer.GetComponentsInChildren<Collider>())                
                {
                    col.enabled = false;
                }
                Debug.Log($"Picked up storage container: {heldContainer.containerName}");
            }
            else if (heldShelf != null)
            {
                //Held Item Visual
                heldItemVisual = heldShelf.gameObject;
                heldItemVisualTransform = heldItemVisual.transform;
                heldItemVisualTransform.SetParent(playerCamera);
                heldItemVisualTransform.localPosition = heldShelf.heldOffset;
                heldItemVisualTransform.localRotation = heldShelf.heldRotation;
            }
            else
            {
                Debug.Log("[PLAYER] Cannot pickup - no interactable object provided");
            }


        }


                    // Pick up the actual item GameObject (like we do with shelves/containers)

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

                    UnityEngine.Debug.Log($"[PLAYER] Picked up counter item: {pickup.itemInstance.Definition.DisplayName}");
                }
            }
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
                UnityEngine.Debug.Log($"Stored {heldItem.Definition.DisplayName} in {container.name}");

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
                UnityEngine.Debug.Log($"Cannot store {heldItem.Definition.DisplayName} in {container.name} - wrong storage type");
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
                UnityEngine.Debug.Log($"Stocked {heldItem.Definition.DisplayName} on {shelf.name}");

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
                UnityEngine.Debug.Log($"Cannot stock {heldItem.Definition.DisplayName} on {shelf.name}");
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
            UnityEngine.Debug.Log($"[CHECKOUT UnityEngine.Debug] SnapToCheckoutPosition called");
            CharacterController controller = GetComponent<CharacterController>();
            if (controller != null) controller.enabled = false;
            transform.position = position.position;
            Quaternion newRotation = position.rotation * Quaternion.Euler(0, 180f, 0);
            transform.rotation = newRotation;
            if (controller != null) controller.enabled = true;
            UnityEngine.Debug.Log("[CHECKOUT UnityEngine.Debug] ✓ Snap complete!");
        }

        public void LockMovement(bool locked)
        {
            movementLocked = locked;
            if (locked)
            {
                UnityEngine.Debug.Log("[CHECKOUT UnityEngine.Debug] Movement locked");
            }
            else
            {
                UnityEngine.Debug.Log("[CHECKOUT UnityEngine.Debug] Movement unlocked");
            }
        }

        private void TryScanItemAtTerminal(CheckoutTerminal terminal)
        {
            if (terminal == null)
                return;

            if (heldItem == null)
            {
                UnityEngine.Debug.Log("[CHECKOUT] No item to scan");
                return;
            }

            if (terminal.TryScanItem(heldItem))
            {
                UnityEngine.Debug.Log("[CHECKOUT] Item scanned!");

                if (heldItemVisual != null)
                {
                    Destroy(heldItemVisual);
                    heldItemVisual = null;
                }
                heldItem = null;
            }
            else
            {
                UnityEngine.Debug.Log("[CHECKOUT] Failed to scan item");
            }
        }

        private void TryStartCheckout()
        {
            UnityEngine.Debug.Log("[CHECKOUT UnityEngine.Debug] TryStartCheckout called");
            Ray ray = new Ray(playerCamera.position, playerCamera.forward);

            if (Physics.Raycast(ray, out RaycastHit hit, 3f))
            {
                UnityEngine.Debug.Log($"[CHECKOUT UnityEngine.Debug] Raycast hit: {hit.collider.gameObject.name}");
                CheckoutZone zone = hit.collider.GetComponent<CheckoutZone>();

                if (zone == null)
                {
                    UnityEngine.Debug.Log("[CHECKOUT UnityEngine.Debug] No CheckoutZone in raycast hit");
                }

                if (zone != null)
                {
                    UnityEngine.Debug.Log("[CHECKOUT UnityEngine.Debug] Found CheckoutZone");

                    if (!zone.IsPlayerCheckingOut())
                    {
                        UnityEngine.Debug.Log("[CHECKOUT UnityEngine.Debug] Starting checkout...");
                        zone.TryStartCheckout(this);
                        activeCheckoutZone = zone;
                    }
                    else
                    {
                        UnityEngine.Debug.Log("[CHECKOUT UnityEngine.Debug] Already checking out");
                    }
                }
                else
                {
                    UnityEngine.Debug.Log("[CHECKOUT UnityEngine.Debug] No zone found");
                }
            }
            else
            {
                UnityEngine.Debug.Log("[CHECKOUT UnityEngine.Debug] Raycast hit nothing");
            }
        }
#endregion

#region UnityEngine.Debugging
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