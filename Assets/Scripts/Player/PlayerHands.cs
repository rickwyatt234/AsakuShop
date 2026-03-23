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
    public class PlayerHands : MonoBehaviour, IPickupTarget
    {

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
#region Fields
        public Transform playerCamera;

        // These fields track what the player is currently holding in their hands. Only one of these should be non-null at a time.
        // Or none at all if the player is empty-handed. These are set when picking up items/shelves/containers and cleared when placing them.
        // SET BY ITEMINSTANCE.CS, SHELFCOMPONENT.CS, STORAGECONTAINER.CS IN ONINTERACT() METHODS
        public ItemInstance heldItem; //Data
        public ItemPickup heldItemPickup; //Physical entity with ItemPickup component
        public StorageContainer heldContainer;
        public ShelfComponent heldShelf;
        public bool IsHoldingInteractable => heldItem != null || heldContainer != null || heldShelf != null;

        private Vector3 previewRotation = Vector3.zero;

        [HideInInspector] public IInputManager input;
        [HideInInspector] public GameObject heldItemVisual;
        [HideInInspector] public Transform heldItemVisualTransform;
        [HideInInspector] public Vector3 placementPreviewPosition;
        [HideInInspector] public GameObject placementPreviewVisual;
        [HideInInspector] public bool validPlacement;   
#endregion


/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
#region Unity Methods
        private void Awake()
        {
            playerCamera = Camera.main.transform;
            input = GetComponent<IInputManager>();
            PlayerService.PickupTarget = this;
        }

        private void Update()
        {
            HandlePlacementPreview();
            HandlePreviewRotation();
            HandleHeldItemInput();
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


/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
#region Interaction    
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


/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
#region Item Pickup
        public void TryPickupInteractable(GameObject interactableObject)
        {
            if (InventoryState.IsOpen)
                return;
            if (GameStateController.Instance.CurrentPhase != GamePhase.Playing)
                return;
                
            ItemPickup itemPickup       = interactableObject.GetComponent<ItemPickup>();
            StorageContainer container  = interactableObject.GetComponent<StorageContainer>();
            ShelfComponent shelf        = interactableObject.GetComponent<ShelfComponent>();
            
            if (itemPickup != null)
            {
                heldItem = itemPickup.ItemInstance;
                heldItemPickup = itemPickup;
                
                InstantiateHeldItemVisual();
                Destroy(interactableObject);
                Debug.Log($"Picked up item: {heldItem.Definition.DisplayName}");
            }

            else if (cContainer != null)
            {
                heldItemVisual = container.gameObject;
                heldItemVisualTransform = heldItemVisual.transform;
                heldItemVisualTransform.SetParent(playerCamera);
                heldItemVisualTransform.localPosition = container.heldOffset;
                heldItemVisualTransform.localRotation = container.heldRotation;

                //Component and collider adjustments
                Rigidbody rb = container.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.isKinematic = true;
                }
                foreach (Collider col in container.GetComponentsInChildren<Collider>())                
                {
                    col.enabled = false;
                }
                Debug.Log($"Picked up storage container: {container.DisplayName}");
            }
            else if (shelf != null)
            {
                heldShelf = shelf;
                
                heldItemVisual = shelf.gameObject;
                heldItemVisualTransform = heldItemVisual.transform;
                heldItemVisualTransform.SetParent(playerCamera);
                heldItemVisualTransform.localPosition = shelf.heldOffset;
                heldItemVisualTransform.localRotation = shelf.heldRotation;

                //Component and collider adjustments
                Rigidbody rb = shelf.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.isKinematic = true;
                }
                foreach (Collider col in shelf.GetComponentsInChildren<Collider>())                
                {
                    col.enabled = false;
                }
                Debug.Log($"Picked up: {heldShelf.DisplayName}");
            }
            else
            {
                Debug.Log("[PLAYER] Not looking at anything");
            }


        }
#endregion


