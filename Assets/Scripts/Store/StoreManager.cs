using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using Unity.AI.Navigation;
using UnityEngine.SceneManagement;
using AsakuShop.Core;
using AsakuShop.Storage;
using Codice.Client.Common.GameUI;

namespace AsakuShop.Store
{
    // Central manager for the convenience store. Tracks open/closed state,
    // registered shelves and checkout counters, and spawns pedestrian/customer NPCs.
    // NavMesh is baked in the Editor — this class never calls BuildNavMesh() at runtime.
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
        private readonly List<ShelfComponent> _registeredShelves = new();
        public IReadOnlyList<ShelfComponent> RegisteredShelves => _registeredShelves;
        public void RegisterShelf(ShelfComponent shelf)
        {
            if (shelf == null || _registeredShelves.Contains(shelf)) return;
            _registeredShelves.Add(shelf);
            Debug.Log($"[StoreManager] Shelf registered: {shelf.name} ({_registeredShelves.Count} total)");
        }
        public void UnregisterShelf(ShelfComponent shelf)
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
        private NavMeshSurface navMeshSurface;
        public void UpdateNavMeshSurface() => navMeshSurface.BuildNavMesh();
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

            navMeshSurface = GetComponent<NavMeshSurface>();
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
            spawnCustomerCoroutine = StartCoroutine(SpawnCustomer());
        }
#endregion


#region Customer Coroutine
        private IEnumerator SpawnCustomer()
        {
            while (true)
            {
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
        public void ValidateShelvingUnit(ShelfComponent shelf)
        {
            if (shelf == null) return;

            // if (shelf is not WallMountableShelf)
            // {
            //     //Free standing shelf logic
            //     return;
            // }
            //else
            
            Vector3 rayOrigin = shelf.transform.position + shelf.transform.forward * 0.1f; // Slightly in front of the shelf
            Vector3 rayDirection = -shelf.transform.forward; // Cast backwards
            if (Physics.Raycast(rayOrigin, rayDirection, out RaycastHit hit, 1f))
            {
                if (hit.collider.gameObject.CompareTag("Wall"))
                {
                    if (IsWithinStore(shelf.transform.position))
                    {
                        RegisterShelf(shelf);
                        Debug.Log($"[StoreManager] Shelf '{shelf.name}' validated and registered.");
                        return;
                    }
                    else
                    {
                        Debug.LogWarning($"[StoreManager] Shelf '{shelf.name}' is facing a wall but is not properly aligned within store bounds. hitPoint={hit.point}, shelfPos={shelf.transform.position}");
                    }
                }
            }

            Debug.LogWarning($"[StoreManager] Shelf '{shelf.name}' failed validation and will not be registered. Ensure it is properly aligned with a wall and tagged 'Wall'.");
        }

        // Returns all registered shelves that currently have at least one item.
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
        public ShelfComponent GetRandomShelf()
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
