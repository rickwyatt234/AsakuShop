using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic;
using AsakuShop.Items;
using AsakuShop.Core;
using AsakuShop.Storage;
using AsakuShop.Store;
using DG.Tweening;


namespace AsakuShop.Player
{
    public class PlayerHands : MonoBehaviour, IPickupTarget
    {

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
#region Fields
        public Transform playerCamera;

        public ItemInstance heldItem;
        public ItemPickup heldItemPickup;

        public IHoldable heldContainer;
        public IShelfHoldable heldShelf;

        private bool previousInteractState = false;
        private bool previousExamineState = false;
        private bool interactPressedThisFrame = false;
        private bool examinePressedThisFrame = false;
        public bool IsHoldingInteractable => heldItem != null || heldContainer != null || heldShelf != null;

        private GameObject recentlyDroppedObject = null;
        private float dropIgnoreTimer = 0f;
        private const float DROP_IGNORE_DURATION = 0.15f;

        // DOTween stocking animation settings
        private const float STOCKING_JUMP_POWER  = 0.3f; // Arc height for DOLocalJump
        private const float STOCKING_ANIM_DURATION = 0.4f; // Duration (seconds) for jump and rotate tweens

        // DOTween pickup animation settings
        private const float PICKUP_ANIM_DURATION = 0.3f; // Duration (seconds) for pickup arc into hands

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
            // Track input state every frame
            bool currentInteractState = input.interact;
            interactPressedThisFrame = currentInteractState && !previousInteractState;
            previousInteractState = currentInteractState;

            bool currentExamineState = input.examine;
            examinePressedThisFrame = currentExamineState && !previousExamineState;
            previousExamineState = currentExamineState;

            HandlePlacementPreview();
            HandlePreviewRotation();
            HandleHeldItemInput(interactPressedThisFrame, examinePressedThisFrame);

            // Handle recently dropped object ignore timer
            if (recentlyDroppedObject != null)
            {
                dropIgnoreTimer -= Time.deltaTime;
                if (dropIgnoreTimer <= 0f)
                {
                    //Debug.Log($"[DROP_IGNORE] Timer expired for {recentlyDroppedObject.name}");
                    recentlyDroppedObject = null;
                }
            }
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

