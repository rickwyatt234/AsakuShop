using UnityEngine;
using AsakuShop.Items;
using AsakuShop.Input;
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

                if (!holdingItem && !holdingContainer)
                {
                    TryPickupItem(); 
                }
                else
                {
                    TryPlaceItem();
                }
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

                // Try to pick up normal item
                ItemPickup pickup = hit.collider.GetComponent<ItemPickup>();
                if (pickup != null && pickup.itemInstance != null)
                {
                    heldItem = pickup.itemInstance;
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

        private void TryPlaceItem()
        {
            if (InventoryState.IsOpen)
                return;

            if(validPlacement)
            {
                // Check if holding a storage container
                StorageContainer heldContainer = heldItemVisual?.GetComponent<StorageContainer>();
                if (heldContainer != null)
                {
                    heldItemVisualTransform.SetParent(null);
                    heldItemVisualTransform.position = placementPreviewPosition;
                    heldItemVisualTransform.rotation = Quaternion.Euler(previewRotation);
                    
                    if (placementPreviewVisual != null)
                    {
                        Destroy(placementPreviewVisual);
                        placementPreviewVisual = null;
                    }

                    Rigidbody rb = heldContainer.GetComponent<Rigidbody>();
                    if (rb != null)
                        rb.isKinematic = false;

                    foreach (Collider col in heldContainer.GetComponentsInChildren<Collider>())
                        col.enabled = true;
                    
                    heldItemVisual = null;
                    heldItemVisualTransform = null;
                    previewRotation = Vector3.zero;
                    return;
                }

                // FIX: Clear heldItem BEFORE creating the placed item (prevents duplicates)
                ItemInstance itemToPlace = heldItem;
                heldItem = null;

                // Normal item placement with gravity
                GameObject placedItem = Instantiate(itemToPlace.Definition.WorldPrefab, placementPreviewPosition, Quaternion.Euler(previewRotation));
                ItemPickup pickup = placedItem.AddComponent<ItemPickup>();
                pickup.itemInstance = itemToPlace;

                // Ensure Rigidbody exists and gravity is enabled
                Rigidbody itemRb = placedItem.GetComponent<Rigidbody>();
                if (itemRb == null)
                    itemRb = placedItem.AddComponent<Rigidbody>();
                itemRb.isKinematic = false;
                itemRb.useGravity = true;

                SphereCollider collider2 = placedItem.AddComponent<SphereCollider>();
                collider2.radius = 0.3f;

                // Destroy held item visual
                if (heldItemVisual != null)
                {
                    Destroy(heldItemVisual);
                    heldItemVisual = null;
                }
                
                // FIX: Destroy preview immediately
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
            
            if (!holdingItem && !holdingContainer) 
                return;

            if (GameStateController.Instance.CurrentPhase != GamePhase.Playing)
                return;

            Vector3 previewPos = playerCamera.position + playerCamera.forward * 2f;
            validPlacement = true;

            // Create/update preview for items
            if (holdingItem && heldItem != null)
            {
                if (placementPreviewVisual == null)
                {
                    placementPreviewVisual = Instantiate(heldItem.Definition.WorldPrefab);
                    placementPreviewVisual.transform.position = previewPos; // SET POSITION FIRST
                    
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

                    // Calculate bounds at correct position
                    Bounds bounds = new Bounds(placementPreviewVisual.transform.position, Vector3.zero);
                    foreach (Renderer renderer in placementPreviewVisual.GetComponentsInChildren<Renderer>())
                        bounds.Encapsulate(renderer.bounds);

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
                    placementPreviewVisual.transform.position = previewPos; // SET POSITION FIRST
                    
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

                    // Calculate bounds at correct position
                    Bounds bounds = new Bounds(placementPreviewVisual.transform.position, Vector3.zero);
                    foreach (Renderer renderer in placementPreviewVisual.GetComponentsInChildren<Renderer>())
                        bounds.Encapsulate(renderer.bounds);
                }
                placementPreviewPosition = previewPos;
            }
        }

    private void HandlePreviewRotation()
    {
        bool holdingItem = heldItem != null;
        bool holdingContainer = heldItemVisual != null && heldItemVisual.GetComponent<StorageContainer>() != null;
        
        if (!holdingItem && !holdingContainer)
        {
            previewRotation = Vector3.zero;
            return;
        }

        float scrollInput = UnityEngine.Input.GetAxis("Mouse ScrollWheel");
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
                // Don't collide with the preview itself or held item
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
#endregion

#region Debugging
        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(0, 40, 400, 20));
            
            string heldName = "None";
            if (heldItem != null)
                heldName = heldItem.Definition.DisplayName;
            else if (heldItemVisual != null)
            {
                StorageContainer container = heldItemVisual.GetComponent<StorageContainer>();
                if (container != null)
                    heldName = container.name; // Or "Storage Container", etc.
            }
            
            GUILayout.Label($"Held Item: {heldName}");
            GUILayout.EndArea();
        }
#endregion
    }
}