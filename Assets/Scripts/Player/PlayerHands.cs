using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using AsakuShop.Items;
using AsakuShop.Input;
using AsakuShop.Core;


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
            if (InventoryState.IsOpen) return;
            if (GameStateController.Instance.CurrentPhase != GamePhase.Playing) return;

            IShelfHoldable shelf     = interactableObject.GetComponent<IShelfHoldable>();
            IHoldable container      = shelf == null ? interactableObject.GetComponent<IHoldable>() : null;
            ItemPickup itemPickup    = interactableObject.GetComponent<ItemPickup>();

            if (itemPickup != null)
            {
                heldItem = itemPickup.ItemInstance;
                heldItemPickup = itemPickup;
                InstantiateHeldItemVisual();
                Destroy(interactableObject);
                Debug.Log($"Picked up item: {heldItem.Definition.DisplayName}");
            }
            else if (shelf != null)
            {
                heldShelf = shelf;

                heldItemVisual = shelf.GameObject;
                heldItemVisualTransform = heldItemVisual.transform;
                heldItemVisualTransform.SetParent(playerCamera);
                heldItemVisualTransform.localPosition = shelf.HeldOffset;
                heldItemVisualTransform.localRotation = shelf.HeldRotation;

                Rigidbody rb = heldItemVisual.GetComponent<Rigidbody>();
                if (rb != null) rb.isKinematic = true;
                foreach (Collider col in heldItemVisual.GetComponentsInChildren<Collider>())
                    col.enabled = false;
                Debug.Log($"Picked up: {shelf.DisplayName}");
            }
            else if (container != null)
            {
                heldContainer = container;

                heldItemVisual = container.GameObject;
                heldItemVisualTransform = heldItemVisual.transform;
                heldItemVisualTransform.SetParent(playerCamera);
                heldItemVisualTransform.localPosition = container.HeldOffset;
                heldItemVisualTransform.localRotation = container.HeldRotation;

                Rigidbody rb = heldItemVisual.GetComponent<Rigidbody>();
                if (rb != null) rb.isKinematic = true;
                foreach (Collider col in heldItemVisual.GetComponentsInChildren<Collider>())
                    col.enabled = false;
                Debug.Log($"Picked up storage container: {container.DisplayName}");
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
            if (!IsHoldingInteractable) return;
            if (GameStateController.Instance.CurrentPhase != GamePhase.Playing) return;
            if (InventoryState.IsOpen) return;

            if (input.interact)
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
                    TryPlaceInteractable();
                else
                    DropInteractable();
                return;
            }

            if (input.examine)
            {
                Ray ray = new Ray(playerCamera.position, playerCamera.forward);
                if (Physics.Raycast(ray, out RaycastHit hit, 3f))
                {
                    IHoldable container = hit.collider.GetComponent<IHoldable>();
                    // Only store into non-shelf holdables (containers)
                    if (container != null && !(container is IShelfHoldable) && heldItem != null)
                    {
                        TryStoreItem(container);
                        return;
                    }
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


        private void TryStoreItem(IHoldable container)
        {
            if (heldItem == null || container == null) return;

            IStorageUnit storageUnit = container.GameObject.GetComponent<IStorageUnit>();
            if (storageUnit != null && storageUnit.TryAddItem(heldItem))
            {
                Debug.Log($"Stored {heldItem.Definition.DisplayName} in {container.DisplayName}");
                ClearHeldState();
                CoreEvents.RaiseStorageUIRefreshRequested(container.GameObject);
            }
        }

        private void TryStockItemOnShelf(IShelfHoldable shelf)
        {
            if (heldItem == null || shelf == null) return;

            IStorageUnit storageUnit = shelf.GameObject.GetComponent<IStorageUnit>();
            if (storageUnit == null || !storageUnit.TryAddItem(heldItem))
            {
                Debug.Log($"Cannot stock {heldItem.Definition.DisplayName} on {shelf.DisplayName}");
                return;
            }

            Debug.Log($"Stocked {heldItem.Definition.DisplayName} on {shelf.DisplayName}");

            IShelfLayout shelfLayout = shelf.GameObject.GetComponent<IShelfLayout>();
            Vector3 slotPos = shelfLayout != null
                ? shelfLayout.GetSlotPosition(heldItem) + shelfLayout.GetStockingOffset()
                : shelf.GameObject.transform.position;
            Quaternion slotRot = shelfLayout != null
                ? Quaternion.Euler(shelfLayout.GetStockingRotation())
                : Quaternion.identity;

            ItemInstance itemToStock = heldItem;
            GameObject stockedVisual = Instantiate(itemToStock.Definition.WorldPrefab, slotPos, slotRot);

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

                foreach (Collider col in heldContainer.GameObject.GetComponentsInChildren<Collider>())
                    col.enabled = true;
                if (heldContainer.GameObject.TryGetComponent(out Rigidbody rb))
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

                foreach (Collider col in heldShelf.GameObject.GetComponentsInChildren<Collider>())
                    col.enabled = true;
                if (heldShelf.GameObject.TryGetComponent(out Rigidbody rb)) { rb.isKinematic = false; rb.useGravity = true; rb.linearVelocity = playerCamera.forward * 2f; }

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

            heldItemVisual = Instantiate(heldItem.Definition.WorldPrefab);
            heldItemVisualTransform = heldItemVisual.transform;
            heldItemVisualTransform.SetParent(playerCamera);
            heldItemVisualTransform.localPosition = new Vector3(0.5f, -0.5f, 1f);
            heldItemVisualTransform.localRotation = Quaternion.Euler(0, 90f, 0);

            Rigidbody rb = heldItemVisual.GetComponent<Rigidbody>();
            if (rb != null) Destroy(rb);

            foreach (Collider col in heldItemVisual.GetComponentsInChildren<Collider>())
                col.enabled = false;
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
                        previewPos = hit.point + hit.normal * heldShelf.MountOffsetDistance;
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
                    Component si = placementPreviewVisual.GetComponent(typeof(IStorageUnit)) as Component;
                    if (si != null) Destroy(si);
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

        private void UpdatePreviewColor()
        {
            if (placementPreviewVisual == null) return;
            foreach (Renderer renderer in placementPreviewVisual.GetComponentsInChildren<Renderer>())
            {
                foreach (Material mat in renderer.materials)
                {
                    mat.color = validPlacement
                        ? new Color(0.2f, 0.2f, 0.2f, 0.5f)
                        : new Color(0.8f, 0.1f, 0.1f, 0.5f);
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
#endregion


/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
#region Debugging
        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(10, 10, 300, 200));
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
