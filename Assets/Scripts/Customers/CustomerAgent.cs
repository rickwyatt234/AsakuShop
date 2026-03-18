using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;
using AsakuShop.Items;
using AsakuShop.Storage;
using AsakuShop.Store;

namespace AsakuShop.Customers
{
    public class CustomerAgent : MonoBehaviour, IStoreCustomer
    {
        private enum State 
        { 
            WalkingToStore,          // Navigate to store entrance
            Spawning,                // Wait in store area before browsing
            Browsing, 
            WalkingToCheckout, 
            WaitingForPlayerAtCheckout,
            PlacingItems,
            Paying, 
            Leaving 
        }

        [SerializeField] private float browseTimePerShelf = 6f;
        [SerializeField] private float maxTotalBrowseTime = 60f;
        [SerializeField] private float stoppingDistance = 0.5f;
        [SerializeField] private float itemPlacementHeight = 1.1f;

        private State currentState = State.WalkingToStore;  // START HERE
        private List<ItemInstance> shoppingList;
        private List<ItemInstance> shoppingCart = new();
        private NavMeshAgent navAgent;
        private Transform checkoutTarget;
        private CheckoutZone checkoutZone;
        private CustomerQueue customerQueue;
        private StoreArea storeArea;
        private bool inStoreArea = false;

        private float browseTimer = 0f;
        private float currentShelfBrowseTimer = 0f;
        private Transform currentBrowseTarget;
        private ShelfComponent currentBrowseShelf;
        private bool itemsPlacedOnCounter = false;
        private List<ShelfComponent> visitedShelves = new();

        private void Start()
        {
            navAgent = GetComponent<NavMeshAgent>();
            if (navAgent == null)
            {
                navAgent = gameObject.AddComponent<NavMeshAgent>();
                navAgent.speed = 2f;
                navAgent.stoppingDistance = stoppingDistance;
                navAgent.height = 1.8f;
                navAgent.baseOffset = 0.9f;
                navAgent.radius = 0.25f;
                navAgent.avoidancePriority = 50;
            }

            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = gameObject.AddComponent<Rigidbody>();
            }
            rb.isKinematic = true;
            rb.constraints = RigidbodyConstraints.FreezeAll;
            
            if (GetComponent<CapsuleCollider>() == null)
            {
                CapsuleCollider col = gameObject.AddComponent<CapsuleCollider>();
                col.height = 1.8f;
                col.radius = 0.25f;
            }

            FindCheckoutAndQueue();
            TransitionToState(State.WalkingToStore);  // Walk to store first
        }

        private void Update()
        {
            browseTimer -= Time.deltaTime;
            currentShelfBrowseTimer -= Time.deltaTime;

            switch (currentState)
            {
                case State.WalkingToStore:
                    UpdateWalkingToStore();
                    break;
                case State.Spawning:
                    UpdateSpawning();
                    break;
                case State.Browsing:
                    UpdateBrowsing();
                    break;
                case State.WalkingToCheckout:
                    UpdateWalkingToCheckout();
                    break;
                case State.WaitingForPlayerAtCheckout:
                    UpdateWaitingForPlayer();
                    break;
                case State.PlacingItems:
                    UpdatePlacingItems();
                    break;
                case State.Paying:
                    UpdatePaying();
                    break;
                case State.Leaving:
                    UpdateLeaving();
                    break;
            }
        }

        private void OnEnable()
        {
            CheckoutEvents.OnItemScanned += HandleItemScanned;
        }

        private void OnDisable()
        {
            CheckoutEvents.OnItemScanned -= HandleItemScanned;
        }

        private void HandleItemScanned(ItemInstance scannedItem)
        {
            if (shoppingCart.Contains(scannedItem))
            {
                shoppingCart.Remove(scannedItem);
                Debug.Log($"[CUSTOMER] Item {scannedItem.Definition.DisplayName} was scanned and removed from cart");
            }
        }

        public void SetShoppingList(List<ItemInstance> list)
        {
            shoppingList = new List<ItemInstance>(list);
            Debug.Log($"[CUSTOMER] Shopping for {shoppingList.Count} items");
        }

        private void UpdateWalkingToStore()
        {
            // Walk toward store area
            if (storeArea == null)
            {
                Debug.LogWarning("[CUSTOMER] No StoreArea found!");
                TransitionToState(State.Browsing);  // Skip to browsing if no store
                return;
            }

            // Navigate to store area
            navAgent.SetDestination(storeArea.transform.position);

            // Once close enough or entered trigger, wait for trigger
            if (inStoreArea)
            {
                Debug.Log("[CUSTOMER] Entered store!");
                TransitionToState(State.Spawning);
            }
        }

