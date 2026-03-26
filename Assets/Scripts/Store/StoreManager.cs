using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using AsakuShop.Core;
using AsakuShop.Storage;

namespace AsakuShop.Store
{
    // Central manager for the convenience store. Tracks open/closed state,
    // registered shelves and checkout counters, and spawns pedestrian/customer NPCs.
    // NavMesh is baked in the Editor — this class never calls BuildNavMesh() at runtime.
    public class StoreManager : MonoBehaviour
    {
        public static StoreManager Instance { get; private set; }

        // Registers the bootstrap factory with GameBootstrapper before any scene loads.
        // This avoids a cyclic assembly reference between AsakuShop.Core and AsakuShop.Store.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void RegisterBootstrapper()
        {
            GameBootstrapper.RegisterBootstrapper(() =>
            {
                if (Instance != null) return;
                GameObject go = new GameObject("[StoreManager]");
                go.AddComponent<StoreManager>();
                // DontDestroyOnLoad is called inside StoreManager.Awake().
            });
        }

        // ── Inspector ────────────────────────────────────────────────────────
        [Header("Store Bounds (drawn as yellow gizmo)")]
        [SerializeField] private Bounds storeBounds;

        [Header("Customer Spawning")]
        [SerializeField] private List<Transform> spawnPoints = new List<Transform>();
        [SerializeField] private List<GameObject> customerPrefabs = new List<GameObject>();
        [SerializeField] private int maxShoppers = 10;
        [SerializeField] private float minSpawnInterval = 5f;
        [SerializeField] private float maxSpawnInterval = 15f;

        [Header("Pedestrian Wandering")]
        [Tooltip("Optional waypoints pedestrians roam between. If empty, spawn points are used as a fallback.")]
        [SerializeField] private List<Transform> wanderPoints = new List<Transform>();

        // ── State ────────────────────────────────────────────────────────────
        public bool IsOpen { get; private set; }

        // ── Shelf registry (single source of truth) ──────────────────────────
        private readonly List<ShelfComponent> _registeredShelves = new();
        public IReadOnlyList<ShelfComponent> RegisteredShelves => _registeredShelves;

        // ── Counters ─────────────────────────────────────────────────────────
        private List<CheckoutCounter> checkoutCounters = new List<CheckoutCounter>();

        // ── Active shoppers ───────────────────────────────────────────────────
        private List<ICheckoutCustomer> activeShoppers = new List<ICheckoutCustomer>();

        // ── Lifecycle ────────────────────────────────────────────────────────
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnEnable()
        {
            SceneManager.sceneUnloaded += OnSceneUnloaded;
        }

        private void OnDisable()
        {
            SceneManager.sceneUnloaded -= OnSceneUnloaded;
        }

        private void Start()
        {
            if (customerPrefabs.Count > 0 && spawnPoints.Count > 0)
                StartCoroutine(SpawnCustomers());
            else
                Debug.LogWarning("[StoreManager] No customer prefabs or spawn points assigned. Spawning disabled.");
        }

        // Clears scene-object references when a scene is unloaded so DontDestroyOnLoad
        // does not hold stale (destroyed) MonoBehaviour pointers.
        private void OnSceneUnloaded(Scene scene)
        {
            _registeredShelves.Clear();
            checkoutCounters.Clear();
            activeShoppers.Clear();
        }

        // ── Open / Close ─────────────────────────────────────────────────────

        //Toggles the store open/closed state and fires the appropriate CoreEvent.
        public void ToggleOpen()
        {
            IsOpen = !IsOpen;

            if (IsOpen)
                CoreEvents.RaiseStoreOpened();
            else
            {
                CoreEvents.RaiseStoreClosed();
                // Ask all active shoppers to leave
                foreach (var shopper in activeShoppers.ToList())
                    (shopper as IAskToLeave)?.AskToLeave();
            }
        }

        // ── Bounds ───────────────────────────────────────────────────────────

        // The store's world-space bounds used for shelf registration checks.
        public Bounds StoreBounds => storeBounds;

        //Returns true if the world-space position is within the store bounds.
        public bool IsWithinStore(Vector3 worldPosition) => storeBounds.Contains(worldPosition);

        // ── Spawn / Wander points ─────────────────────────────────────────────

        //Returns a random spawn point position (used for exits and pedestrian approach paths).
        public Vector3 GetExitPoint()
        {
            if (spawnPoints.Count == 0) return transform.position;
            return spawnPoints[Random.Range(0, spawnPoints.Count)].position;
        }

        // Returns a random wander waypoint for non-shopping pedestrians.
        // Falls back to a spawn point if no dedicated wander points are configured.
        public Vector3 GetWanderPoint()
        {
            List<Transform> pool = wanderPoints.Count > 0 ? wanderPoints : spawnPoints;
            if (pool.Count == 0) return transform.position;
            return pool[Random.Range(0, pool.Count)].position;
        }

        // ── Shelf registry ───────────────────────────────────────────────────
        // Called by PlayerHands after a successful wall-mount placement confirms the shelf
        // is inside StoreBounds. Registers it in the typed list so customers
        // can discover it via GetStockedShelves.
        public void RegisterShelf(ShelfComponent shelf)
        {
            if (shelf == null || _registeredShelves.Contains(shelf)) return;
            _registeredShelves.Add(shelf);
            Debug.Log($"[StoreManager] Shelf registered: {shelf.name} ({_registeredShelves.Count} total)");
        }