        public IInteractable GetHeldInteractable()
        {
            if (heldItem != null && heldItemPickup != null)
                return heldItemPickup;
            // StorageContainer and ShelfComponent both implement IInteractable; cast is safe
            if (heldContainer != null)
                return heldContainer as IInteractable;
            if (heldShelf != null)
                return heldShelf as IInteractable;
            return null;
        }
#endregion


/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
#region Item Pickup
        public void TryPickupInteractable(GameObject interactableObject)
        {
            //Debug.Log($"[PICKUP] Called on {interactableObject.name}. interactPressedThisFrame: {interactPressedThisFrame}");

            // Ignore recently dropped objects
            if (interactableObject == recentlyDroppedObject)
            {
                //Debug.Log("[PICKUP] IGNORED: Object was recently dropped");
                return;
            }

            if (InventoryState.IsOpen) return;
            if (GameStateController.Instance.CurrentPhase != GamePhase.Playing) return;


            IShelfHoldable shelf     = interactableObject.GetComponent<IShelfHoldable>();
            IHoldable container      = shelf == null ? interactableObject.GetComponent<IHoldable>() : null;
            ItemPickup itemPickup    = interactableObject.GetComponent<ItemPickup>();

            if (itemPickup != null || container != null)
            {
                //Debug.Log($"[PICKUP] Item/Container requires interactPressedThisFrame. Current: {interactPressedThisFrame}");

                if (!interactPressedThisFrame) 
                {
                    //Debug.Log("[PICKUP] BLOCKED: interactPressedThisFrame is false");

                    return;
                }
            }
            else if (shelf != null && !IsShelfWallMounted(shelf))
            {
                //Debug.Log($"[PICKUP] Floor Shelf requires interactPressedThisFrame. Current: {interactPressedThisFrame}");

                if (!interactPressedThisFrame) 
                {
                    //Debug.Log("[PICKUP] BLOCKED: interactPressedThisFrame is false");

                    return;
                }
            }

            if (itemPickup != null)
            {
                heldItem = itemPickup.ItemInstance;
                heldItemPickup = itemPickup;
                InstantiateHeldItemVisual();
                interactableObject.SetActive(false);
                //Debug.Log($"Picked up item: {heldItem.Definition.DisplayName}");
            }
            else if (shelf != null)
            {
                //Debug.Log("[PICKUP] Picking up shelf");

                // Before holding the shelf, eject any stocked items so they fall to the floor.
                ShelfComponent shelfComponent = shelf.GameObject.GetComponent<ShelfComponent>();
                if (shelfComponent != null)
                {
                    shelfComponent.NotifyPickedUp();
                    StoreManager.Instance?.UnregisterShelf(shelfComponent);
                    shelfComponent.EjectAllStockedItems();
                }

                // Also clear the shelf's logical storage data so the shelf starts empty.
                IStorageUnit storageUnit = shelf.GameObject.GetComponent<IStorageUnit>();
                if (storageUnit != null)
                    storageUnit.ClearAllItems();

                heldShelf = shelf;

                heldItemVisual = shelf.GameObject;
                heldItemVisualTransform = heldItemVisual.transform;
                // Parent to camera — Unity preserves world position, giving us the correct start local pos.
                heldItemVisualTransform.SetParent(playerCamera);

                Rigidbody rb = heldItemVisual.GetComponent<Rigidbody>();
                if (rb != null) rb.isKinematic = true;
                foreach (Collider col in heldItemVisual.GetComponentsInChildren<Collider>())
                    col.enabled = false;

                // DOLocalMove animates from the shelf's current local position to the held offset.
                heldItemVisualTransform.DOLocalMove(shelf.HeldOffset, PICKUP_ANIM_DURATION)
                    .SetEase(Ease.OutCubic);
                // DOLocalRotate smoothly orients the shelf to the held rotation.
                heldItemVisualTransform.DOLocalRotate(shelf.HeldRotation.eulerAngles, PICKUP_ANIM_DURATION)
                    .SetEase(Ease.OutCubic);
                //Debug.Log($"Picked up: {shelf.DisplayName}");
            }
            else if (container != null)
            {
                //Debug.Log("[PICKUP] Picking up container");

                heldContainer = container;

                heldItemVisual = container.GameObject;
                heldItemVisualTransform = heldItemVisual.transform;
                // Parent to camera — Unity preserves world position, giving us the correct start local pos.
                heldItemVisualTransform.SetParent(playerCamera);

                Rigidbody rb = heldItemVisual.GetComponent<Rigidbody>();
                if (rb != null) rb.isKinematic = true;
                foreach (Collider col in heldItemVisual.GetComponentsInChildren<Collider>())
                    col.enabled = false;

                // DOLocalMove animates from the container's current local position to the held offset.
                heldItemVisualTransform.DOLocalMove(container.HeldOffset, PICKUP_ANIM_DURATION)
                    .SetEase(Ease.OutCubic);
                // DOLocalRotate smoothly orients the container to the held rotation.
                heldItemVisualTransform.DOLocalRotate(container.HeldRotation.eulerAngles, PICKUP_ANIM_DURATION)
                    .SetEase(Ease.OutCubic);
                //Debug.Log($"Picked up storage container: {container.DisplayName}");
            }
            else
            {
                //Debug.Log("[PLAYER] Not looking at anything");
            }
        }
#endregion


/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
#region Placement

