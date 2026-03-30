using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using Unity.AI.Navigation;
using UnityEngine.SceneManagement;
using AsakuShop.Core;
using AsakuShop.Storage;

namespace AsakuShop.Store
{
    // Central manager for the convenience store. Tracks open/closed state,
    // registered shelves and checkout counters, and spawns pedestrian/customer NPCs.
<<<<<<< Updated upstream
    // Call UpdateNavMeshSurface() after the player moves furniture to rebake the NavMesh at runtime.
=======
>>>>>>> Stashed changes
    public class StoreManager : MonoBehaviour
    {
#region Singleton and Initialization
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
#endregion


#region Configurable fields and properties
        [Header("Store Bounds (drawn as yellow gizmo)")]
        [SerializeField] private Bounds storeBounds;
        public Bounds StoreBounds => storeBounds;

        
        [Header("Delivery Point")]
        [SerializeField, Tooltip("The point where deliveries are made (e.g., Products, Furnitures).")]
        private Transform deliveryPoint;
        public Transform DeliveryPoint => deliveryPoint;


        [Header("Customer Spawning")]
        [SerializeField] private List<Transform> spawnPoints = new List<Transform>();
        [SerializeField] private List<GameObject> customerPrefabs = new List<GameObject>();
        private int maxShoppers => GameConfig.Instance.BaseMaxCustomers;
        private Coroutine spawnCustomerCoroutine;
        private List<ICheckoutCustomer> customers = new List<ICheckoutCustomer>();


        [Header("Shelves")]
        private readonly List<ShelfContainer> _registeredShelves = new();
        public IReadOnlyList<ShelfContainer> RegisteredShelves => _registeredShelves;
        public void RegisterShelf(ShelfContainer shelf)
        {
            if (shelf == null || _registeredShelves.Contains(shelf)) return;
            _registeredShelves.Add(shelf);
            Debug.Log($"[StoreManager] Shelf registered: {shelf.name} ({_registeredShelves.Count} total)");
        }
        public void UnregisterShelf(ShelfContainer shelf)
        {
            if (_registeredShelves.Remove(shelf))
                Debug.Log($"[StoreManager] Shelf unregistered: {shelf.name} ({_registeredShelves.Count} remaining)");
        }


        [Header("Checkout Counters")]
        private List<CheckoutCounter> checkoutCounters = new List<CheckoutCounter>();
        public void RegisterCounter(CheckoutCounter counter)
        {
            if (counter != null && !checkoutCounters.Contains(counter))
                checkoutCounters.Add(counter);
        }


        [Header("Store State")]
        private bool signPressedThisFrame;
        private bool isOpen;
        public bool IsOpen
        {
            get => isOpen;
            private set
            {
                if (isOpen == value) return;
                isOpen = value;
            }
        }


        [Header("NavMesh")]
<<<<<<< Updated upstream
        private NavMeshSurface navMeshSurface;

        // Rebakes the NavMesh after furniture has been moved. The NavMeshSurface lives on a
        // scene GameObject, so we look it up lazily rather than via GetComponent (this
        // StoreManager is created as a bare procedural GameObject and will never have one).
        public void UpdateNavMeshSurface()
        {
            if (navMeshSurface == null)
                navMeshSurface = FindAnyObjectByType<NavMeshSurface>();

            if (navMeshSurface != null)
                navMeshSurface.BuildNavMesh();
            else
                Debug.LogWarning("[StoreManager] UpdateNavMeshSurface: no NavMeshSurface found in the current scene.");
=======
        private AsyncOperation _navMeshUpdateOperation;
        private bool _rebakePending;

        private NavMeshSurface _navMeshSurface;
        private NavMeshSurface NavMeshSurface
        {
            get
            {
                if (_navMeshSurface == null)
                    _navMeshSurface = FindFirstObjectByType<NavMeshSurface>();
                return _navMeshSurface;
            }
>>>>>>> Stashed changes
        }
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

<<<<<<< Updated upstream
            navMeshSurface = FindAnyObjectByType<NavMeshSurface>();
=======
            // Prefabs load from Resources — always available
            customerPrefabs = Resources.LoadAll<GameObject>("Store/Prefabs/Customers").ToList();
>>>>>>> Stashed changes
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded   += OnSceneLoaded;
            SceneManager.sceneUnloaded += OnSceneUnloaded;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded   -= OnSceneLoaded;
            SceneManager.sceneUnloaded -= OnSceneUnloaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // Skip the bootstrap scene
            if (scene.name == "Bootstrap") return;

            // Now scene objects exist — find spawn points
            spawnPoints = GameObject.FindGameObjectsWithTag("SpawnPoint")
                .Select(go => go.transform).ToList();
            Debug.Log($"[StoreManager] Scene '{scene.name}' loaded — found {spawnPoints.Count} spawn point(s).");

            // Bake initial NavMesh
            _navMeshSurface = null; // Clear cached ref so it finds the new scene's surface
            if (NavMeshSurface != null)
            {
                NavMeshSurface.BuildNavMesh();
                Debug.Log("[StoreManager] Initial NavMesh baked.");
            }
            else
            {
                Debug.LogError("[StoreManager] No NavMeshSurface found in scene!");
            }

            // Start the customer spawn loop
            if (spawnCustomerCoroutine != null)
                StopCoroutine(spawnCustomerCoroutine);
            spawnCustomerCoroutine = StartCoroutine(SpawnCustomer());
        }
#endregion


#region Customer Coroutine
        private IEnumerator SpawnCustomer()
        {
            while (true)
            {
                if (NavMeshSurface == null) yield return null;
                float waitTime = GameConfig.Instance.GetRandomSpawnTime;
                yield return new WaitForSeconds(waitTime);

                if (isOpen && customers.Count < maxShoppers)
                {
                    int randomCustomerIndex = Random.Range(0, customerPrefabs.Count);
                    GameObject customerPrefab = customerPrefabs[randomCustomerIndex];

                    int randomSpawnPointIndex = Random.Range(0, spawnPoints.Count);
                    Transform spawnPoint = spawnPoints[randomSpawnPointIndex];

                    GameObject customerObj = Instantiate(customerPrefab, spawnPoint.position, spawnPoint.rotation);
                    var checkoutCustomer = customerObj.GetComponent<ICheckoutCustomer>();
                    customers.Add(checkoutCustomer);
                    checkoutCustomer.OnLeave += () => customers.Remove(checkoutCustomer);
                }
            }
        }
#endregion


#region Store State Management
        //Toggles the store open/closed state and fires the appropriate CoreEvent.
        public void ToggleOpen()
        {
            if (!signPressedThisFrame)
            {
                signPressedThisFrame = true;
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
                    foreach (var shopper in customers.ToList())
                        (shopper as IAskToLeave)?.AskToLeave();
                    Debug.Log("[StoreManager] Store is now CLOSED. Asked all shoppers to leave.");
                }
            }
            signPressedThisFrame = false;
        }
#endregion


#region Shelving Validation
        public void ValidateShelvingUnit(ShelfContainer shelf)
        {
            if (shelf == null) return;

            if (IsWithinStore(shelf.transform.position))
            {
                RegisterShelf(shelf);
                RequestNavMeshUpdate();
            }
            else
            {
                Debug.LogWarning($"[StoreManager] Shelf '{shelf.name}' is outside store bounds.");
            }
        }


