using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using DG.Tweening;
using AsakuShop.Core;
using AsakuShop.Items;
using AsakuShop.Store;
using AsakuShop.Storage;

namespace AsakuShop.Customers
{
    /// AI pedestrian/customer. Always spawns as a pedestrian wandering the world.
    /// If wantsToShop is true (set at spawn time), will enter the store and shop.
    /// Implements ICheckoutCustomer so CheckoutCounter can process this customer.
    /// Implements IAskToLeave + IPedestrianCustomer for StoreManager to control.
    [RequireComponent(typeof(Animator), typeof(NavMeshAgent))]
    public class Customer : MonoBehaviour, ICheckoutCustomer
    {

        [Header("NavmeshAgent Settings")]
        private const float WALK_SPEED              = 1.5f;
        private const float CONTINUE_SHOPPING_CHANCE = 0.5f;
        private const float STOPPING_DISTANCE       = 0.6f;
        private const float ARRIVED_THRESHOLD       = 0.8f;


        //Behavior Flag
        public event System.Action OnLeave;


        [Header("References")]
        [SerializeField] private CustomerHandAttachments handAttachments;
        [SerializeField] private CustomerOverheadUI      overheadUI;
        private Animator animator;
        private NavMeshAgent agent;
        private ShelfComponent targetShelf;
        private CheckoutCounter targetCounter;
        private int queueNumber = int.MaxValue;
        private bool isPicking = false;

        [Header("Dialogue")]
        private Dialogue notFoundDialogue => GameConfig.Instance.NotFoundDialogue;
        private Dialogue overpricedDialogue => GameConfig.Instance.OverpricedDialogue;

        [Header("Inventory")]
        public List<ItemInstance> Inventory { get; private set; } = new List<ItemInstance>();


        [Header("Debug")]
        [SerializeField] private bool debugEntryLogs = true;


#region Unity Lifecycle

        private void Awake()
        {
            animator = GetComponent<Animator>();
            agent    = GetComponent<NavMeshAgent>();

            agent.speed                  = WALK_SPEED;
            agent.angularSpeed           = 3600f;
            agent.acceleration           = 100f;
            agent.stoppingDistance       = STOPPING_DISTANCE;
            agent.obstacleAvoidanceType  = ObstacleAvoidanceType.NoObstacleAvoidance;
            agent.SetAreaCost(3, 50f);
        }

        private void Start()
        {
            StartCoroutine(CheckEnteringStore());
            StartCoroutine(Shopping());
        }

        private void Update()
        {
            CheckStoreDoors();
        }
#endregion


#region Door Interaction
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
#endregion


#region Coroutines
        private IEnumerator CheckEnteringStore()
        {
            while (!StoreManager.Instance.IsWithinStore(transform.position))
            {
                yield return new WaitForSeconds(0.1f);
            }

            //Play a SFX or something here to indicate the customer has entered the store
        }

        private IEnumerator Shopping()
        {
            bool continueShopping = true;

            while (continueShopping)
            {
                yield return FindShelf();
                yield return BrowseShelf();

                // Random chance to continue shopping after each item
                continueShopping = Random.value < CONTINUE_SHOPPING_CHANCE;
            }

            if (targetShelf != null && targetShelf.IsOpen)
            {
                // Close the shelving unit if it's open (e.g., Fridges, Freezers)
                targetShelf.Close(true, false);
            }

            if (Inventory.Count > 0)
            {
                yield return Checkout();
                yield return Leave();
            }
            else
            {
                // Customer leaves without buying anything
                overheadUI.ShowDialog(notFoundDialogue.GetRandomLine());
                yield return Leave();
            }
        }

        private IEnumerator FindShelf()
        {
            ShelfComponent shelf = StoreManager.Instance.GetRandomShelf();

            if (targetShelf != null && targetShelf != shelf && targetShelf.IsOpen)
            {
                // Close the previous shelving unit if it's open (e.g., Fridges, Freezers)
                targetShelf.Close(true, false);
            }

            targetShelf = shelf;

            if (targetShelf == null)
            {
                Debug.LogWarning("[Customer] No shelves available.");
                yield break;
            }

            StoreManager.Instance.UnregisterShelf(targetShelf); // Temporarily remove from pool while browsing
            agent.SetDestination(targetShelf.transform.position + targetShelf.transform.forward * -0.5f);

            while (!HasArrived())
            {
                if (targetShelf.IsMoving)
                {
                    agent.SetDestination(transform.position); // Stop moving if shelf is moving
                    targetShelf = null;
                    yield break;
                }
                yield return LookAt(targetShelf.transform);
            }
        }