/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
#region Placement

        private void HandleHeldItemInput()
        {
            if (!IsHoldingInteractable)
                return;
            if (GameStateController.Instance.CurrentPhase != GamePhase.Playing)
                return;
            if (InventoryState.IsOpen)
                return;
        
            // Drop / Place / Stock(if looking at shelf)
            if (input.interact)
            {
                // Raycast to see what the player is looking at
                Ray ray = new Ray(playerCamera.position, playerCamera.forward);
                if (Physics.Raycast(ray, out RaycastHit hit, 3f))
                {
                    ShelfComponent shelf = hit.collider.GetComponent<ShelfComponent>();
                    if (shelf != null && heldItem != null)
                    {
                        TryStockItemOnShelf(shelf);
                        return;
                    }
                }
                
                if (validPlacement)
                    TryPlaceInteractable();
                else
                    DropInteractable();
                return;
            }
        
            // Stock item on shelf or store in container (examine key while holding)
            if (input.examine)
            {
                // Raycast to see what the player is looking at
                Ray ray = new Ray(playerCamera.position, playerCamera.forward);
                if (Physics.Raycast(ray, out RaycastHit hit, 3f))
                {
                    StorageContainer container = hit.collider.GetComponent<StorageContainer>();
                    if (container != null && heldItem != null)
                    {
                        TryStoreItem(container);
                        return;
                    }
                }
            }
        }


        private void TryPlaceInteractable()
        {
            if (InventoryState.IsOpen)
                return;
            if (!validPlacement)
                return;
                
            heldItemVisualTransform.SetParent(null);
            heldItemVisualTransform.position = placementPreviewPosition;


            if (heldItem != null)
            {
                ItemInstance itemToPlace = heldItem;
                heldItem = null;
                heldItemPickup = null;
            
                GameObject placedObject = Instantiate(
                    itemToPlace.Definition.WorldPrefab,
                    placementPreviewPosition,
                    Quaternion.Euler(previewRotation));
        
                ItemPickup pickup = placedObject.AddComponent<ItemPickup>();
                pickup.Initialize(itemToPlace);
            
                if (!placedObject.TryGetComponent(out Rigidbody itemRb))
                    itemRb = placedObject.AddComponent<Rigidbody>();
                itemRb.isKinematic = false;
                itemRb.useGravity = true;
            
                 if (!placedObject.TryGetComponent<Collider>(out _))
                {
                    MeshCollider mc = placedObject.AddComponent<MeshCollider>();
                    mc.convex = true;
                }
            
                   // Destroy the camera-parented visual (only exists for items, not shelves/containers)
                if (heldItemVisual != null)
                {
                    Destroy(heldItemVisual);
                }
            }
                
             else if (heldContainer != null)
            {
                heldItemVisualTransform.rotation = Quaternion.Euler(previewRotation);

                   foreach (Collider col in heldContainer.GetComponentsInChildren<Collider>())
                    col.enabled = true;
                if (heldContainer.TryGetComponent(out Rigidbody rb))
                {
                    rb.isKinematic = false;
                     rb.useGravity = true;
                }
        
                heldContainer = null;
            }
                
            else if (heldShelf != null)
            {
                    Ray wallRay = new Ray(playerCamera.position, playerCamera.forward);
                       bool isWallMount = Physics.Raycast(wallRay, out RaycastHit hit, 5f) &&
                        hit.collider.gameObject.layer == LayerMask.NameToLayer("Wall");
                
                    heldItemVisualTransform.rotation = isWallMount
                        ? Quaternion.LookRotation(hit.normal, Vector3.up) * Quaternion.Euler(heldShelf.RotationOffset)
                        : Quaternion.Euler(previewRotation);
                
                    // Unparent any stocked items that were visually parented during pickup
                    List<Transform> itemsToUnparent = new();
                    foreach (Transform child in heldItemVisualTransform)
                    {
                        if (child.GetComponent<ItemPickup>() != null)
                              itemsToUnparent.Add(child);
                    }
                    foreach (Transform item in itemsToUnparent)
                       {
                        item.SetParent(null);
                        if (!isWallMount)
                        {
                            if (item.TryGetComponent(out Rigidbody itemRb))
                               {
                                itemRb.isKinematic = false;
                                itemRb.useGravity = true;
                            }
                            foreach (Collider col in item.GetComponentsInChildren<Collider>())
                                col.enabled = true;
                           }
                    }
                
                      // Restore shelf physics
                    foreach (Collider col in heldShelf.GetComponentsInChildren<Collider>())
                        col.enabled = true;
                    if (heldShelf.TryGetComponent(out Rigidbody rb))
                    {
                           rb.isKinematic = isWallMount;
                        rb.useGravity = !isWallMount;
                    }
                
                      heldShelf = null;
            }
                
               if (placementPreviewVisual != null)
            {
                Destroy(placementPreviewVisual);
                placementPreviewVisual = null;
               }
                
               heldItemVisual = null;
            heldItemVisualTransform = null;
            previewRotation = Vector3.zero;        
        }


        private void TryStoreItem(StorageContainer container)
        {
            if (heldItem == null || container == null)
                return;

            if (container.TryAddItem(heldItem))
            {
                UnityEngine.Debug.Log($"Stored {heldItem.Definition.DisplayName} in {container.name}");
                ClearHeldState();
                StorageInventoryUI.Instance?.RefreshUI(container);
            }
        }

        private void TryStockItemOnShelf(ShelfComponent shelf)
        {
            if (heldItem == null || shelf == null)
                return;

            if (!shelf.TryAddItem(heldItem))
            {
                 Debug.Log($"Cannot stock {heldItem.Definition.DisplayName} on {shelf.name}");
                    return;
            }
            
            Debug.Log($"Stocked {heldItem.Definition.DisplayName} on {shelf.name}");
            
            ItemInstance itemToStock = heldItem;
            GameObject stockedVisual = Instantiate(
                itemToStock.Definition.WorldPrefab,
                shelf.GetSlotPosition(itemToStock) + shelf.GetStockingOffset(),
                Quaternion.Euler(shelf.GetStockingRotation()));
                
            int itemLayer = LayerMask.NameToLayer("Item");
            foreach (Transform t in stockedVisual.GetComponentsInChildren<Transform>())
                t.gameObject.layer = itemLayer;

            ItemPickup pickup = stockedVisual.AddComponent<ItemPickup>();
            pickup.Initialize(itemToStock);

            if (!stockedVisual.TryGetComponent<Collider>(out _))
            {
                MeshCollider col = stockedVisual.AddComponent<MeshCollider>();
                col.convex = true;
            }

            if (!stockedVisual.TryGetComponent(out Rigidbody rb))
                rb = stockedVisual.AddComponent<Rigidbody>();
            rb.isKinematic = true;

            ClearHeldState();
        }

        
        private void DropInteractable()
        {
            // Drop position: slightly in front of and below the camera
            Vector3 dropPosition = playerCamera.position + playerCamera.forward * 0.8f + Vector3.down * 0.3f;
        
            if (heldItem != null)
            {
                ItemInstance itemToDrop = heldItem;
                
                GameObject droppedObject = Instantiate(
                    itemToDrop.Definition.WorldPrefab,
                    dropPosition,
                    Quaternion.Euler(previewRotation));
        
                ItemPickup pickup = droppedObject.AddComponent<ItemPickup>();
                pickup.Initialize(itemToDrop);
        
                if (!droppedObject.TryGetComponent(out Rigidbody itemRb))
                    itemRb = droppedObject.AddComponent<Rigidbody>();
                itemRb.isKinematic = false;
                itemRb.useGravity = true;
                // Give it a little forward toss
                itemRb.linearVelocity = playerCamera.forward * 2f;
        
                if (!droppedObject.TryGetComponent<Collider>(out _))
                {
                    MeshCollider mc = droppedObject.AddComponent<MeshCollider>();
                    mc.convex = true;
                }
            }
            else if (heldContainer != null)
            {
                heldItemVisualTransform.SetParent(null);
                heldItemVisualTransform.position = dropPosition;
        
                foreach (Collider col in heldContainer.GetComponentsInChildren<Collider>())
                    col.enabled = true;
                if (heldContainer.TryGetComponent(out Rigidbody rb))
                {
                    rb.isKinematic = false;
                    rb.useGravity = true;
                    rb.linearVelocity = playerCamera.forward * 2f;
                }

                heldItemVisual = null;
                heldItemVisualTransform = null;
                heldContainer = null;
                if (placementPreviewVisual != null) { Destroy(placementPreviewVisual); placementPreviewVisual = null; }
                previewRotation = Vector3.zero;
                return;
            }
            else if (heldShelf != null)
            {
                heldItemVisualTransform.SetParent(null);
                heldItemVisualTransform.position = dropPosition;
            
                // Unparent stocked items
                List<Transform> itemsToUnparent = new();
                foreach (Transform child in heldItemVisualTransform)
                    if (child.GetComponent<ItemPickup>() != null)
                        itemsToUnparent.Add(child);
                foreach (Transform item in itemsToUnparent)
                {
                    item.SetParent(null);
                    if (item.TryGetComponent(out Rigidbody itemRb)) { itemRb.isKinematic = false; itemRb.useGravity = true; }
                    foreach (Collider col in item.GetComponentsInChildren<Collider>()) col.enabled = true;
                }
            
                foreach (Collider col in heldShelf.GetComponentsInChildren<Collider>())
                    col.enabled = true;
                if (heldShelf.TryGetComponent(out Rigidbody rb)) { rb.isKinematic = false; rb.useGravity = true; rb.linearVelocity = playerCamera.forward * 2f; }
            
                heldItemVisual = null;
                heldItemVisualTransform = null;
                heldShelf = null;
                if (placementPreviewVisual != null) { Destroy(placementPreviewVisual); placementPreviewVisual = null; }
                previewRotation = Vector3.zero;
                return;
            }
        
            ClearHeldState();
        }

        
        // Clears all held state and visuals. Call after any successful placement/store/stock.
        private void ClearHeldState()
        {
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
            heldItemVisualTransform = null;
            heldItem = null;
            heldItemPickup = null;
            previewRotation = Vector3.zero;
        }