        // Returns all registered shelves that currently have at least one item.
        // Customers use this to find shelves worth browsing.
        public List<ShelfContainer> GetStockedShelves()
        {
            var result = new List<ShelfContainer>();
            foreach (var shelf in _registeredShelves)
                if (shelf != null && shelf.PeekItem() != null)
                    result.Add(shelf);
            return result;
        }

        // Returns a random registered shelf from the browsing pool, or null if none are registered.
        // Skips any destroyed/null entries (can occur if a scene is unloaded mid-frame).
        public ShelfContainer GetRandomShelf()
        {
            // Remove any destroyed references before sampling.
            _registeredShelves.RemoveAll(s => s == null);
            if (_registeredShelves.Count == 0) return null;
            return _registeredShelves[Random.Range(0, _registeredShelves.Count)];
        }
#endregion


#region  Checkout counter decision logic
        public CheckoutCounter GetCounterAtIndex(int index)
        {
            if (checkoutCounters == null || index < 0 || index >= checkoutCounters.Count)
                return null;

            return checkoutCounters[index];
        }

        public CheckoutCounter GetShortestQueueCounter()
        {
            return checkoutCounters
                .OrderBy(counter => counter.LiningCustomers.Count)
                .FirstOrDefault();
        }
#endregion


#region Navigation and Spawning
        //Returns a random spawn point position (used for exits and pedestrian approach paths).
        public Transform GetExitPoint()
        {
            if (spawnPoints == null || spawnPoints.Count == 0) return null;

            return spawnPoints[Random.Range(0, spawnPoints.Count)];
        }

        //NOTE: Customers call this when finished shopping to find a valid point to exit towards. 
        //Customers use a FindShelvingUnit method to find store.
#endregion

        private void OnSceneUnloaded(Scene scene)
        {
            _registeredShelves.Clear();
            checkoutCounters.Clear();
            customers.Clear();
        }



#region Helper Methods & Interfaces
    public bool IsWithinStore(Vector3 worldPosition) => storeBounds.Contains(worldPosition);

    public interface IAskToLeave
    {
        void AskToLeave();
    }
    public interface IPedestrianCustomer
    {
        void SetWantsToShop(bool value);
    }
#endregion

#region NavMesh Updates
        public void RequestNavMeshUpdate()
        {
            if (_rebakePending) return;
            _rebakePending = true;
            StartCoroutine(RebakeNextFrame());
        }
        private IEnumerator RebakeNextFrame()
        {
            // Wait one frame to batch multiple furniture changes together
            yield return null;
            _rebakePending = false;

            if (NavMeshSurface == null) yield break;

            _navMeshUpdateOperation = NavMeshSurface.UpdateNavMesh(NavMeshSurface.navMeshData);
            _navMeshUpdateOperation.completed += _ =>
            {
                Debug.Log("[StoreManager] NavMesh updated.");
            };
        }
#endregion


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

}
