using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using DG.Tweening;
using AsakuShop.Core;
using AsakuShop.Items;
using AsakuShop.Store;

namespace AsakuShop.Customers
{
    /// AI pedestrian/customer. Always spawns as a pedestrian wandering the world.
    /// If wantsToShop is true (set at spawn time), will enter the store and shop.
    /// Implements ICheckoutCustomer so CheckoutCounter can process this customer.
    /// Implements IAskToLeave + IPedestrianCustomer for StoreManager to control.
    [RequireComponent(typeof(Animator), typeof(NavMeshAgent))]
    public class Customer : MonoBehaviour,
        ICheckoutCustomer,
        IAskToLeave,
        IPedestrianCustomer
    {
        // ── Constants ─────────────────────────────────────────────────────────
        private const float WALK_SPEED              = 1.5f;
        private const float CONTINUE_SHOPPING_CHANCE = 0.5f;
        private const float STOPPING_DISTANCE       = 0.6f;
        private const float ARRIVED_THRESHOLD       = 0.8f;

        // ── Inspector ─────────────────────────────────────────────────────────
        [Header("References")]
        [SerializeField] private CustomerHandAttachments handAttachments;
        [SerializeField] private CustomerOverheadUI      overheadUI;

        [Header("Dialogue")]
        [SerializeField] private Dialogue notFoundDialogue;
        [SerializeField] private Dialogue overpricedDialogue;

        // ── Runtime state ─────────────────────────────────────────────────────
        public bool wantsToShop { get; private set; }

        // ICheckoutCustomer
        public List<ItemInstance> Inventory { get; private set; } = new List<ItemInstance>();

        private Animator      animator;
        private NavMeshAgent  agent;
        private MonoBehaviour targetShelf; // WallShelf — typed as MonoBehaviour to keep the assembly linear
        private bool          isLeaving;
        private bool          askedToLeave;
        private Coroutine     shoppingCoroutine;

        [Header("Debug")]
        [SerializeField] private bool debugEntryLogs = true;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            animator = GetComponent<Animator>();
            agent    = GetComponent<NavMeshAgent>();

            // Ensure the agent starts on the NavMesh using Warp (no pathfinding)
            if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 2f, NavMesh.AllAreas))
            {
                agent.Warp(hit.position);
            }

            agent.areaMask = NavMesh.AllAreas;

            agent.speed                  = WALK_SPEED;
            agent.angularSpeed           = 3600f;
            agent.acceleration           = 100f;
            agent.stoppingDistance       = STOPPING_DISTANCE;
            agent.obstacleAvoidanceType  = ObstacleAvoidanceType.NoObstacleAvoidance;
        }

        private void Start()
        {
            StoreManager.Instance?.RegisterPedestrian();
            StartCoroutine(PedestrianLoop());
        }

        private void Update()
        {
            CheckStoreDoors();
            UpdateAnimator();
        }

        // ── IPedestrianCustomer ───────────────────────────────────────────────

        public void SetWantsToShop(bool value) => wantsToShop = value;

        // ── IAskToLeave ───────────────────────────────────────────────────────

        public void AskToLeave()
        {
            askedToLeave = true;
            if (Inventory.Count == 0 && shoppingCoroutine != null)
            {
                StopCoroutine(shoppingCoroutine);
                shoppingCoroutine = null;
                StartCoroutine(Leave());
            }
        }

        // ── ICheckoutCustomer ─────────────────────────────────────────────────

        public void OnCheckoutComplete()
        {
            Inventory.Clear();
        }

        // ── Door detection ────────────────────────────────────────────────────

        private void CheckStoreDoors()
        {
            Vector3 origin    = transform.position + Vector3.up * 0.8f;
            Vector3 direction = transform.forward;

            if (Physics.Raycast(origin, direction, out RaycastHit hit, 1.5f))
            {
                var entranceDoor = hit.collider.GetComponent<EntranceDoor>();
                if (entranceDoor != null) entranceDoor.OpenIfClosed();

                var door = hit.collider.GetComponent<Door>();
                if (door != null) door.OpenIfClosed();
            }
        }

        // ── AI Loops ──────────────────────────────────────────────────────────

        private IEnumerator PedestrianLoop()
        {
            if (StoreManager.Instance == null)
            {
                LogEntry("No StoreManager instance found. Aborting.");
                yield break;
            }

            // If we don't want to shop, just wander indefinitely
            if (!wantsToShop)
            {
                //LogEntry("Spawned as pedestrian only. Will wander instead of entering the store.");
                yield return WanderForever();
                yield break;
            }

            LogEntry($"Trying to enter store. startPos={transform.position}, insideBounds={StoreManager.Instance.IsWithinStore(transform.position)}");

            while (!StoreManager.Instance.IsWithinStore(transform.position) && !askedToLeave)
            {
                Vector3 dest = StoreManager.Instance.GetEntryPoint();
                bool wasInside = StoreManager.Instance.IsWithinStore(transform.position);

                LogEntry($"Heading toward store entry. currentPos={transform.position}, entryTarget={dest}, insideBounds={wasInside}");
                agent.SetDestination(dest);

                yield return new WaitUntil(() =>
                    HasArrived() ||
                    StoreManager.Instance.IsWithinStore(transform.position) ||
                    askedToLeave);

                bool hasArrived = HasArrived();
                bool isInside = StoreManager.Instance.IsWithinStore(transform.position);

                LogEntry($"Entry step finished. currentPos={transform.position}, arrived={hasArrived}, insideBounds={isInside}, askedToLeave={askedToLeave}");

                if (isInside)
                    LogEntry("Entered store bounds.");

                if (hasArrived && !isInside)
                    LogEntry("Reached entry target but still outside store bounds. Proceeding anyway.");

                if (hasArrived)
                    break;

                if (!isInside)
                    yield return new WaitForSeconds(Random.Range(0.5f, 2f));
            }

            if (askedToLeave)
            {
                LogEntry("Asked to leave before shopping started.");
                yield return Leave();
                yield break;
            }

            LogEntry("Commencing shopping.");
            CoreEvents.RaiseCustomerEntered(this);
            overheadUI?.ShowStatus("🛒");
            StoreManager.Instance.RegisterShopper(this);

            shoppingCoroutine = StartCoroutine(ShoppingLoop());
            yield return shoppingCoroutine;
        }

        private IEnumerator WanderForever()
        {
            while (!isLeaving)
            {
                if (StoreManager.Instance == null) yield break;

                Vector3 dest = StoreManager.Instance.GetWanderPoint();
                agent.SetDestination(dest);
                yield return new WaitUntil(() => HasArrived());
                yield return new WaitForSeconds(Random.Range(2f, 6f));
            }
        }

        private IEnumerator ShoppingLoop()
        {
            bool keepShopping = true;

            while (keepShopping && !askedToLeave)
            {
                yield return FindShelf();

                if (targetShelf != null)
                    yield return BrowseShelf();

                keepShopping = Random.value < CONTINUE_SHOPPING_CHANCE;
            }

            if (askedToLeave)
            {
                yield return Leave();
                yield break;
            }

            if (Inventory.Count > 0)
                yield return Checkout();
            else
            {
                string line = notFoundDialogue != null
                    ? notFoundDialogue.GetRandomLine()
                    : "Nothing for me today...";
                overheadUI?.ShowDialog(line);
            }

            yield return Leave();
        }

        private IEnumerator FindShelf()
        {
            if (StoreManager.Instance == null) yield break;

            targetShelf = StoreManager.Instance.GetRandomShelf();
            if (targetShelf == null) yield break;

            // Claim the shelf so other customers don't pile on it
            StoreManager.Instance.UnregisterShelf(targetShelf);

            // TODO: WallShelf must expose a FrontPoint property (Vector3).
            // Replace the line below with: agent.SetDestination(((WallShelf)targetShelf).FrontPoint);
#pragma warning disable CS0168
            Vector3 destination;
            try
            {
                // Duck-type call: look for IShelfFrontPoint interface or use transform.position as fallback
                var frontPointProvider = targetShelf as IShelfFrontPoint;
                destination = frontPointProvider != null
                    ? frontPointProvider.FrontPoint
                    : targetShelf.transform.position + targetShelf.transform.forward * 0.8f;
            }
            catch
            {
                destination = targetShelf.transform.position;
            }
#pragma warning restore CS0168

            agent.SetDestination(destination);
            yield return new WaitUntil(() => HasArrived());
            yield return FaceTarget(targetShelf.transform);
        }

        private IEnumerator BrowseShelf()
        {
            if (targetShelf == null) yield break;

            // TODO: WallShelf must expose PeekItem() → ItemInstance and TakeItem() → ItemInstance
            // Replace the stub below once WallShelf has these methods.
            ItemInstance item = null;
            var itemProvider  = targetShelf as IShelfItemProvider;
            if (itemProvider != null)
                item = itemProvider.PeekItem();

            if (item == null)
            {
                // Shelf is empty — put it back and move on
                StoreManager.Instance.RegisterShelf(targetShelf);
                overheadUI?.ShowDialog("Hmm, nothing here.");
                yield break;
            }

            // Decide whether to buy
            if (IsWillingToBuy(item))
            {
                if (itemProvider != null)
                    itemProvider.TakeItem();

                Inventory.Add(item);
                animator.SetTrigger("GrabItem");
                overheadUI?.ShowStatus("✓");
                yield return new WaitForSeconds(0.8f);
            }
            else
            {
                string line = overpricedDialogue != null
                    ? overpricedDialogue.GetRandomLine().Replace("{item}", item.Definition.DisplayName)
                    : $"{item.Definition.DisplayName} is too expensive!";
                overheadUI?.ShowDialog(line);
                yield return new WaitForSeconds(1.5f);
            }

            // Return shelf to the pool
            StoreManager.Instance.RegisterShelf(targetShelf);
            targetShelf = null;
        }

        private IEnumerator Checkout()
        {
            if (StoreManager.Instance == null) yield break;

            var counter = StoreManager.Instance.GetShortestQueueCounter();
            if (counter == null)
            {
                Debug.LogWarning("[Customer] No checkout counter available.");
                yield break;
            }

            counter.LiningCustomers.Add(this);
            overheadUI?.ShowStatus("🧾");

            // Walk to queue position
            Vector3 queuePos = counter.GetQueuePosition(this, out Vector3 lookDir);
            agent.SetDestination(queuePos);
            yield return new WaitUntil(() => HasArrived());

            // Face the cashier stand
            if (lookDir != Vector3.zero)
                yield return FaceDirection(lookDir);

            // Wait for items to be placed and transaction to complete
            yield return counter.PlaceProducts(this);
            yield return new WaitUntil(() => counter.CurrentState == CheckoutCounter.State.Standby);
        }

        private IEnumerator Leave()
        {
            if (isLeaving) yield break;
            isLeaving = true;

            StoreManager.Instance?.UnregisterShopper(this);
            StoreManager.Instance?.UnregisterPedestrian(); 
            CoreEvents.RaiseCustomerLeft(this);

            overheadUI?.ShowDialog("Bye!", 1.5f);
            yield return new WaitForSeconds(1.5f);
            overheadUI?.Hide();

            if (StoreManager.Instance != null)
            {
                agent.SetDestination(StoreManager.Instance.GetExitPoint());
                float timeout = 20f;
                float elapsed = 0f;
                while (!HasExitedStore() && elapsed < timeout)
                {
                    elapsed += Time.deltaTime;
                    yield return null;
                }
            }

            yield return new WaitForEndOfFrame();
            Destroy(gameObject);
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private bool IsWillingToBuy(ItemInstance item)
        {
            // Tolerance follows a curve: customers rarely pay way over market price,
            // but occasionally accept a slight markup.
            float tolerance     = 1f + Mathf.Pow(Random.value, 2f); // range: [1.0, 2.0], weighted low
            int   effectiveMarket = item.Definition.EffectiveMarketPrice;
            int   maxAcceptable = Mathf.RoundToInt(effectiveMarket * tolerance);
            int   askingPrice   = Mathf.RoundToInt(item.CurrentPrice);
            return askingPrice <= maxAcceptable;
        }

        private bool HasArrived()
        {
            bool arrived = !agent.pathPending
                && agent.remainingDistance <= ARRIVED_THRESHOLD
                && (!agent.hasPath || agent.velocity.sqrMagnitude < 0.01f);
            return arrived;
        }

        private bool HasExitedStore()
        {
            if (StoreManager.Instance == null) return true;
            return !StoreManager.Instance.IsWithinStore(transform.position) && HasArrived();
        }

        private IEnumerator FaceTarget(Transform target)
        {
            if (target == null) yield break;
            Vector3 dir = (target.position - transform.position).normalized;
            dir.y = 0f;
            if (dir == Vector3.zero) yield break;
            yield return transform.DORotateQuaternion(Quaternion.LookRotation(dir), 0.4f)
                .SetEase(Ease.OutQuad)
                .WaitForCompletion();
        }

        private IEnumerator FaceDirection(Vector3 dir)
        {
            dir.y = 0f;
            if (dir == Vector3.zero) yield break;
            yield return transform.DORotateQuaternion(Quaternion.LookRotation(dir), 0.4f)
                .SetEase(Ease.OutQuad)
                .WaitForCompletion();
        }
        private void LogEntry(string message)
        {
            if (!debugEntryLogs) return;
            Debug.Log($"[Customer:{name}] {message}");
        }
        private void UpdateAnimator()
        {
            if (animator == null) return;
            
            bool hasPath = agent.hasPath;
            float remainingDist = agent.remainingDistance;
            
            bool isMoving = hasPath && remainingDist > agent.stoppingDistance && !float.IsInfinity(remainingDist);
            animator.SetBool("IsMoving", isMoving);
        }


    }

}