#endregion


/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
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
            bool holdingContainer = heldContainer != null;
            bool holdingShelf = heldShelf != null;

            if (!holdingItem && !holdingContainer && !holdingShelf)
                return;
            if (GameStateController.Instance.CurrentPhase != GamePhase.Playing)
                return;

            Vector3 previewPos = playerCamera.position + playerCamera.forward * 2f;
            validPlacement = true;

            
            if (holdingShelf)
            {
                if (placementPreviewVisual == null)
                {
                    placementPreviewVisual = Instantiate(heldItemVisual);

                    foreach (Collider col in placementPreviewVisual.GetComponentsInChildren<Collider>())
                        col.enabled = false;
                    if (placementPreviewVisual.TryGetComponent(out Rigidbody rb)) Destroy(rb);
                    if (placementPreviewVisual.TryGetComponent(out ShelfComponent sc)) Destroy(sc);

                    ApplyPreviewMaterial(placementPreviewVisual);
                    UpdatePreviewColor();
                }

                Ray forwardRay = new Ray(playerCamera.position, playerCamera.forward);
                validPlacement = false;
                
                if (Physics.Raycast(forwardRay, out RaycastHit hit, 5f))
                {
                    int layer = hit.collider.gameObject.layer;

                    if (layer == LayerMask.NameToLayer("Wall"))
                    {
                        validPlacement = true;
                        float offset = heldShelf.mountOffsetDistance;
                        previewPos = hit.point + hit.normal * offset;
                        Quaternion wallRotation = Quaternion.LookRotation(hit.normal, Vector3.up)
                                                  * Quaternion.Euler(heldShelf.RotationOffset);
                        placementPreviewVisual.transform.SetPositionAndRotation(previewPos, wallRotation);
                        placementPreviewPosition = previewPos;
                        UpdatePreviewColor();
                        return;
                    }
                    else if (layer == LayerMask.NameToLayer("Ground"))
                    {
                        validPlacement = true;
                        previewPos = hit.point;
                    }
                }
                placementPreviewVisual.transform.SetPositionAndRotation(previewPos, Quaternion.Euler(previewRotation));
                placementPreviewPosition = previewPos;
                UpdatePreviewColor();
                return;
            }


            
            else if (holdingItem)
            {
                if (placementPreviewVisual == null)
                {
                    placementPreviewVisual = Instantiate(heldItem.Definition.WorldPrefab);
                    ApplyPreviewMaterial(placementPreviewVisual);
                    if (placementPreviewVisual.TryGetComponent(out Rigidbody rb)) Destroy(rb);
                }
                placementPreviewVisual.transform.position = previewPos;
                placementPreviewPosition = previewPos;
                UpdatePreviewColor();
                return;
            }

        

            else if (holdingContainer)
            {
                if (placementPreviewVisual == null)
                {
                    placementPreviewVisual = Instantiate(heldItemVisual);
        
                    foreach (Collider col in placementPreviewVisual.GetComponentsInChildren<Collider>())
                        col.enabled = false;
                    if (placementPreviewVisual.TryGetComponent(out Rigidbody rb)) Destroy(rb);
                    if (placementPreviewVisual.TryGetComponent(out StorageInteraction si)) Destroy(si);
        
                    ApplyPreviewMaterial(placementPreviewVisual);
                    placementPreviewVisual.transform.rotation = Quaternion.Euler(previewRotation);
                }
                placementPreviewVisual.transform.position = previewPos;
                placementPreviewPosition = previewPos;
                UpdatePreviewColor();
            }
        }




        private void HandlePreviewRotation()
        {
            bool holdingItem = heldItem != null;
            bool holdingContainer = heldContainer != null;
            bool holdingShelf = heldShelf != null;

            if (!holdingItem && !holdingContainer && !holdingShelf)
            {
                previewRotation = Vector3.zero;
                return;
            }

            //dont allow rotation for snapped shelf previews
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

            //Mouse scroll rotation input
            float scrollInput = Mouse.current.scroll.ReadValue().y / 20f;
            if (scrollInput != 0)
            {
                if (input.rotatePreviewModifier)
                    previewRotation.y += scrollInput * 90f;
                else
                    previewRotation.x += scrollInput * 90f;
            }
            //gamepad rotation input
            previewRotation.x += input.rotatePreviewVertical   * 90f * Time.deltaTime;
            previewRotation.y += input.rotatePreviewHorizontal * 90f * Time.deltaTime;
        
            previewRotation.x %= 360f;
            previewRotation.y %= 360f;

            if (placementPreviewVisual == null)
                return;

            float previewDistance = 2f;
            Ray forwardRay = new Ray(playerCamera.position, playerCamera.forward);
            
            if (Physics.Raycast(forwardRay, out RaycastHit fwdHit, 10f) &&
            fwdHit.collider.gameObject != placementPreviewVisual &&
            fwdHit.collider.gameObject != heldItemVisual)
            {
                previewDistance = Mathf.Clamp(fwdHit.distance - 0.3f, 0.5f, 2.5f);
            }
            Vector3 previewPos = playerCamera.position + playerCamera.forward * previewDistance;
        
            // Clamp to floor height
            if (Physics.Raycast(new Ray(previewPos, Vector3.down), out RaycastHit floorHit, 100f) &&
                floorHit.collider.gameObject.layer == LayerMask.NameToLayer("Ground"))
            {
                previewPos.y = Mathf.Max(previewPos.y, floorHit.point.y + 0.5f);
            }
        
            placementPreviewVisual.transform.SetPositionAndRotation(previewPos, Quaternion.Euler(previewRotation));
            placementPreviewPosition = previewPos;
        }