        private void UpdateSpawning()
        {
            // Already in store, transition to browsing
            Debug.Log("[CUSTOMER] Starting to browse");
            TransitionToState(State.Browsing);
        }

        private void UpdateBrowsing()
        {
            if (browseTimer <= 0f)
            {
                Debug.Log($"[CUSTOMER] Browse time expired. Found {shoppingCart.Count}/{shoppingList.Count}. Going to checkout.");
                TransitionToState(State.WalkingToCheckout);
                return;
            }

            if (shoppingCart.Count >= shoppingList.Count)
            {
                Debug.Log($"[CUSTOMER] Found all {shoppingCart.Count} items! Going to checkout.");
                TransitionToState(State.WalkingToCheckout);
                return;
            }

            if (currentBrowseTarget == null || currentShelfBrowseTimer <= 0f)
            {
                PickRandomUnvisitedShelf();
                currentShelfBrowseTimer = browseTimePerShelf;
            }

            if (currentBrowseTarget != null)
            {
                navAgent.SetDestination(currentBrowseTarget.position);

                if (navAgent.remainingDistance < stoppingDistance + 0.5f && !navAgent.hasPath)
                {
                    TryPickItemsFromShelf(currentBrowseShelf);
                }
            }
        }

        private void UpdateWalkingToCheckout()
        {
            if (checkoutTarget == null)
            {
                Debug.LogWarning("[CUSTOMER] No checkout target!");
                TransitionToState(State.Leaving);
                return;
            }

            navAgent.SetDestination(checkoutTarget.position);

            if (!navAgent.hasPath || navAgent.remainingDistance <= stoppingDistance)
            {
                Debug.Log($"[CUSTOMER] Arrived at checkout with {shoppingCart.Count} items");
                TransitionToState(State.WaitingForPlayerAtCheckout);
            }
        }

        private void UpdateWaitingForPlayer()
        {
            if (checkoutZone != null && checkoutZone.IsPlayerCheckingOut())
            {
                Debug.Log("[CUSTOMER] Player arrived!");
                TransitionToState(State.PlacingItems);
            }
            else if (browseTimer <= 0f)
            {
                Debug.Log("[CUSTOMER] Player took too long, leaving");
                TransitionToState(State.Leaving);
            }
        }

        private void UpdatePlacingItems()
        {
            if (!itemsPlacedOnCounter)
            {
                PlaceItemsOnCounter();
            }
            TransitionToState(State.Paying);
        }

        private void UpdatePaying()
        {
            if (shoppingCart.Count == 0)
            {
                TransitionToState(State.Leaving);
            }
        }

        private void UpdateLeaving()
        {
            if (checkoutTarget == null)
            {
                Destroy(gameObject);
                return;
            }

            Vector3 exitDirection = (transform.position - checkoutTarget.position).normalized;
            Vector3 exitTarget = transform.position + exitDirection * 20f;
            navAgent.SetDestination(exitTarget);

            if (Vector3.Distance(transform.position, checkoutTarget.position) > 20f)
            {
                Debug.Log("[CUSTOMER] Left store");
                Destroy(gameObject);
            }
        }

        private void PickRandomUnvisitedShelf()
        {
            ShelfComponent[] allShelves = FindObjectsByType<ShelfComponent>(FindObjectsSortMode.None);
            
            if (allShelves.Length == 0)
            {
                Debug.LogWarning("[CUSTOMER] No shelves found!");
                return;
            }

            ShelfComponent selectedShelf = null;
            for (int i = 0; i < allShelves.Length; i++)
            {
                if (!visitedShelves.Contains(allShelves[i]))
                {
                    selectedShelf = allShelves[i];
                    break;
                }
            }

            if (selectedShelf == null)
            {
                selectedShelf = allShelves[Random.Range(0, allShelves.Length)];
            }

            visitedShelves.Add(selectedShelf);
            
            // Position customer IN FRONT of shelf by using the browsing offset
            Vector3 shelfPos = selectedShelf.transform.position;
            Vector3 shelfForward = selectedShelf.transform.forward;
            float browsingDist = selectedShelf.GetBrowsingDistance();
            
            // Create a target slightly in front of the shelf
            Transform targetTransform = new GameObject("BrowseTarget").transform;
            targetTransform.position = shelfPos + (shelfForward * browsingDist);
            targetTransform.SetParent(selectedShelf.transform);
            
            currentBrowseTarget = targetTransform;
            currentBrowseShelf = selectedShelf;
            Debug.Log($"[CUSTOMER] Browsing shelf {visitedShelves.Count}");
        }

