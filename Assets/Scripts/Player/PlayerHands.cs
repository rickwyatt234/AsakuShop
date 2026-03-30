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
        public Transform player;

        public ItemInstance heldItem;
        public ItemPickup heldItemPickup;

        public IHoldable heldContainer;
        public IShelfHoldable heldShelf;
        public IMountableHoldable heldMountable;
        public int _storedItemFrame = -1; // Frame count when an item was stored in a container, used to prevent immediate re-pickup
        private bool _suppressExamineUntilRelease = false; // Used to prevent examine action immediately after storing an item

        private bool previousInteractState = false;
        private bool previousExamineState = false;
        private bool interactPressedThisFrame = false;
        private bool examinePressedThisFrame = false;
        public bool IsHoldingInteractable => heldItem != null || heldContainer != null || 
                                            heldShelf != null || heldMountable != null || 
                                            _suppressExamineUntilRelease;
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
            player = playerCamera.parent;
            input = GetComponent<IInputManager>();
            PlayerService.PickupTarget   = this;
            PlayerService.InputManager   = input;
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

            if (_suppressExamineUntilRelease && !input.examine)
                _suppressExamineUntilRelease = false;

            HandlePlacementPreview();
            HandlePreviewRotation();
            HandleHeldItemInput(interactPressedThisFrame, examinePressedThisFrame);

            // Handle recently dropped object ignore timer
            if (recentlyDroppedObject != null)
            {
                dropIgnoreTimer -= Time.deltaTime;
                if (dropIgnoreTimer <= 0f)
                {
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
            if (heldContainer != null)
                return heldContainer as IInteractable;
            if (heldMountable != null && heldMountable is IInteractable interactable)
                return interactable;
            if (heldShelf != null)
                return heldShelf as IInteractable;
            return null;
        }
#endregion

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
#region Item Pickup
        public void TryPickupInteractable(GameObject interactableObject)
        {
            if (GameStateController.Instance.CurrentPhase == GamePhase.Checkout)
                return;

            if (interactableObject == recentlyDroppedObject)
                return;

            if (InventoryState.IsOpen) return;
            if (GameStateController.Instance.CurrentPhase != GamePhase.Playing) return;

            IMountableHoldable mountable = interactableObject.GetComponent<IMountableHoldable>();
            IHoldable container = mountable == null ? interactableObject.GetComponent<IHoldable>() : null;
            ItemPickup itemPickup = interactableObject.GetComponent<ItemPickup>();

            if (itemPickup != null || container != null)
            {
                if (!interactPressedThisFrame) 
                    return;
            }
            else if (mountable != null)
            {
                // Determine whether the object is already mounted (placed on a surface).
                bool isMounted = false;
                var shelfContainer = interactableObject.GetComponent<ShelfContainer>();
                if (shelfContainer != null)
                    isMounted = shelfContainer.IsMounted;

                Debug.Log($"[PlayerHands] TryPickupInteractable — mountable '{interactableObject.name}', isMounted={isMounted}.");

                if (isMounted)
                {
                    // Mounted objects are retrieved via Examine, not Interact.
                    if (!examinePressedThisFrame) return;
                }
                else if (!interactPressedThisFrame)
                {
                    return;
                }
            }

            if (itemPickup != null)
            {
                heldItem = itemPickup.ItemInstance;
                heldItemPickup = itemPickup;
                InstantiateHeldItemVisual();
                interactableObject.SetActive(false);
            }
            else if (mountable != null)
            {
                mountable.NotifyPickedUp();

                // Eject and clear all stocked items from a ShelfContainer when picked up.
                var shelfContainer = interactableObject.GetComponent<ShelfContainer>();
                if (shelfContainer != null)
                {
                    Debug.Log($"[PlayerHands] Picking up ShelfContainer '{shelfContainer.name}' — ejecting and clearing stocked items.");
                    StoreManager.Instance?.UnregisterShelf(shelfContainer);
                    shelfContainer.EjectAllStockedItems();
                    shelfContainer.ClearAllShelves();
                }

                heldMountable = mountable;
                heldItemVisual = mountable.GameObject;
                heldItemVisualTransform = heldItemVisual.transform;
                heldItemVisualTransform.SetParent(playerCamera);

                Rigidbody rb = heldItemVisual.GetComponent<Rigidbody>();
                if (rb != null) rb.isKinematic = true;
                foreach (Collider col in heldItemVisual.GetComponentsInChildren<Collider>())
                    col.enabled = false;

                heldItemVisualTransform.DOLocalMove(mountable.HeldOffset, PICKUP_ANIM_DURATION)
                    .SetEase(Ease.OutCubic);
                heldItemVisualTransform.DOLocalRotate(mountable.HeldRotation.eulerAngles, PICKUP_ANIM_DURATION)
                    .SetEase(Ease.OutCubic);

                // For compatibility with old code
                heldShelf = (mountable is IShelfHoldable shelfHoldable) ? shelfHoldable : null;

                //Rebake navmesh
                StoreManager.Instance?.UpdateNavMeshSurface();
            }
            else if (container != null)
            {
                heldContainer = container;
                heldItemVisual = container.GameObject;
                heldItemVisualTransform = heldItemVisual.transform;
                heldItemVisualTransform.SetParent(playerCamera);

                Rigidbody rb = heldItemVisual.GetComponent<Rigidbody>();
                if (rb != null) rb.isKinematic = true;
                foreach (Collider col in heldItemVisual.GetComponentsInChildren<Collider>())
                    col.enabled = false;

                heldItemVisualTransform.DOLocalMove(container.HeldOffset, PICKUP_ANIM_DURATION)
                    .SetEase(Ease.OutCubic);
                heldItemVisualTransform.DOLocalRotate(container.HeldRotation.eulerAngles, PICKUP_ANIM_DURATION)
                    .SetEase(Ease.OutCubic);

                //Rebake navmesh
                StoreManager.Instance?.UpdateNavMeshSurface();
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
                    IShelfHoldable shelf = hit.collider.GetComponent<IShelfHoldable>()
                        ?? hit.collider.GetComponentInParent<IShelfHoldable>();
                    if (shelf != null && heldItem != null)
                    {
                        // Pass the specific Shelf board that was hit (if any) so TryStockItemOnShelf
                        // can prefer it over other child shelves in the same container.
                        Shelf preferredShelf = hit.collider.GetComponent<Shelf>();
                        Debug.Log($"[PlayerHands] Interact raycast hit '{hit.collider.name}' — IShelfHoldable='{shelf.GameObject.name}', preferredShelf='{preferredShelf?.name ?? "none"}'.");
                        TryStockItemOnShelf(shelf, preferredShelf);
                        return;
                    }
                    else if (heldItem != null)
                    {
                        Debug.Log($"[PlayerHands] Interact raycast hit '{hit.collider.name}' — no IShelfHoldable found; skipping stock attempt.");
                    }
                }

                if (validPlacement)
                {
                    TryPlaceInteractable();
                }
                else
                {
                    DropInteractable();
                }
            }

            if (examinePressed)
            {
                Ray ray = new Ray(playerCamera.position, playerCamera.forward);
                if (Physics.Raycast(ray, out RaycastHit hit, 3f))
                {
                    IHoldable container = hit.collider.GetComponent<IHoldable>();
                    if (container != null && !(container is IShelfHoldable) && heldItem != null)
                    {
                        TryStoreItem(container);
                        return;
                    }
                }

                if (heldItem != null)
                {
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
                //Rebake navmesh
                StoreManager.Instance?.UpdateNavMeshSurface();
            }
            else if (heldMountable != null)
            {
                bool placedOnWall = false;
                bool placedOnGround = false;
                Ray ray = new Ray(playerCamera.position, playerCamera.forward);
                RaycastHit hit;
                if (Physics.Raycast(ray, out hit, 5f))
                {
                    int layer = hit.collider.gameObject.layer;
                    if (heldMountable.AllowedSurfaces.HasFlag(MountSurfaceMask.Wall) && layer == LayerMask.NameToLayer("Wall"))
                        placedOnWall = true;
                    if (heldMountable.AllowedSurfaces.HasFlag(MountSurfaceMask.Ground) && layer == LayerMask.NameToLayer("Ground"))
                        placedOnGround = true;
                }

                if (placedOnWall && heldMountable.AlignToSurfaceNormal)
                {
                    heldItemVisualTransform.rotation = Quaternion.LookRotation(hit.normal, Vector3.up) *
                                                       Quaternion.Euler((heldMountable is IShelfHoldable sh) ? sh.RotationOffset : Vector3.zero);
                }
                else
                {
                    heldItemVisualTransform.rotation = Quaternion.Euler(previewRotation);
                }

                foreach (Collider col in heldMountable.GameObject.GetComponentsInChildren<Collider>())
                    col.enabled = true;

                if (heldMountable.GameObject.TryGetComponent(out Rigidbody rb))
                {
                    rb.isKinematic = placedOnWall || placedOnGround;
                    rb.useGravity = false;
                }

                var shelfContainer = heldMountable.GameObject.GetComponent<ShelfContainer>();
                if (shelfContainer != null && (placedOnWall || placedOnGround))
                {
                    Debug.Log($"[PlayerHands] Placing ShelfContainer '{shelfContainer.name}' — placedOnWall={placedOnWall}, placedOnGround={placedOnGround}.");
                    var store = StoreManager.Instance;
                    if (store != null && store.StoreBounds.Contains(shelfContainer.transform.position))
                        store.RegisterShelf(shelfContainer);
                    else
                        Debug.Log($"[PlayerHands] {shelfContainer.name} mounted outside store bounds — not registered.");
                }

                heldMountable.NotifyMounted();
                heldMountable = null;
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

            //Rebake navmesh
            StoreManager.Instance?.UpdateNavMeshSurface();
        }

        private void TryStoreItem(IHoldable container)
        {
            if (heldItem == null || container == null) 
                return;

            IStorageUnit storageUnit = container.GameObject.GetComponent<IStorageUnit>();
            if (storageUnit == null)
                return;
            
            if (storageUnit.TryAddItem(heldItem))
            {
                _suppressExamineUntilRelease = true;
                ClearHeldState();
                CoreEvents.RaiseStorageUIRefreshRequested(container.GameObject);
            }
        }

        private void TryStockItemOnShelf(IShelfHoldable shelf, Shelf preferredShelf = null)
        {
            if (heldItem == null || shelf == null) return;

            Debug.Log($"[PlayerHands] TryStockItemOnShelf — item='{heldItem.Definition.DisplayName}', container='{shelf.GameObject.name}', preferredShelf='{preferredShelf?.name ?? "none"}'.");

            if (!IsShelfMounted(shelf))
            {
                Debug.Log("[PlayerHands] STOCK blocked — shelf container is not mounted.");
                return;
            }

            IStorageUnit storageUnit = null;
            IShelfLayout shelfLayout = null;

            // Prefer the specific Shelf board the player was looking at.
            if (preferredShelf != null)
            {
                if (preferredShelf.CanAddItem(heldItem))
                {
                    Debug.Log($"[PlayerHands] STOCK — using preferred Shelf '{preferredShelf.name}'.");
                    storageUnit = preferredShelf;
                    shelfLayout = preferredShelf;
                }
                else
                {
                    Debug.Log($"[PlayerHands] STOCK — preferred Shelf '{preferredShelf.name}' cannot accept item; searching other boards.");
                }
            }

            if (storageUnit == null || shelfLayout == null)
            {
                storageUnit = shelf.GameObject.GetComponent<IStorageUnit>();
                shelfLayout = shelf.GameObject.GetComponent<IShelfLayout>();

                if (storageUnit == null || shelfLayout == null)
                {
                    // Fall back to children — Shelf boards keep IStorageUnit / IShelfLayout on the child objects.
                    Debug.Log($"[PlayerHands] STOCK — no IStorageUnit/IShelfLayout on container root; searching children.");
                    foreach (var unit in shelf.GameObject.GetComponentsInChildren<IStorageUnit>())
                    {
                        if (unit.CanAddItem(heldItem) && unit is IShelfLayout layout)
                        {
                            storageUnit = unit;
                            shelfLayout = layout;
                            Debug.Log($"[PlayerHands] STOCK — found accepting Shelf child '{(unit as MonoBehaviour)?.name}'.");
                            break;
                        }
                    }
                }
            }

            if (storageUnit == null)
            {
                Debug.Log("[PlayerHands] STOCK failed — no IStorageUnit found on shelf or any child.");
                return;
            }

            if (shelfLayout == null)
            {
                Debug.Log("[PlayerHands] STOCK failed — no IShelfLayout found on shelf or any child.");
                return;
            }

            if (!storageUnit.CanAddItem(heldItem))
            {
                Debug.Log($"[PlayerHands] STOCK failed — shelf '{(storageUnit as MonoBehaviour)?.name}' is full or item size not supported.");
                return;
            }

            ItemInstance itemToStock = heldItem;
            if (!storageUnit.TryAddItem(itemToStock))
            {
                Debug.Log("[PlayerHands] STOCK failed — TryAddItem returned false unexpectedly.");
                return;
            }

            Vector3 localOffset = shelfLayout.GetSlotPosition(itemToStock) + shelfLayout.GetStockingOffset();
            Vector3 stockingRotationEuler = shelfLayout.GetStockingRotation();

            Debug.Log($"[PlayerHands] STOCK — placing '{itemToStock.Definition.DisplayName}' at localOffset={localOffset}, rotation={stockingRotationEuler}.");

            GameObject visual = heldItemVisual;
            // Parent to the object that owns the slot layout so local positions are correct.
            Transform stockParent = (storageUnit as MonoBehaviour)?.transform ?? shelf.GameObject.transform;
            Debug.Log($"[PlayerHands] STOCK — parenting visual to '{stockParent.name}'.");
            visual.transform.SetParent(stockParent);

            Collider[] colliders = visual.GetComponentsInChildren<Collider>();
            foreach (Collider col in colliders)
                col.enabled = false;

            visual.transform.DOLocalJump(localOffset, STOCKING_JUMP_POWER, 1, STOCKING_ANIM_DURATION)
                .OnComplete(() =>
                {
                    foreach (Collider col in colliders)
                        col.enabled = true;

                    if (!visual.TryGetComponent(out Rigidbody rb))
                        rb = visual.AddComponent<Rigidbody>();
                    rb.isKinematic = true;
                    rb.useGravity  = false;

                    if (!visual.TryGetComponent(out ItemPickup pickup))
                        pickup = visual.AddComponent<ItemPickup>();
                    pickup.Initialize(itemToStock);

                    itemToStock.IsOnAShelf = true;
                    Debug.Log($"[PlayerHands] STOCK complete — '{itemToStock.Definition.DisplayName}' is now on shelf.");

                    int itemLayer = LayerMask.NameToLayer("Item");
                    foreach (Transform t in visual.GetComponentsInChildren<Transform>(true))
                        t.gameObject.layer = itemLayer;
                });

            visual.transform.DOLocalRotate(stockingRotationEuler, STOCKING_ANIM_DURATION);

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
            Vector3 dropPosition = placementPreviewPosition;
            Quaternion dropRotation = Quaternion.Euler(previewRotation);

            if (heldItem != null)
            {
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
                heldItemVisualTransform.SetParent(null);
                heldItemVisualTransform.position = dropPosition;
                heldItemVisualTransform.rotation = Quaternion.Euler(previewRotation);

                List<Transform> itemsToUnparent = new();
                foreach (ItemPickup pickup in heldItemVisualTransform.GetComponentsInChildren<ItemPickup>())
                    itemsToUnparent.Add(pickup.transform);
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
                heldMountable = null;
                if (placementPreviewVisual != null) { Destroy(placementPreviewVisual); placementPreviewVisual = null; }
                previewRotation = Vector3.zero;
                return;
            }
            else if (heldMountable != null)
            {
                heldItemVisualTransform.SetParent(null);
                heldItemVisualTransform.position = dropPosition;
                heldItemVisualTransform.rotation = Quaternion.Euler(previewRotation);

                foreach (Collider col in heldMountable.GameObject.GetComponentsInChildren<Collider>())
                    col.enabled = true;

                if (heldMountable.GameObject.TryGetComponent(out Rigidbody rb))
                {
                    rb.isKinematic = false;
                    rb.useGravity = true;
                    rb.linearVelocity = Vector3.zero;
                }

                recentlyDroppedObject = heldMountable.GameObject;
                dropIgnoreTimer = DROP_IGNORE_DURATION;

                heldItemVisual = null;
                heldItemVisualTransform = null;
                if (heldMountable is IShelfHoldable) heldShelf = null;
                heldMountable = null;
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

            Vector3 spawnWorldPos = heldItemPickup != null
                ? heldItemPickup.transform.position
                : playerCamera.position + playerCamera.forward;
            Quaternion spawnWorldRot = heldItemPickup != null
                ? heldItemPickup.transform.rotation
                : Quaternion.identity;

            heldItemVisual = Instantiate(heldItem.Definition.WorldPrefab, spawnWorldPos, spawnWorldRot);
            heldItemVisualTransform = heldItemVisual.transform;
            heldItemVisualTransform.SetParent(playerCamera);

            Rigidbody rb = heldItemVisual.GetComponent<Rigidbody>();
            if (rb != null) Destroy(rb);

            foreach (Collider col in heldItemVisual.GetComponentsInChildren<Collider>())
                col.enabled = false;

            heldItemVisualTransform.DOLocalJump(
                new Vector3(0.5f, -0.5f, 1f),
                0.15f, 1, PICKUP_ANIM_DURATION)
                .SetEase(Ease.OutCubic);

            heldItemVisualTransform.DOLocalRotate(
                Quaternion.Euler(-90f, 90f, 0f).eulerAngles,
                PICKUP_ANIM_DURATION)
                .SetEase(Ease.OutCubic);
        }

        private void HandlePlacementPreview()
        {
            bool holdingItem = heldItem != null;
            bool holdingContainer = heldContainer != null;
            bool holdingMountable = heldMountable != null;

            if (!holdingItem && !holdingContainer && !holdingMountable) return;
            if (GameStateController.Instance.CurrentPhase != GamePhase.Playing) return;

            Vector3 previewPos = playerCamera.position + playerCamera.forward * 2f;
            validPlacement = true;

            if (holdingMountable)
            {
                if (placementPreviewVisual == null)
                {
                    placementPreviewVisual = Instantiate(heldItemVisual);
                    foreach (Collider col in placementPreviewVisual.GetComponentsInChildren<Collider>())
                        col.enabled = false;
                    if (placementPreviewVisual.TryGetComponent(out Rigidbody rb)) Destroy(rb);
                    ApplyPreviewMaterial(placementPreviewVisual);
                }

                Ray forwardRay = new Ray(playerCamera.position, playerCamera.forward);
                validPlacement = false;

                if (Physics.Raycast(forwardRay, out RaycastHit hit, 5f))
                {
                    int layer = hit.collider.gameObject.layer;
                    bool wallAllowed = heldMountable.AllowedSurfaces.HasFlag(MountSurfaceMask.Wall);
                    bool groundAllowed = heldMountable.AllowedSurfaces.HasFlag(MountSurfaceMask.Ground);

                    if (wallAllowed && layer == LayerMask.NameToLayer("Wall"))
                    {
                        validPlacement = true;
                        previewPos = hit.point + hit.normal * (heldMountable is IShelfHoldable sh ? sh.MountOffsetDistance : 0.5f);
                        Quaternion wallRotation = heldMountable.AlignToSurfaceNormal
                            ? Quaternion.LookRotation(hit.normal, Vector3.up) *
                            Quaternion.Euler((heldMountable is IShelfHoldable sh2) ? sh2.RotationOffset : Vector3.zero)
                            : Quaternion.Euler(previewRotation);
                        placementPreviewVisual.transform.SetPositionAndRotation(previewPos, wallRotation);
                        placementPreviewPosition = previewPos;
                        return;
                    }
                    else if (groundAllowed && layer == LayerMask.NameToLayer("Ground"))
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
                validPlacement = false;

                if (placementPreviewVisual == null)
                {
                    placementPreviewVisual = Instantiate(heldItemVisual);
                    foreach (Collider col in placementPreviewVisual.GetComponentsInChildren<Collider>())
                        col.enabled = false;
                    if (placementPreviewVisual.TryGetComponent(out Rigidbody rb)) Destroy(rb);
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
            bool holdingMountable = heldMountable != null;

            if (!holdingItem && !holdingContainer && !holdingMountable)
            {
                previewRotation = Vector3.zero;
                return;
            }

            if (holdingMountable)
            {
                Ray checkRay = new Ray(playerCamera.position, playerCamera.forward);
                if (Physics.Raycast(checkRay, out RaycastHit hit, 5f))
                {
                    int layer = hit.collider.gameObject.layer;

                    // Wall-mounted: zero rotation and hand full control to HandlePlacementPreview.
                    if (heldMountable.AllowedSurfaces.HasFlag(MountSurfaceMask.Wall) &&
                        layer == LayerMask.NameToLayer("Wall") &&
                        heldMountable.AlignToSurfaceNormal)
                    {
                        previewRotation = Vector3.zero;
                        return;
                    }

                    // Ground-mounted: HandlePlacementPreview already set the correct hit.point
                    // position — only apply rotation here so the +0.5 f floor-snap below does
                    // not push the unit up into the air.
                    if (heldMountable.AllowedSurfaces.HasFlag(MountSurfaceMask.Ground) &&
                        layer == LayerMask.NameToLayer("Ground"))
                    {
                        // Still allow manual yaw rotation input before we return.
                        float scrollDelta = Mouse.current.scroll.ReadValue().y / 20f;
                        if (scrollDelta != 0 && heldMountable.AllowManualYawRotation)
                            previewRotation.y += scrollDelta * 90f;
                        previewRotation.y += input.rotatePreviewHorizontal * 90f * Time.deltaTime;
                        previewRotation.y %= 360f;

                        if (placementPreviewVisual != null)
                            placementPreviewVisual.transform.rotation = Quaternion.Euler(previewRotation);
                        return;
                    }
                }
            }

            float scrollInput = Mouse.current.scroll.ReadValue().y / 20f;
            if (scrollInput != 0)
            {
                bool allowYaw = holdingMountable ? heldMountable.AllowManualYawRotation : true;
                if (input.rotatePreviewModifier && allowYaw)
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

        private bool IsShelfMounted(IShelfHoldable shelf)
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

            int wallLayer   = LayerMask.NameToLayer("Wall");
            int groundLayer = LayerMask.NameToLayer("Ground");

            foreach (Vector3 direction in directions)
            {
                Ray wallCheckRay = new Ray(shelf.GameObject.transform.position, direction);
                if (Physics.Raycast(wallCheckRay, out RaycastHit hit, 2f))
                {
                    int layer = hit.collider.gameObject.layer;
                    if (layer == wallLayer || layer == groundLayer)
                        return true;
                }
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

        public Transform GetTransform()
        {            
            if (PlayerService.PickupTarget is MonoBehaviour mb)
            {
                Transform current = mb.transform;
                while (current.parent != null)
                    current = current.parent;
                return current;
            }
            return null;
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