        // Called by PlayerHands before the shelf is picked up off the wall.
        // Removes the shelf from the typed list and the browsing pool.
        public void UnregisterShelf(ShelfComponent shelf)
        {
            if (_registeredShelves.Remove(shelf))
                Debug.Log($"[StoreManager] Shelf unregistered: {shelf.name} ({_registeredShelves.Count} remaining)");
        }

        // MonoBehaviour overloads — used by the Customer AI claim/return cycle.
        // Delegates to the typed overloads so the two registries never diverge.
        public void RegisterShelf(MonoBehaviour shelf)
        {
            if (shelf is ShelfComponent sc) RegisterShelf(sc);
        }

        public void UnregisterShelf(MonoBehaviour shelf)
        {
            if (shelf is ShelfComponent sc) UnregisterShelf(sc);
        }

        // Returns all registered wall-mounted shelves that currently have at least one item.
        // Customers use this to find shelves worth browsing.
        public List<ShelfComponent> GetStockedShelves()
        {
            var result = new List<ShelfComponent>();
            foreach (var shelf in _registeredShelves)
                if (shelf != null && shelf.Items.Count > 0)
                    result.Add(shelf);
            return result;
        }

        // Returns a random registered shelf from the browsing pool, or null if none are registered.
        // Skips any destroyed/null entries (can occur if a scene is unloaded mid-frame).
        public MonoBehaviour GetRandomShelf()
        {
            // Remove any destroyed references before sampling.
            _registeredShelves.RemoveAll(s => s == null);
            if (_registeredShelves.Count == 0) return null;
            return _registeredShelves[Random.Range(0, _registeredShelves.Count)];
        }

        // ── Counter registry ──────────────────────────────────────────────────

        public void RegisterCounter(CheckoutCounter counter)
        {
            if (counter != null && !checkoutCounters.Contains(counter))
                checkoutCounters.Add(counter);
        }

        //Returns the counter with the shortest customer queue.
        public CheckoutCounter GetShortestQueueCounter()
        {
            checkoutCounters.RemoveAll(c => c == null);
            if (checkoutCounters.Count == 0) return null;
            return checkoutCounters.OrderBy(c => c.LiningCustomers.Count).First();
        }

        // ── Shopper tracking ──────────────────────────────────────────────────

        public void RegisterShopper(ICheckoutCustomer shopper)
        {
            if (!activeShoppers.Contains(shopper)) activeShoppers.Add(shopper);
        }

        public void UnregisterShopper(ICheckoutCustomer shopper)
        {
            activeShoppers.Remove(shopper);
        }

        // ── Spawning ──────────────────────────────────────────────────────────

        private IEnumerator SpawnCustomers()
        {
            while (true)
            {
                float wait = Random.Range(minSpawnInterval, maxSpawnInterval);
                yield return new WaitForSeconds(wait);
                SpawnOnePedestrian();
            }
        }

        private void SpawnOnePedestrian()
        {
            if (customerPrefabs.Count == 0 || spawnPoints.Count == 0) return;

            // Pick a random prefab and a random spawn point
            GameObject prefab    = customerPrefabs[Random.Range(0, customerPrefabs.Count)];
            Transform  spawnPt   = spawnPoints[Random.Range(0, spawnPoints.Count)];

            GameObject go = Instantiate(prefab, spawnPt.position, spawnPt.rotation);

            // If the customer wants to shop: only if store is open, below max shoppers, and there are shelves
            bool wantsToShop = IsOpen
                && activeShoppers.Count < maxShoppers
                && _registeredShelves.Count > 0;

            // Set wantsToShop via interface or reflection — Customer component sets it publicly
            var customerComp = go.GetComponent<ICheckoutCustomer>();

            // Use reflection-free duck-typing: look for a "wantsToShop" public bool field via the IWantsToShop interface
            var pedestrian = go.GetComponent<IPedestrianCustomer>();
            if (pedestrian != null)
                pedestrian.SetWantsToShop(wantsToShop);

            if (wantsToShop && customerComp != null)
                RegisterShopper(customerComp);
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            // Draw store bounds as a yellow wire cube
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(storeBounds.center, storeBounds.size);

            // Draw spawn points as cyan spheres
            Gizmos.color = Color.cyan;
            foreach (var sp in spawnPoints)
            {
                if (sp != null)
                    Gizmos.DrawWireSphere(sp.position, 0.4f);
            }

            // Draw wander points as green spheres
            Gizmos.color = Color.green;
            foreach (var wp in wanderPoints)
            {
                if (wp != null)
                    Gizmos.DrawWireSphere(wp.position, 0.4f);
            }
        }
#endif
    }

    // ── Helper interfaces (thin, in same file for simplicity) ─────────────────

    //Allows StoreManager to call AskToLeave() on Customers without a direct type reference.
    public interface IAskToLeave
    {
        void AskToLeave();
    }

    //Allows StoreManager to set the wantsToShop flag on Customer without a direct type reference.
    public interface IPedestrianCustomer
    {
        void SetWantsToShop(bool value);
    }
}