        private void TryPickItemsFromShelf(ShelfComponent shelf)
        {
            if (shelf == null)
                return;

            List<ItemInstance> shelfItems = shelf.GetAllItems();
            List<ItemInstance> itemsToPick = new();

            foreach (var needed in shoppingList)
            {
                if (shoppingCart.Exists(item => item.Definition.ItemId == needed.Definition.ItemId))
                    continue;

                foreach (var itemOnShelf in shelfItems)
                {
                    if (itemOnShelf.Definition.ItemId == needed.Definition.ItemId)
                    {
                        itemsToPick.Add(itemOnShelf);
                        Debug.Log($"[CUSTOMER] Needs {itemOnShelf.Definition.DisplayName}");
                        break;
                    }
                }
            }

            foreach (var item in itemsToPick)
            {
                if (shelf.TryRemoveItem(item))
                {
                    shoppingCart.Add(item);
                    DestroyShelfItemVisual(shelf, item);
                    Debug.Log($"[CUSTOMER] Picked up {item.Definition.DisplayName}");
                }
            }

            if (itemsToPick.Count > 0)
            {
                Debug.Log($"[CUSTOMER] Found {itemsToPick.Count} items. Cart: {shoppingCart.Count}");
            }
        }

        private void DestroyShelfItemVisual(ShelfComponent shelf, ItemInstance item)
        {
            ItemPickup[] allPickups = shelf.GetComponentsInChildren<ItemPickup>();
            foreach (var pickup in allPickups)
            {
                if (pickup.itemInstance == item)
                {
                    Debug.Log($"[CUSTOMER] Removing visual for {item.Definition.DisplayName} from shelf");
                    Destroy(pickup.gameObject);
                    return;
                }
            }
        }

        private void PlaceItemsOnCounter()
        {
            if (itemsPlacedOnCounter || shoppingCart.Count == 0)
                return;

            if (checkoutZone == null)
                return;

            CustomerItemDropZone dropZone = checkoutZone.GetComponentInChildren<CustomerItemDropZone>();
            Transform dropPoint = (dropZone != null) ? dropZone.transform : checkoutZone.transform;

            Debug.Log($"[CUSTOMER] Placing {shoppingCart.Count} items on counter");

            foreach (var item in shoppingCart)
            {
                Vector3 spawnPos = dropPoint.position + Vector3.up * itemPlacementHeight + Random.insideUnitSphere * 0.1f;
                
                GameObject itemVisual = Instantiate(
                    item.Definition.WorldPrefab,
                    spawnPos,
                    Quaternion.identity
                );

                ItemPickup pickup = itemVisual.AddComponent<ItemPickup>();
                pickup.itemInstance = item;

                Rigidbody itemRb = itemVisual.AddComponent<Rigidbody>();
                itemRb.isKinematic = true;  // CHANGED: Keep items still on counter
                itemRb.useGravity = false;
                itemRb.mass = 0.3f;

                if (itemVisual.GetComponent<Collider>() == null)
                {
                    MeshCollider col = itemVisual.AddComponent<MeshCollider>();
                    col.convex = true;
                }

                itemVisual.layer = LayerMask.NameToLayer("Item");
            }

            itemsPlacedOnCounter = true;
        }

        private void TransitionToState(State newState)
        {
            currentState = newState;
            
            if (newState == State.Browsing)
            {
                browseTimer = maxTotalBrowseTime;
            }
            else if (newState == State.WaitingForPlayerAtCheckout)
            {
                browseTimer = 30f;
            }

            Debug.Log($"[CUSTOMER] → {newState} | Cart: {shoppingCart.Count}/{shoppingList.Count}");
        }

        private void FindCheckoutAndQueue()
        {
            checkoutZone = FindFirstObjectByType<CheckoutZone>();
            
            // Customer uses DIFFERENT position than player
            checkoutTarget = checkoutZone != null ? checkoutZone.GetCustomerQueuePosition() : null;
            
            storeArea = FindFirstObjectByType<StoreArea>();
            customerQueue = FindFirstObjectByType<CustomerQueue>();

            if (checkoutZone == null)
                Debug.LogError("[CUSTOMER] No CheckoutZone found!");
            if (checkoutTarget == null)
                Debug.LogError("[CUSTOMER] No customer queue position found!");
            if (storeArea == null)
                Debug.LogWarning("[CUSTOMER] No StoreArea found!");
        }

        public void SetInStoreArea(bool inStore)
        {
            inStoreArea = inStore;
        }

        public List<ItemInstance> GetShoppingCart() => shoppingCart;
        public void RemoveItemFromCart(ItemInstance item) => shoppingCart.Remove(item);
    }
}