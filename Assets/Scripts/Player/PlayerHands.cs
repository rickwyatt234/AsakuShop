using UnityEngine;
using AsakuShop.Items;
using AsakuShop.Input;

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
            HandlePlacementPreview();
        }
#endregion


#region Interaction
        private void HandleInteraction()
        {
            bool currentInteractState = input.interact;
            bool interactPressed = currentInteractState && !previousInteractState;
            previousInteractState = currentInteractState;

            if (interactPressed)
            {
                if (heldItem == null)
                {
                    TryPickupItem();
                }
                else
                {
                    TryPlaceItem();
                }
            }
        }
#endregion


#region Item Pickup and Placement
        private void TryPickupItem()
        {
            //Raycast from center of screen to find item to pick up
            Ray ray = new Ray(playerCamera.position, playerCamera.forward);
            if (Physics.Raycast(ray, out RaycastHit hit, 3f, LayerMask.GetMask("Item")))
            {
                //Debug.Log($"Hit object: {hit.collider.name}");
                ItemPickup pickup = hit.collider.GetComponent<ItemPickup>();
                if (pickup != null && pickup.itemInstance != null)
                {
                    heldItem = pickup.itemInstance;
                    Destroy(pickup.gameObject); // Remove item from world
                    InstantiateHeldItemVisual();
                }
                else 
                {
                    Debug.Log("No ItemPickup component found on hit object.");
                }
            }
        }

        private void TryPlaceItem()
        {
            // Implement item placement logic here
            if(validPlacement)
            {
                // Place the item in the world at the preview location
                GameObject placedItem = Instantiate(heldItem.Definition.WorldPrefab, placementPreviewPosition, Quaternion.identity);
                ItemPickup pickup = placedItem.AddComponent<ItemPickup>();
                pickup.itemInstance = heldItem;

                SphereCollider collider = placedItem.AddComponent<SphereCollider>();
                collider.radius = 0.3f;

                        // Clean up visuals
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
            if (heldItem == null) return;

            //Raycast from center of screen to find valid placement location. If item
            //can be placed on a valid ground layer, show a transparent preview of the item at that location.
            Ray ray = new Ray(playerCamera.position, playerCamera.forward);
            if (Physics.Raycast(ray, out RaycastHit hit, 3f))
            {
                // Check if hit surface is valid for placement (e.g., has "Ground" layer)
                if (hit.collider.gameObject.layer == LayerMask.NameToLayer("Ground"))
                {
                    validPlacement = true;
                    // Show placement preview at hit.point
                    if (placementPreviewVisual == null)
                    {
                        placementPreviewVisual = Instantiate(heldItem.Definition.WorldPrefab);
                        // Make the preview semi-transparent or otherwise indicate it's a preview
                        foreach (Renderer renderer in placementPreviewVisual.GetComponentsInChildren<Renderer>())
                        {
                            foreach (Material mat in renderer.materials)
                            {
                                Color color = mat.color;
                                color.r *= 0.2f;  // Darken red
                                color.g *= 0.2f;  // Darken green
                                color.b *= 0.2f;  // Darken blue
                                mat.color = color;
                            }
                        }
                    }
                    placementPreviewVisual.transform.position = hit.point + Vector3.up * 0.45f; // Slightly above ground to prevent z-fighting
                    placementPreviewPosition = hit.point + Vector3.up * 0.45f;
                }
                else
                {
                    validPlacement = false;
                    if (placementPreviewVisual != null)
                    {
                        Destroy(placementPreviewVisual);
                        placementPreviewVisual = null;
                    }
                    // Hide or indicate invalid placement
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
                // Hide or indicate invalid placement
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