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
            HandleExamination();
            HandlePlacementPreview();
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

        private void HandleExamination()
        {
            // Allow examining storage containers OR held items
            if (GameStateController.Instance.CurrentPhase != GamePhase.Playing)
                return;

            bool currentExamineState = input.itemExamine;
            bool examinePressed = currentExamineState && !previousExamineState;
            previousExamineState = currentExamineState;

            if (examinePressed)
            {
                Ray ray = new Ray(playerCamera.position, playerCamera.forward);
                
                // Check for interactable first (storage containers) - allow trigger hits
                if (Physics.Raycast(ray, out RaycastHit hit, 3f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide))
                {
                    IInteractable interactable = hit.collider.GetComponent<IInteractable>();
                    if (interactable != null)
                    {
                        interactable.OnInteract();
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
            Ray ray = new Ray(playerCamera.position, playerCamera.forward);
            if (Physics.Raycast(ray, out RaycastHit hit, 3f))
            {
                // First, check if it's an interactable (like storage containers)
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
            heldItemVisual = container.gameObject;
            heldItemVisualTransform = heldItemVisual.transform;
            heldItemVisualTransform.SetParent(playerCamera);
            heldItemVisualTransform.localPosition = new Vector3(0.5f, -0.5f, 1f);
            heldItemVisualTransform.localRotation = Quaternion.identity;
            
            // Disable the container's collider while held
            Collider collider = container.GetComponent<Collider>();
            if (collider != null)
                collider.enabled = false;
            
            Debug.Log($"Picked up storage container: {container.name}");
        }

        private void TryPlaceItem()
        {
            if(validPlacement)
            {
                // Check if holding a storage container
                StorageContainer heldContainer = heldItemVisual?.GetComponent<StorageContainer>();
                if (heldContainer != null)
                {
                    // Place container back in world
                    heldItemVisualTransform.SetParent(null);
                    heldItemVisualTransform.position = placementPreviewPosition;
                    
                    // Re-enable collider
                    Collider collider = heldContainer.GetComponent<Collider>();
                    if (collider != null)
                        collider.enabled = true;
                    
                    heldItemVisual = null;
                    heldItemVisualTransform = null;
                    return;
                }

                // Normal item placement logic
                GameObject placedItem = Instantiate(heldItem.Definition.WorldPrefab, placementPreviewPosition, Quaternion.identity);
                ItemPickup pickup = placedItem.AddComponent<ItemPickup>();
                pickup.itemInstance = heldItem;

                SphereCollider collider2 = placedItem.AddComponent<SphereCollider>();
                collider2.radius = 0.3f;

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

                heldItem = null;
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
            heldItemVisualTransform.localRotation = Quaternion.identity;
            Rigidbody rb = heldItemVisual.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.useGravity = false; // Disable gravity on the held item
                rb.isKinematic = true; // Disable physics interactions on the held item
            }
        }

        private void HandlePlacementPreview()
        {
            // Check if holding either an item or a container
            bool holdingItem = heldItem != null;
            bool holdingContainer = heldItemVisual != null && heldItemVisual.GetComponent<StorageContainer>() != null;
            
            if (!holdingItem && !holdingContainer) 
                return;

            // Don't show placement preview during examination or other non-playing phases
            if (GameStateController.Instance.CurrentPhase != GamePhase.Playing)
                return;

            //Raycast from center of screen to find valid placement location
            Ray ray = new Ray(playerCamera.position, playerCamera.forward);
            if (Physics.Raycast(ray, out RaycastHit hit, 3f))
            {
                // Check if hit surface is valid for placement (e.g., has "Ground" layer)
                if (hit.collider.gameObject.layer == LayerMask.NameToLayer("Ground"))
                {
                    validPlacement = true;
                    
                    // Only create/update preview for items, not containers
                    if (holdingItem && heldItem != null)
                    {
                        if (placementPreviewVisual == null)
                        {
                            placementPreviewVisual = Instantiate(heldItem.Definition.WorldPrefab);
                            foreach (Renderer renderer in placementPreviewVisual.GetComponentsInChildren<Renderer>())
                            {
                                foreach (Material mat in renderer.materials)
                                {
                                    Color color = mat.color;
                                    color.r *= 0.2f;
                                    color.g *= 0.2f;
                                    color.b *= 0.2f;
                                    mat.color = color;
                                }
                            }
                        }
                        placementPreviewVisual.transform.position = hit.point + Vector3.up * 0.45f;
                        placementPreviewPosition = hit.point + Vector3.up * 0.45f;
                    }
                    else if (holdingContainer)
                    {
                        // For containers, just update the position without a preview visual
                        placementPreviewPosition = hit.point + Vector3.up * 0.45f;
                    }
                }
                else
                {
                    validPlacement = false;
                    if (placementPreviewVisual != null)
                    {
                        Destroy(placementPreviewVisual);
                        placementPreviewVisual = null;
                    }
                }
            }
            else
            {
                validPlacement = false;
                if (placementPreviewVisual != null)
                {
                    Destroy(placementPreviewVisual);
                    placementPreviewVisual = null;
                }
            }
        }
#endregion

#region Debugging
        //Display currently held item
        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(0, 40, 200, 20));
            string heldItemName = heldItem != null ? heldItem.Definition.DisplayName : "None";
            GUILayout.Label($"Held Item: {heldItemName}");
            GUILayout.EndArea();
        }
#endregion
    }
}