        private IEnumerator BrowseShelf()
        {
            if (targetShelf == null) yield break;
            overheadUI.ShowStatus("👀");
            while (true)
            {
                if (targetShelf.IsMoving)
                {
                    targetShelf = null;
                    yield break;
                }

                // If shelf has items, there's a chance the customer will take one
                if (targetShelf.PeekItem() != null)
                {
                    ItemInstance item = targetShelf.PeekItem();
                    if (IsWillingToBuy(item))
                    {
                        overheadUI.ShowStatus("👍");


                        ShelfTakeResult taken = targetShelf.TakeItem();
                        if (!taken.HasItem)
                        {
                            yield return null;
                            continue;
                        }

                        Inventory.Add(taken.Item);
                        if (!targetShelf.IsOpen) targetShelf.Open(true, false);

                        float height = targetShelf.transform.position.y;
                        string pickTrigger = "PickMedium";
                        if (height < 0.5f) pickTrigger = "PickLow";
                        else if (height > 1.5f) pickTrigger = "PickHigh";
                        animator.SetTrigger(pickTrigger);

                        yield return new WaitUntil(() => isPicking);

                        if (taken.Pickup != null)
                        {
                            Transform grip = handAttachments.Grip;
                            Transform t = taken.Pickup.transform;

                            t.SetParent(grip, true);

                            if (taken.Pickup.TryGetComponent(out Rigidbody rb))
                            {
                                rb.isKinematic = true;
                                rb.useGravity = false;
                                rb.linearVelocity = Vector3.zero;
                                rb.angularVelocity = Vector3.zero;
                            }

                            Collider[] cols = taken.Pickup.GetComponentsInChildren<Collider>(true);
                            foreach (Collider c in cols) c.enabled = false;

                            t.DOLocalRotate(Vector3.zero, 0.25f);
                            t.DOLocalMove(Vector3.zero, 0.25f);
                        }

                        isPicking = false;

                        bool isIdle = false;
                        while (!isIdle)
                        {
                            isIdle = animator.GetCurrentAnimatorStateInfo(0).IsName("Idle");
                            yield return null;
                        }

                        if (taken.Pickup != null)
                            Destroy(taken.Pickup.gameObject);

                        yield return new WaitForSeconds(0.5f);
                        break; // Done after taking one item; Shopping() decides whether to browse again
                    }
                    else
                    {
                        overheadUI.ShowDialog(overpricedDialogue.GetRandomLine());
                        yield return new WaitForSeconds(1f);
                        break; // Done after rejecting the top item; move on to the next shelf visit
                    }
                }
                else
                {
                    // Shelf is empty — yield one frame to avoid a tight spin, then exit
                    yield return null;
                    break;
                }
            }

            // Return the shelf to the browsing pool so other customers can visit it
            if (targetShelf != null)
                StoreManager.Instance.RegisterShelf(targetShelf);
        }
#endregion


#region MoveTo/LookAt Helpers
        private IEnumerator MoveTo(Vector3 destination)
        {
            agent.SetDestination(destination);
            yield return new WaitUntil(() => HasArrived());
            yield return new WaitForSeconds(0.2f); // Small pause after arriving
        }

        private IEnumerator LookAt(Transform target)
        {
            var lookDirection = (target.position - transform.position).Flatten();
            var lookRotation = Quaternion.LookRotation(lookDirection);
            yield return transform.DORotateQuaternion(lookRotation, 0.5f).WaitForCompletion();
        }

        private IEnumerator LookAt(Vector3 lookDirection)
        {
            var lookRotation = Quaternion.LookRotation(lookDirection.Flatten());
            yield return transform.DORotateQuaternion(lookRotation, 0.5f).WaitForCompletion();
        }
        public void OnPick(AnimationEvent _)
        {
            isPicking = true;
        }
#endregion


#region Decision Helpers
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
#endregion


#region Queueing, Checkout and Leaving
        public IEnumerator UpdateQueue()
        {
            // While the customer's queue number is greater than 0 (meaning they are still in the queue).
            while (queueNumber > 0)
            {
                int newQueueNumber = targetCounter.GetQueueNumber(this);

                // Check if the customer's queue number has improved (become lower).
                if (newQueueNumber < queueNumber)
                {
                    // Update the customer's queue number.
                    queueNumber = newQueueNumber;

                    Vector3 queuePosition = targetCounter.GetQueuePosition(this, out Vector3 lookDirection);

                    // Move the customer to their new queue position.
                    yield return MoveTo(queuePosition);

                    // Make the customer look in the correct direction at their new position.
                    yield return LookAt(lookDirection);
                }
                else
                {
                    // If the queue number hasn't improved, wait briefly before checking again.
                    yield return new WaitForSeconds(0.1f);
                }
            }
            // When the queueNumber is 0, this coroutine will stop.
        }

        private IEnumerator Checkout()
        {
            targetCounter = StoreManager.Instance.GetShortestQueueCounter();
            targetCounter.LiningCustomers.Add(this);
            yield return StartCoroutine(UpdateQueue());
            yield return targetCounter.PlaceProducts(this);
            yield return new WaitUntil(() => targetCounter.CurrentState == CheckoutCounter.State.Standby);
        }

        public void OnCheckoutComplete()
        {
            // This method is called by the counter after payment is complete.
            // We can trigger any post-checkout behavior here, such as showing a thank you message.
            overheadUI.ShowStatus("Thank you!");
            queueNumber = int.MaxValue; // Reset queue number to indicate we're no longer in line
        }

        private IEnumerator Leave()
        {
            if (targetCounter != null)
                targetCounter.LiningCustomers.Remove(this);
            
            OnLeave?.Invoke();
            var exitPoint = StoreManager.Instance.GetExitPoint();
            agent.SetDestination(exitPoint.position);
            yield return new WaitForEndOfFrame(); // Ensure position is updated before checking
            Destroy(gameObject);
        }

#endregion


        private bool HasArrived()
        {
            if (!agent.pathPending)
            {
                if (agent.remainingDistance <= agent.stoppingDistance)
                {
                    if (!agent.hasPath || agent.velocity.sqrMagnitude == 0f)
                    {
                        animator.SetBool("IsMoving", false);
                        return true;
                    }
                }
            }

            animator.SetBool("IsMoving", true);
            return false;
        }


    }

}
