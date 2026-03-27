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
#region Singleton, Bootstrap & References
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

        [Header("Store Bounds (drawn as yellow gizmo)")]
        [SerializeField] private Bounds storeBounds;

        [Header("Customer Spawning")]
        [SerializeField] private List<Transform> spawnPoints = new List<Transform>();
        [SerializeField] private List<GameObject> customerPrefabs = new List<GameObject>();
        [SerializeField] private int maxShoppers = 5;
        [SerializeField] private int maxNumberOfPedestriansOnMap = 150;
        [SerializeField] private float minSpawnInterval = 0.1f;
        [SerializeField] private float maxSpawnInterval = 1f;

        [Header("Pedestrian Wandering")]
        [Tooltip("Optional waypoints pedestrians roam between. If empty, spawn points are used as a fallback.")]
        [SerializeField] private List<Transform> wanderPoints = new List<Transform>();

        public bool IsOpen { get; private set; }

        private readonly List<ShelfComponent> _registeredShelves = new();
        public IReadOnlyList<ShelfComponent> RegisteredShelves => _registeredShelves;

        private List<CheckoutCounter> checkoutCounters = new List<CheckoutCounter>();

        private List<ICheckoutCustomer> activeShoppers = new List<ICheckoutCustomer>();
#endregion


#region Unity Lifecycle
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            if (customerPrefabs.Count == 0 && spawnPoints.Count == 0)
            {
                LoadConfigurationFromResources();
            }
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
            StartCoroutine(DeferredStartup());
        }

        private IEnumerator DeferredStartup()
        {
            yield return null; // Wait one frame

            if (customerPrefabs.Count == 0 && spawnPoints.Count == 0)
            {
                LoadConfigurationFromResources();
            }

            if (customerPrefabs.Count > 0 && spawnPoints.Count > 0)
                StartCoroutine(SpawnCustomers());
            else
                Debug.LogWarning("[StoreManager] No customer prefabs or spawn points assigned. Spawning disabled.");
        }
#endregion


#region Configuration & Initialization
        public void ConfigureStore(Bounds bounds, List<Transform> spawnPts, 
        List<GameObject> prefabs, List<Transform> wander, int maxShoppers,
        float minSpawn, float maxSpawn)
            {
                storeBounds = bounds;
                spawnPoints = spawnPts;
                customerPrefabs = prefabs;
                wanderPoints = wander;
                this.maxShoppers = maxShoppers;
                minSpawnInterval = minSpawn;
                maxSpawnInterval = maxSpawn;
            }

        // Loads customer prefabs from Resources and finds spawn/wander points by tag in the scene.
        public void LoadConfigurationFromResources(string prefabsPath = "Store/Prefabs", 
            string spawnPointTag = "SpawnPoint", string wanderPointTag = "WanderPoint",
            string boundsObjectName = "StoreBounds")
        {
            // Load prefabs from Resources folder
            var prefabs = Resources.LoadAll<GameObject>(prefabsPath);
            customerPrefabs = new List<GameObject>(prefabs);
            //Debug.Log($"[StoreManager] Loaded {customerPrefabs.Count} customer prefabs from Resources/{prefabsPath}");

            // Find spawn points in scene by tag
            var spawnGOs = GameObject.FindGameObjectsWithTag(spawnPointTag);
            spawnPoints = new List<Transform>();
            foreach (var go in spawnGOs)
                spawnPoints.Add(go.transform);
            //Debug.Log($"[StoreManager] Found {spawnPoints.Count} spawn points with tag '{spawnPointTag}'");
            // After loading spawn points, verify they're on the NavMesh
            var validSpawns = new List<Transform>();
            foreach (var spawnPt in spawnPoints)
            {
                if (NavMesh.SamplePosition(spawnPt.position, out NavMeshHit hit, 2f, NavMesh.AllAreas))
                    validSpawns.Add(spawnPt);
                else
                    Debug.LogWarning($"Spawn point '{spawnPt.name}' is not on NavMesh!");
            }
            spawnPoints = validSpawns;
            //Debug.Log($"[StoreManager] {spawnPoints.Count} valid spawn points on NavMesh");

            // Find wander points in scene by tag
            var wanderGOs = GameObject.FindGameObjectsWithTag(wanderPointTag);
            wanderPoints = new List<Transform>();
            foreach (var go in wanderGOs)
                wanderPoints.Add(go.transform);
            //Debug.Log($"[StoreManager] Found {wanderPoints.Count} wander points with tag '{wanderPointTag}'");

            // Find store bounds GameObject and extract its Collider bounds
            var boundsObj = GameObject.Find(boundsObjectName);
            if (boundsObj != null && boundsObj.TryGetComponent<BoxCollider>(out var bc))
            {
                storeBounds = bc.bounds;
                //Debug.Log($"[StoreManager] Loaded bounds from '{boundsObjectName}'");
            }
        }