#endregion


/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
#region Helpers
        public bool IsLookingAtSuitableShelfMountingPosition
        {
            get
            {
                // Only relevant when holding a shelf
                if (heldShelf == null)
                    return false;
        
                Ray ray = new Ray(playerCamera.position, playerCamera.forward);
                if (Physics.Raycast(ray, out RaycastHit hit, 5f))
                {
                    return hit.collider.gameObject.layer == LayerMask.NameToLayer("Wall");
                }
        
                return false;
            }
        }

        // Applies a semi-transparent dark tint to all renderers on a GameObject.
        private void ApplyPreviewMaterial(GameObject target)
        {
            foreach (Renderer renderer in target.GetComponentsInChildren<Renderer>())
            {
                foreach (Material mat in renderer.materials)
                {
                    Color c = mat.color;
                    c.r *= 0.2f;
                    c.g *= 0.2f;
                    c.b *= 0.2f;
                    c.a = 0.5f;
                    mat.color = c;
                }
            }
        }

        
        private void UpdatePreviewColor()
        {
            if (placementPreviewVisual == null) return;
        
            foreach (Renderer renderer in placementPreviewVisual.GetComponentsInChildren<Renderer>())
            {
                foreach (Material mat in renderer.materials)
                {
                    // Red tint = invalid, dark tint = valid
                    mat.color = validPlacement
                        ? new Color(0.2f, 0.2f, 0.2f, 0.5f)
                        : new Color(0.8f, 0.1f, 0.1f, 0.5f);
                }
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


/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
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