        private void HandleHeldItemInput(bool interactPressed, bool examinePressed)
        {
            if (!IsHoldingInteractable) return;
            if (GameStateController.Instance.CurrentPhase != GamePhase.Playing) return;
            if (InventoryState.IsOpen) return;


            if (interactPressed)
            {
                Ray ray = new Ray(playerCamera.position, playerCamera.forward);
                if (Physics.Raycast(ray, out RaycastHit hit, 3f))
                {
                    IShelfHoldable shelf = hit.collider.GetComponent<IShelfHoldable>();
                    if (shelf != null && heldItem != null)
                    {
                        TryStockItemOnShelf(shelf);
                        return;
                    }
                }


                if (validPlacement)
                {
                    //Debug.Log($"[PLACEMENT] Placing container/shelf (validPlacement is TRUE)");
                    TryPlaceInteractable();
                }
                else
                {
                    //Debug.Log($"[PLACEMENT] Dropping container/shelf (validPlacement is FALSE)");
                    DropInteractable();
                }
            }

            if (examinePressed)
            {
                // First, try to store into a container
                Ray ray = new Ray(playerCamera.position, playerCamera.forward);
                if (Physics.Raycast(ray, out RaycastHit hit, 3f))
                {
                    IHoldable container = hit.collider.GetComponent<IHoldable>();
                    if (container != null && !(container is IShelfHoldable) && heldItem != null)
                    {
                        //Debug.Log($"Attempting to store {heldItem.Definition.DisplayName} into {container.DisplayName}");
                        TryStoreItem(container);
                        return;
                    }
                }

                // Otherwise, examine the held item
                if (heldItem != null)
                {
                    //Debug.Log($"Examining held item: {heldItem.Definition.DisplayName}");
                    CoreEvents.RaiseExamineRequested(heldItem);
                }
            }
        }