#endregion



        // Clears scene-object references when a scene is unloaded so DontDestroyOnLoad
        // does not hold stale (destroyed) MonoBehaviour pointers.
        private void OnSceneUnloaded(Scene scene)
        {
            _registeredShelves.Clear();
            checkoutCounters.Clear();
            activeShoppers.Clear();
            _activePedestrianCount = 0;
        }

        // ── Open / Close ─────────────────────────────────────────────────────

        //Toggles the store open/closed state and fires the appropriate CoreEvent.
        public void ToggleOpen()
        {
            IsOpen = !IsOpen;

            if (IsOpen)
            {
                CoreEvents.RaiseStoreOpened();
                Debug.Log("[StoreManager] Store is now OPEN.");
            }
            else
            {
                CoreEvents.RaiseStoreClosed();
                // Ask all active shoppers to leave
                foreach (var shopper in activeShoppers.ToList())
                    (shopper as IAskToLeave)?.AskToLeave();
                Debug.Log("[StoreManager] Store is now CLOSED. Asked all shoppers to leave.");
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
            
            Vector3 spawnPos = spawnPoints[Random.Range(0, spawnPoints.Count)].position;
            
            if (NavMesh.SamplePosition(spawnPos, out NavMeshHit hit, 5f, NavMesh.AllAreas))
            {
                //Debug.Log($"[StoreManager] Valid exit point: original={spawnPos}, snapped={hit.position}");
                return hit.position;
            }
            
            Debug.LogWarning("[StoreManager] Exit point is off NavMesh!");
            return transform.position;
        }

        public Vector3 GetEntryPoint()
        {
            if (storeBounds.size != Vector3.zero)
            {
                if (NavMesh.SamplePosition(storeBounds.center, out NavMeshHit hit, storeBounds.extents.magnitude, NavMesh.AllAreas))
                {
                    Debug.Log($"[StoreManager] Entry point from StoreBounds center: {hit.position}");
                    return hit.position;
                }

                Debug.LogWarning($"[StoreManager] StoreBounds exists but no NavMesh point was found near its center. center={storeBounds.center}, extents={storeBounds.extents}");
            }

            if (wanderPoints.Count > 0)
            {
                Vector3 wp = wanderPoints[Random.Range(0, wanderPoints.Count)].position;
                if (NavMesh.SamplePosition(wp, out NavMeshHit wHit, 5f, NavMesh.AllAreas))
                {
                    Debug.Log($"[StoreManager] Entry point from WanderPoint: {wHit.position}");
                    return wHit.position;
                }

                Debug.LogWarning($"[StoreManager] WanderPoint was chosen for entry but was not on NavMesh. point={wp}");
            }

            Debug.LogWarning("[StoreManager] No valid store entry point found. Falling back to exit point.");
            return GetExitPoint();
        }

        // Returns a random wander waypoint for non-shopping pedestrians.
        // Falls back to a spawn point if no dedicated wander points are configured.
        public Vector3 GetWanderPoint()
        {
        List<Transform> pool = wanderPoints.Count > 0 ? wanderPoints : spawnPoints;
        if (pool.Count == 0) return transform.position;
        Vector3 wanderPos = pool[Random.Range(0, pool.Count)].position;

        if (NavMesh.SamplePosition(wanderPos, out NavMeshHit hit, 5f, NavMesh.AllAreas))
            return hit.position;

        Debug.LogWarning("[StoreManager] Wander point is off NavMesh!");
        return transform.position;
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

            private int _activePedestrianCount = 0;
            public void RegisterPedestrian()  => _activePedestrianCount++;
            public void UnregisterPedestrian() => _activePedestrianCount--;
        private void SpawnOnePedestrian()
        {
            if (customerPrefabs.Count == 0 || spawnPoints.Count == 0) return;

            if (_activePedestrianCount >= maxNumberOfPedestriansOnMap)
                return;

            GameObject prefab = customerPrefabs[Random.Range(0, customerPrefabs.Count)];
            Transform spawnPt = spawnPoints[Random.Range(0, spawnPoints.Count)];

            Vector3 spawnPos = spawnPt.position;
            
            if (!NavMesh.SamplePosition(spawnPos, out NavMeshHit hit, 5f, NavMesh.AllAreas))
            {
                Debug.LogWarning($"Spawn area invalid, finding nearest mesh point...");
                return;
            }
            spawnPos = hit.position;
            
            GameObject go = Instantiate(prefab, spawnPos, spawnPt.rotation);

            bool wantsToShop = IsOpen
                && activeShoppers.Count < maxShoppers;

            var customerComp = go.GetComponent<ICheckoutCustomer>();
            var pedestrian = go.GetComponent<IPedestrianCustomer>();
            if (pedestrian != null)
                pedestrian.SetWantsToShop(wantsToShop);

            if (wantsToShop && customerComp != null)
                RegisterShopper(customerComp);
        }



        #if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            // Spawn points - cyan
            var spawnGOs = GameObject.FindGameObjectsWithTag("SpawnPoint");
            foreach (var sp in spawnGOs)
            {
                bool isValid = NavMesh.SamplePosition(sp.transform.position, out _, 2f, NavMesh.AllAreas);
                Gizmos.color = isValid ? Color.cyan : Color.red;
                Gizmos.DrawWireSphere(sp.transform.position, 0.3f);
            }

            // Wander points - green
            var wanderGOs = GameObject.FindGameObjectsWithTag("WanderPoint");
            foreach (var wp in wanderGOs)
            {
                bool isValid = NavMesh.SamplePosition(wp.transform.position, out _, 2f, NavMesh.AllAreas);
                Gizmos.color = isValid ? Color.green : Color.red;
                Gizmos.DrawWireSphere(wp.transform.position, 0.4f);
            }

            // Store bounds - yellow
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(storeBounds.center, storeBounds.size);
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