        private void TryPlaceInteractable()
        {
            if (InventoryState.IsOpen) return;
            if (!validPlacement) return;

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

                if (heldItemVisual != null)
                    Destroy(heldItemVisual);
            }
            else if (heldContainer != null)
            {
                heldItemVisualTransform.rotation = Quaternion.Euler(previewRotation);

                foreach (Collider col in heldContainer.GameObject.GetComponentsInChildren<Collider>())
                    col.enabled = true;
                if (heldContainer.GameObject.TryGetComponent(out Rigidbody rb))
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

                foreach (Collider col in heldShelf.GameObject.GetComponentsInChildren<Collider>())
                    col.enabled = true;
                if (heldShelf.GameObject.TryGetComponent(out Rigidbody shelfRb))
                {
                    shelfRb.isKinematic = isWallMount;
                    shelfRb.useGravity = !isWallMount;
                }
                //Debug.Log($"IsShelfWallMounted(heldShelf): {IsShelfWallMounted(heldShelf)}");
                heldShelf = null;

                // Notify and register with StoreManager if the shelf was placed on a wall inside the store.
                ShelfComponent sc = heldItemVisualTransform?.GetComponent<ShelfComponent>();
                if (sc != null && isWallMount)
                {
                    sc.NotifyMounted();
                    var store = StoreManager.Instance;
                    if (store != null && store.StoreBounds.Contains(sc.transform.position))
                        store.RegisterShelf(sc);
                    else
                        Debug.Log($"[PlayerHands] {sc.name} mounted outside store bounds — not registered.");
                }
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



        private void TryStoreItem(IHoldable container)
        {
            if (heldItem == null || container == null) 
            {
                //Debug.Log("[STORE] Failed: heldItem or container is null");
                return;
            }

            IStorageUnit storageUnit = container.GameObject.GetComponent<IStorageUnit>();
            if (storageUnit == null)
            {
                //Debug.Log("[STORE] Failed: No IStorageUnit found on container");
                return;
            }
            
            if (storageUnit.TryAddItem(heldItem))
            {
                //Debug.Log($"Stored {heldItem.Definition.DisplayName} in {container.DisplayName}");
                ClearHeldState();
                CoreEvents.RaiseStorageUIRefreshRequested(container.GameObject);
            }
            else
            {
                //Debug.Log($"[STORE] Failed to add item to storage unit. Current Weight: {storageUnit.GetCurrentWeight()}, Item Weight: {heldItem.Definition.WeightKg}, Container Capacity: {storageUnit.GetCapacity()}");
            }
        }


        private void TryStockItemOnShelf(IShelfHoldable shelf)
        {
            // Guard 1: both heldItem and shelf must be present.
            if (heldItem == null || shelf == null) return;

            // Guard 2: the shelf must be wall-mounted — floor shelves are picked up, not stocked.
            if (!IsShelfWallMounted(shelf))
            {
                Debug.Log("[STOCK] Cannot stock item on a non-wall-mounted shelf.");
                return;
            }

            // Guard 3: shelf must expose IStorageUnit to register items.
            IStorageUnit storageUnit = shelf.GameObject.GetComponent<IStorageUnit>();
            if (storageUnit == null)
            {
                Debug.Log("[STOCK] Cannot stock item — no IStorageUnit found on shelf.");
                return;
            }

            // Guard 4: shelf must expose IShelfLayout to compute slot positions.
            IShelfLayout shelfLayout = shelf.GameObject.GetComponent<IShelfLayout>();
            if (shelfLayout == null)
            {
                Debug.Log("[STOCK] Cannot stock item — no IShelfLayout found on shelf.");
                return;
            }

            // Guard 5: verify the item can be stocked (size allowed, contiguous slots available).
            if (!storageUnit.CanAddItem(heldItem))
            {
                Debug.Log("Cannot stock item — shelf is full or item size not supported.");
                return;
            }

            // Register the item in the shelf's data; this assigns the anchor slot index.
            ItemInstance itemToStock = heldItem;
            if (!storageUnit.TryAddItem(itemToStock))
            {
                Debug.Log("[STOCK] TryAddItem failed unexpectedly.");
                return;
            }

            // Step 6: compute the local-space slot position (now that the anchor is assigned).
            // GetSlotPosition returns the centre of the item's occupied run in local shelf space.
            Vector3 localOffset = shelfLayout.GetSlotPosition(itemToStock) + shelfLayout.GetStockingOffset();
            Vector3 stockingRotationEuler = shelfLayout.GetStockingRotation();

            // Step 7: move the existing heldItemVisual — do NOT destroy it and create a new one.
            GameObject visual = heldItemVisual;

            // 7a. Parent the visual to the shelf transform so it travels with the shelf.
            visual.transform.SetParent(shelf.GameObject.transform);

            // Disable colliders during the tween to avoid physics interference.
            Collider[] colliders = visual.GetComponentsInChildren<Collider>();
            foreach (Collider col in colliders)
                col.enabled = false;

            // 7b. DOLocalJump moves the transform to a local-space target position while arcing upward.
            //     Parameters: (Vector3 endValue, float jumpPower, int numJumps, float duration)
            //     jumpPower controls arc height; numJumps = 1 means a single arc; duration is in seconds.
            visual.transform.DOLocalJump(localOffset, STOCKING_JUMP_POWER, 1, STOCKING_ANIM_DURATION)
                .OnComplete(() =>
                {
                    // Re-enable colliders once the tween finishes.
                    foreach (Collider col in colliders)
                        col.enabled = true;

                    // Ensure a Rigidbody exists; keep it kinematic so the item stays put on the shelf.
                    if (!visual.TryGetComponent(out Rigidbody rb))
                        rb = visual.AddComponent<Rigidbody>();
                    rb.isKinematic = true;
                    rb.useGravity  = false;

                    // Ensure ItemPickup exists and bind it to the item so customers can interact later.
                    if (!visual.TryGetComponent(out ItemPickup pickup))
                        pickup = visual.AddComponent<ItemPickup>();
                    pickup.Initialize(itemToStock);

                    // Mark the item as shelved so shelf-browsing and pricing logic can act on it.
                    itemToStock.IsOnAShelf = true;

                    // Set layer on the visual and all its children so raycasts find it correctly.
                    int itemLayer = LayerMask.NameToLayer("Item");
                    foreach (Transform t in visual.GetComponentsInChildren<Transform>(true))
                        t.gameObject.layer = itemLayer;
                });

            // 7c. DOLocalRotate smoothly rotates the transform to the target local Euler angles over the given duration.
            //     The rotation is applied in local space relative to the shelf, so items face the correct
            //     direction regardless of the shelf's world orientation.
            visual.transform.DOLocalRotate(stockingRotationEuler, STOCKING_ANIM_DURATION);

            // Step 9: clear held-item state without destroying the visual (it is now parented to the shelf).
            heldItem = null;
            heldItemPickup = null;
            heldItemVisual = null;
            heldItemVisualTransform = null;
            if (placementPreviewVisual != null)
            {
                Destroy(placementPreviewVisual);
                placementPreviewVisual = null;
            }
            previewRotation = Vector3.zero;
        }


        private void DropInteractable()
        {
            //Drop in front of the player at the preview position, or if no preview, slightly in front of the player
            Vector3 dropPosition = placementPreviewPosition;
            Quaternion dropRotation = Quaternion.Euler(previewRotation);
            
            if (heldItem != null)
            {
                //Debug.Log("[DROP] Dropping item");
                ItemInstance itemToDrop = heldItem;
                GameObject droppedObject = Instantiate(
                    itemToDrop.Definition.WorldPrefab,
                    dropPosition,
                    dropRotation);

                ItemPickup pickup = droppedObject.AddComponent<ItemPickup>();
                pickup.Initialize(itemToDrop);

                if (!droppedObject.TryGetComponent(out Rigidbody itemRb))
                    itemRb = droppedObject.AddComponent<Rigidbody>();
                itemRb.isKinematic = false;
                itemRb.useGravity = true;
                itemRb.linearVelocity = Vector3.zero;

                if (!droppedObject.TryGetComponent<Collider>(out _))
                {
                    MeshCollider mc = droppedObject.AddComponent<MeshCollider>();
                    mc.convex = true;
                }
                
                recentlyDroppedObject = droppedObject;
                dropIgnoreTimer = DROP_IGNORE_DURATION;
            }
            else if (heldContainer != null)
            {
                //Debug.Log("[DROP] Dropping container");
                heldItemVisualTransform.SetParent(null);
                heldItemVisualTransform.position = dropPosition;
                heldItemVisualTransform.rotation = Quaternion.Euler(previewRotation);

                foreach (Collider col in heldContainer.GameObject.GetComponentsInChildren<Collider>())
                    col.enabled = false;
                StartCoroutine(EnableColliderAfterDelay(heldContainer.GameObject, 0.1f));
                
                if (heldContainer.GameObject.TryGetComponent(out Rigidbody rb))
                {
                    rb.isKinematic = false;
                    rb.useGravity = true;
                    rb.linearVelocity = Vector3.zero;
                }

                recentlyDroppedObject = heldContainer.GameObject;
                dropIgnoreTimer = DROP_IGNORE_DURATION;

                heldItemVisual = null;
                heldItemVisualTransform = null;
                heldContainer = null;
                if (placementPreviewVisual != null) { Destroy(placementPreviewVisual); placementPreviewVisual = null; }
                previewRotation = Vector3.zero;
                return;
            }
            else if (heldShelf != null)
            {
                //////Debug.Log("[DROP] Dropping shelf");
                heldItemVisualTransform.SetParent(null);
                heldItemVisualTransform.position = dropPosition;
                heldItemVisualTransform.rotation = Quaternion.Euler(previewRotation);

                List<Transform> itemsToUnparent = new();
                foreach (Transform child in heldItemVisualTransform)
                    if (child.GetComponent<ItemPickup>() != null)
                        itemsToUnparent.Add(child);
                foreach (Transform item in itemsToUnparent)
                {
                    item.SetParent(null);
                    if (item.TryGetComponent(out Rigidbody itemRb)) 
                    { 
                        itemRb.isKinematic = false; 
                        itemRb.useGravity = true; 
                        itemRb.linearVelocity = Vector3.zero; 
                    }
                    foreach (Collider col in item.GetComponentsInChildren<Collider>()) col.enabled = true;
                }

                foreach (Collider col in heldShelf.GameObject.GetComponentsInChildren<Collider>())
                    col.enabled = false;
                StartCoroutine(EnableColliderAfterDelay(heldShelf.GameObject, 0.1f));
                
                if (heldShelf.GameObject.TryGetComponent(out Rigidbody rb)) 
                { 
                    rb.isKinematic = false; 
                    rb.useGravity = true; 
                    rb.linearVelocity = Vector3.zero;
                }

                recentlyDroppedObject = heldShelf.GameObject;
                dropIgnoreTimer = DROP_IGNORE_DURATION;

                heldItemVisual = null;
                heldItemVisualTransform = null;
                heldShelf = null;
                if (placementPreviewVisual != null) { Destroy(placementPreviewVisual); placementPreviewVisual = null; }
                previewRotation = Vector3.zero;
                return;
            }

            ClearHeldState();
        }


        private void ClearHeldState()
        {
            if (heldItemVisual != null) { Destroy(heldItemVisual); heldItemVisual = null; }
            if (placementPreviewVisual != null) { Destroy(placementPreviewVisual); placementPreviewVisual = null; }
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

            // Spawn at the pickup's world position so the DOTween arc starts from there.
            Vector3 spawnWorldPos = heldItemPickup != null
                ? heldItemPickup.transform.position
                : playerCamera.position + playerCamera.forward;
            Quaternion spawnWorldRot = heldItemPickup != null
                ? heldItemPickup.transform.rotation
                : Quaternion.identity;

            heldItemVisual = Instantiate(heldItem.Definition.WorldPrefab, spawnWorldPos, spawnWorldRot);
            heldItemVisualTransform = heldItemVisual.transform;
            // Parent to camera — Unity converts the world pos/rot to local space automatically.
            heldItemVisualTransform.SetParent(playerCamera);

            Rigidbody rb = heldItemVisual.GetComponent<Rigidbody>();
            if (rb != null) Destroy(rb);

            foreach (Collider col in heldItemVisual.GetComponentsInChildren<Collider>())
                col.enabled = false;

            // DOLocalJump animates from the current local position (world spawn pos in camera space)
            // to the held position, with a small arc for a natural "picked up" feel.
            // Parameters: (endValue, jumpPower, numJumps, duration)
            heldItemVisualTransform.DOLocalJump(
                new Vector3(0.5f, -0.5f, 1f),
                0.15f, 1, PICKUP_ANIM_DURATION)
                .SetEase(Ease.OutCubic);

            // DOLocalRotate smoothly spins the item to the standard held orientation.
            // -90 on X corrects world prefabs that are authored laying on their side.
            heldItemVisualTransform.DOLocalRotate(
                Quaternion.Euler(-90f, 90f, 0f).eulerAngles,
                PICKUP_ANIM_DURATION)
                .SetEase(Ease.OutCubic);
        }


        private void HandlePlacementPreview()
        {
            bool holdingItem = heldItem != null;
            bool holdingContainer = heldContainer != null;
            bool holdingShelf = heldShelf != null;

            if (!holdingItem && !holdingContainer && !holdingShelf) return;
            if (GameStateController.Instance.CurrentPhase != GamePhase.Playing) return;

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
                    // Remove the IShelfHoldable component from preview to avoid logic side effects
                    Component shelfComp = placementPreviewVisual.GetComponent<IShelfHoldable>() as Component;
                    if (shelfComp != null) Destroy(shelfComp);
                    ApplyPreviewMaterial(placementPreviewVisual);
                }

                Ray forwardRay = new Ray(playerCamera.position, playerCamera.forward);
                validPlacement = false;

                if (Physics.Raycast(forwardRay, out RaycastHit hit, 5f))
                {
                    int layer = hit.collider.gameObject.layer;
                    if (layer == LayerMask.NameToLayer("Wall"))
                    {
                        validPlacement = true;
                        previewPos = hit.point + hit.normal * heldShelf.MountOffsetDistance;
                        Quaternion wallRotation = Quaternion.LookRotation(hit.normal, Vector3.up)
                                                  * Quaternion.Euler(heldShelf.RotationOffset);
                        placementPreviewVisual.transform.SetPositionAndRotation(previewPos, wallRotation);
                        placementPreviewPosition = previewPos;
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
                placementPreviewVisual.transform.rotation = Quaternion.Euler(previewRotation);
                placementPreviewPosition = previewPos;
                return;
            }
            else if (holdingContainer)
            {
                validPlacement = false;  // Containers never "place"—they always drop
                
                if (placementPreviewVisual == null)
                {
                    placementPreviewVisual = Instantiate(heldItemVisual);
                    foreach (Collider col in placementPreviewVisual.GetComponentsInChildren<Collider>())
                        col.enabled = false;
                    if (placementPreviewVisual.TryGetComponent(out Rigidbody rb)) Destroy(rb);
                    Component si = placementPreviewVisual.GetComponent(typeof(IStorageUnit)) as Component;
                    if (si != null) Destroy(si);
                    ApplyPreviewMaterial(placementPreviewVisual);
                    placementPreviewVisual.transform.rotation = Quaternion.Euler(previewRotation);
                }
                
                previewPos = playerCamera.position + playerCamera.forward * 2f;
                placementPreviewVisual.transform.position = previewPos;
                placementPreviewPosition = previewPos;
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
            if (scrollInput != 0)
            {
                if (input.rotatePreviewModifier)
                    previewRotation.y += scrollInput * 90f;
                else
                    previewRotation.x += scrollInput * 90f;
            }
            previewRotation.x += input.rotatePreviewVertical   * 90f * Time.deltaTime;
            previewRotation.y += input.rotatePreviewHorizontal * 90f * Time.deltaTime;

            previewRotation.x %= 360f;
            previewRotation.y %= 360f;

            if (placementPreviewVisual == null) return;

            float previewDistance = 2f;
            Ray forwardRay = new Ray(playerCamera.position, playerCamera.forward);
            if (Physics.Raycast(forwardRay, out RaycastHit fwdHit, 10f) &&
                fwdHit.collider.gameObject != placementPreviewVisual &&
                fwdHit.collider.gameObject != heldItemVisual)
            {
                previewDistance = Mathf.Clamp(fwdHit.distance - 0.3f, 0.5f, 2.5f);
            }
            Vector3 previewPos = playerCamera.position + playerCamera.forward * previewDistance;

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
                if (heldShelf == null) return false;
                Ray ray = new Ray(playerCamera.position, playerCamera.forward);
                if (Physics.Raycast(ray, out RaycastHit hit, 5f))
                    return hit.collider.gameObject.layer == LayerMask.NameToLayer("Wall");
                return false;
            }
        }

        private void ApplyPreviewMaterial(GameObject target)
        {
            foreach (Renderer renderer in target.GetComponentsInChildren<Renderer>())
            {
                foreach (Material mat in renderer.materials)
                {
                    Color c = mat.color;
                    c.r *= 0.2f; c.g *= 0.2f; c.b *= 0.2f; c.a = 0.5f;
                    mat.color = c;
                }
            }
        }


        private bool IsShelfWallMounted(IShelfHoldable shelf)
        {
            if (shelf == null) return false;
            Rigidbody rb = shelf.GameObject.GetComponent<Rigidbody>();
            if (rb == null || !rb.isKinematic) return false;

            Vector3[] directions = {
                -shelf.GameObject.transform.forward,
                shelf.GameObject.transform.forward,
                shelf.GameObject.transform.right,
                -shelf.GameObject.transform.right,
                shelf.GameObject.transform.up,
                -shelf.GameObject.transform.up
            };

            foreach (Vector3 direction in directions)
            {
                Ray wallCheckRay = new Ray(shelf.GameObject.transform.position, direction);
                if (Physics.Raycast(wallCheckRay, out RaycastHit hit, 2f) &&
                    hit.collider.gameObject.layer == LayerMask.NameToLayer("Wall"))
                    return true;
            }
            return false;
        }

        private IEnumerator EnableColliderAfterDelay(GameObject obj, float delay)
        {
            yield return new WaitForSeconds(delay);
            if (obj != null)
            {
                foreach (Collider col in obj.GetComponentsInChildren<Collider>())
                    col.enabled = true;
            }
        }
#endregion


/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
#region Debugging
        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(0, 40, 300, 200));
            if (heldItem != null)
                GUILayout.Label($"Holding: {heldItem.Definition.DisplayName}");
            else if (heldContainer != null)
                GUILayout.Label($"Holding: {heldContainer.DisplayName}");
            else if (heldShelf != null)
                GUILayout.Label($"Holding: {heldShelf.DisplayName}");
            GUILayout.EndArea();
        }
#endregion
    }
}