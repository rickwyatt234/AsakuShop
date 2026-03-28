using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using Unity.Cinemachine;
using AsakuShop.Core;
using AsakuShop.Economy;

namespace AsakuShop.Store
{
    // The player-operated checkout counter. The player interacts with it to
    // become the cashier, then drags items across the scanner to ring up a customer.
    // All money values are int yen. No fractions, no decimals, no dollars.
    public class CheckoutCounter : MonoBehaviour, IInteractable
    {
        [Header("Queue")]
            [Tooltip("The point where customers line up for checkout. Relative to counter transform.")]
                [SerializeField] private Vector3 checkoutPoint;

            [Tooltip("Direction that customers line up in. Should be parallel to the counter front.")]
                [SerializeField] private Vector3 liningDirection = Vector3.left;

        [Header("Belt & Packing")]
        
            [Tooltip("Bounds within which products are placed on the belt. Relative to counter transform.")]
                [SerializeField] private Bounds  placementBounds;

            [Tooltip("The position where change is given to the customer.")]
                [SerializeField] private Vector3 moneyPoint;

            [Tooltip("Number of attempts to find a non-overlapping position for belt items. Placement fails after this many attempts, and the item is spawned anyway.")]
                [SerializeField] private int     maxPlacementAttempts = 100;

            [Tooltip("Point where items move to when scanned, before being destroyed. Relative to counter transform.")]
                [SerializeField] private Vector3 packingPoint;

        [Header("Player Snap")]
            [Tooltip("Point where the player stands when operating the counter. Used for queue positioning and camera look direction.")]
                [SerializeField] private Transform cashierStandPoint;

        [Header("Monitor")]
            [Tooltip("Text mesh pro component used to display information on the checkout monitor.")]
                [SerializeField] private TextMeshProUGUI monitorText;

        [Header("Drag Handler & Snap Zones")]
                [SerializeField] private CheckoutDragHandler checkoutDragHandler;
                [SerializeField] private Transform           scanZoneTransform;
                [SerializeField] private Transform           bagZoneTransform;

        [Header("Payment Apparatus")]
                [Tooltip("PaymentTerminal component on the card reader 3D object.")]
                [SerializeField] private PaymentTerminal paymentTerminal;
                [Tooltip("CashRegister component on the cash register 3D object.")]
                [SerializeField] private CashRegister    cashRegister;

        [Header("Player Snapping")]
                private Vector3 savedPlayerPosition;
                private Quaternion savedPlayerRotation;


        //Queue management
                public int GetQueueNumber(ICheckoutCustomer customer) => LiningCustomers.IndexOf(customer);
                public List<ICheckoutCustomer> LiningCustomers { get; private set; } = new List<ICheckoutCustomer>();

        
        //Checkout state management
        public enum State { Standby, Placing, Scanning, CashPay, CardPay }
        public State CurrentState { get; private set; }


        //Belt Items
        private List<CheckoutItem> beltItems = new List<CheckoutItem>();


        //Transactions
        private int      totalPrice;      // yen
        private int      customerPayment; // yen
        private ICheckoutCustomer currentCustomer;


        //Checkout mode: is the player at the counter?
        private bool playerAtCounter;


        // Set to true by ConfirmPayment() when the player finishes the transaction (giving change,  confirming card payment, etc.)
        private bool confirmPayment;


        //Yen denomination list (for CashPay reference)
        private static readonly int[] YenDenominations = { 10000, 5000, 2000, 1000, 100, 50, 10, 5, 1 };

#region Unity Lifecycle
        private void Awake()
        {
            UpdateMonitorText();
        }

        private void Start()
        {
            StoreManager.Instance?.RegisterCounter(this);
        }

        private void Update()
        {
            if (playerAtCounter && PlayerService.InputManager?.cancel == true)
            {
                ExitCheckoutMode();
            }
        }
#endregion


#region IInteractable Implementation

        public void OnInteract()
        {
            if (playerAtCounter) return;
            EnterCheckoutMode();
        }

        public void OnExamine() { }

        public void OnCancel()
        {
            if (playerAtCounter)
            {
                ExitCheckoutMode();
            }
        }
#endregion


#region Player Checkout Mode
        private void EnterCheckoutMode()
        {
            playerAtCounter = true;

            GameStateController.Instance.RequestTransition(GamePhase.Checkout);

            Transform playerRoot = PlayerService.PickupTarget?.GetTransform();
            
            if (playerRoot != null)
            {
                savedPlayerPosition = playerRoot.position;
                savedPlayerRotation = playerRoot.rotation;
                
                if (cashierStandPoint != null)
                {
                    playerRoot.position = cashierStandPoint.position;
                    playerRoot.rotation = cashierStandPoint.rotation;
                }
            }
            
            PlayerService.InputManager?.EnableLookInput();
            PlayerService.InputManager?.DisableMovementInput();

            checkoutDragHandler?.Configure(scanZoneTransform, bagZoneTransform);
            PlayerService.PickupTarget?.TryPickupInteractable(null);

            Debug.Log("[CheckoutCounter] Player entered checkout mode.");
        }

        private void ExitCheckoutMode()
        {
            playerAtCounter = false;

            // Close any open payment UIs and zoom cameras before restoring the player.
            paymentTerminal?.Close();
            cashRegister?.Close();

            RemoveCustomerCashInteractable();

            GameStateController.Instance.RequestTransition(GamePhase.Playing);
            
            Transform playerRoot = PlayerService.PickupTarget?.GetTransform();
            
            if (playerRoot != null)
            {
                playerRoot.position = savedPlayerPosition;
                playerRoot.rotation = savedPlayerRotation;
            }
            
            PlayerService.InputManager?.EnableLookInput();
            PlayerService.InputManager?.EnableMovementInput();

            Debug.Log("[CheckoutCounter] Player exited checkout mode.");
        }
#endregion


#region Product Placement and Queue Management
        // Places the customer's inventory items onto the checkout belt with DOTween jump animations.
        public IEnumerator PlaceProducts(ICheckoutCustomer customer)
        {
            SetState(State.Placing);
            currentCustomer = customer;

            foreach (var item in customer.Inventory)
            {
                if (item?.Definition?.WorldPrefab == null) continue;

                int    attempts           = 0;
                bool   placementSucceeded = false;
                Vector3   position        = Vector3.zero;
                Quaternion rotation       = Quaternion.identity;

                while (attempts < maxPlacementAttempts)
                {
                    position.x = Random.Range(placementBounds.min.x, placementBounds.max.x);
                    position.y = placementBounds.min.y;
                    position.z = Random.Range(placementBounds.min.z, placementBounds.max.z);
                    position   = transform.TransformPoint(position);
                    rotation   = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

                    // Crude overlap check — any collider at this position?
                    Collider[] overlaps = Physics.OverlapBox(position, Vector3.one * 0.1f, rotation);
                    if (overlaps.Length == 0) { placementSucceeded = true; break; }
                    attempts++;
                    yield return null;
                }

                if (!placementSucceeded)
                    Debug.LogWarning($"[CheckoutCounter] Could not place '{item.Definition.DisplayName}' after {maxPlacementAttempts} attempts.");

                // Spawn at "customer hand" position, animate to belt
                Vector3 spawnPos = currentCustomer is MonoBehaviour mb
                    ? mb.transform.TransformPoint(new Vector3(0f, 1f, 0.5f))
                    : position + Vector3.up * 1.5f;

                GameObject model = Instantiate(item.Definition.WorldPrefab, spawnPos, Quaternion.identity);
                model.transform.localScale = Vector3.zero;

                const float jumpDur = 0.3f;
                model.transform.DOJump(position, 0.5f, 1, jumpDur);
                model.transform.DORotateQuaternion(rotation, jumpDur);
                model.transform.DOScale(Vector3.one, jumpDur);
                yield return new WaitForSeconds(jumpDur);

                var checkoutItem = model.AddComponent<CheckoutItem>();
                checkoutItem.Initialize(item, () => HandleScanning(checkoutItem),
                                        null, scanZoneTransform, bagZoneTransform);
                beltItems.Add(checkoutItem);
            }

            SetState(State.Scanning);
        }


        public Vector3 GetQueuePosition(ICheckoutCustomer customer, out Vector3 lookDirection)
        {
            Vector3 worldCheckoutPoint = transform.TransformPoint(checkoutPoint);
            int queueNumber            = GetQueueNumber(customer);

            lookDirection = queueNumber > 0
                ? -liningDirection
                : (cashierStandPoint != null
                    ? (cashierStandPoint.position - worldCheckoutPoint).normalized
                    : Vector3.forward);

            return worldCheckoutPoint + liningDirection * queueNumber * 0.5f;
        }
#endregion


#region Player Scanning and Payment Processing
        private void HandleScanning(CheckoutItem item)
        {
            if (!beltItems.Contains(item)) return;
            beltItems.Remove(item);

            item.transform.DOMove(transform.TransformPoint(packingPoint), 0.3f)
                .OnComplete(() => Destroy(item.gameObject));

            totalPrice += Mathf.RoundToInt(item.Item.CurrentPrice);
            UpdateMonitorText();

            if (beltItems.Count == 0)
                StartCoroutine(ProcessPayment());
        }
        
        private IEnumerator ProcessPayment()
        {
            bool useCash = Random.value < 0.6f;
            SetState(useCash ? State.CashPay : State.CardPay);

            customerPayment = useCash
                ? RoundUpToNearestDenomination(totalPrice)
                : totalPrice; // card: exact

            UpdateMonitorText();

            // For cash payments, make the customer's money interactable so the player can
            // click it to open the cash register.
            if (useCash && currentCustomer is MonoBehaviour customerMb)
            {
                var cashInteractable = customerMb.gameObject.AddComponent<CustomerCashInteractable>();
                cashInteractable.Initialize(this);
            }

            // Wait until the player confirms payment by giving change (if any), or confirming on the card terminal.
            confirmPayment = false;
            Debug.Log($"[CheckoutCounter] Awaiting payment confirmation. Total: ¥{totalPrice:N0}, Customer pays: ¥{customerPayment:N0}.");
            yield return new WaitUntil(() => confirmPayment || !playerAtCounter);

            // Finalize sale
            if (EconomyManager.Instance != null && currentCustomer != null)
            {
                foreach (var item in currentCustomer.Inventory)
                    EconomyManager.Instance.RecordSale(item, Mathf.RoundToInt(item.CurrentPrice));
            }

            currentCustomer?.OnCheckoutComplete();
            if (currentCustomer != null)
                LiningCustomers.Remove(currentCustomer);
            currentCustomer = null;
            totalPrice      = 0;
            customerPayment = 0;
            confirmPayment  = false;

            SetState(State.Standby);
        }
#endregion

        public void ConfirmPayment()
        {
            if (!playerAtCounter) return;
            confirmPayment = true;
            Debug.Log("[CheckoutCounter] Payment confirmed.");
        }

        // Called by CustomerCardInteractable when the player interacts with the customer's card.
        public void BeginCardPayment()
        {
            if (CurrentState != State.CardPay || paymentTerminal == null) return;

            // Guard against double-subscription.
            paymentTerminal.OnPaymentComplete -= HandlePaymentComplete;
            paymentTerminal.OnPaymentComplete += HandlePaymentComplete;

            paymentTerminal.Open(totalPrice);
            Debug.Log($"[CheckoutCounter] Card payment started. Amount: ¥{totalPrice:N0}.");
        }

        // Called by CustomerCashInteractable when the player interacts with the customer's cash.
        public void BeginCashPayment()
        {
            if (CurrentState != State.CashPay || cashRegister == null) return;

            // Guard against double-subscription.
            cashRegister.OnPaymentComplete -= HandlePaymentComplete;
            cashRegister.OnPaymentComplete += HandlePaymentComplete;

            cashRegister.Open(totalPrice, customerPayment);
            Debug.Log($"[CheckoutCounter] Cash payment started. " +
                $"Total: ¥{totalPrice:N0}, Customer tenders: ¥{customerPayment:N0}.");
        }

        // Shared callback fired by either the PaymentTerminal or the CashRegister on completion.
        private void HandlePaymentComplete()
        {
            // Unsubscribe to avoid stale references on the next transaction.
            if (paymentTerminal != null) paymentTerminal.OnPaymentComplete -= HandlePaymentComplete;
            if (cashRegister    != null) cashRegister.OnPaymentComplete    -= HandlePaymentComplete;

            RemoveCustomerCashInteractable();

            ConfirmPayment();
        }

        // Removes the temporary CustomerCashInteractable from the current customer if present.
        private void RemoveCustomerCashInteractable()
        {
            if (currentCustomer is MonoBehaviour customerMb)
            {
                var cashInteractable = customerMb.GetComponent<CustomerCashInteractable>();
                if (cashInteractable != null) Destroy(cashInteractable);
            }
        }
        



        public void RequestExit() => ExitCheckoutMode();

        // ── Helpers ───────────────────────────────────────────────────────────

        private int RoundUpToNearestDenomination(int amount)
        {
            foreach (int denom in YenDenominations)
                if (amount <= denom) return denom;

            // If above all denominations, round up to nearest ¥1000
            return Mathf.CeilToInt(amount / 1000f) * 1000;
        }

        private void SetState(State newState)
        {
            CurrentState = newState;
            UpdateMonitorText();
        }

        private void UpdateMonitorText()
        {
            if (monitorText == null) return;

            string text = CurrentState switch
            {
                State.Standby  => "\n\nStandby...",
                State.Placing  => "\n\nWaiting...",
                State.Scanning => $"Scanning...\n<color=#00a4ff>Total: ¥{totalPrice:N0}</color>",
                State.CashPay  =>
                    $"Cash Payment\n" +
                    $"<color=#00a4ff>Total: ¥{totalPrice:N0}</color>\n" +
                    $"Received: ¥{customerPayment:N0}\n" +
                    $"<color=yellow>Change: ¥{customerPayment - totalPrice:N0}</color>",
                State.CardPay  =>
                    $"Card Payment\n" +
                    $"<color=#00a4ff>Total: ¥{totalPrice:N0}</color>\n" +
                    "Confirm on terminal.",
                _ => string.Empty
            };

            monitorText.text = text;
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            // Checkout queue point
            // Draw the checkout point as a wire sphere in world space, accounting for the counter's transform.
            Gizmos.color = Color.magenta;
            Vector3 worldCheckoutPt = transform.TransformPoint(checkoutPoint);
            Gizmos.DrawWireSphere(worldCheckoutPt, 0.2f);

            // Packing point
            // Draw the packing point as a wire sphere in world space, accounting for the counter's transform.
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(transform.TransformPoint(packingPoint), 0.2f);

            // Placement bounds
            // Draw the placement bounds as a wire cube in world space, accounting for the counter's transform and rotation.
            Vector3 worldCenter = transform.TransformPoint(placementBounds.center);
            Gizmos.matrix = Matrix4x4.TRS(worldCenter, transform.rotation, Vector3.one);
            Gizmos.color  = Color.yellow;
            Gizmos.DrawWireCube(Vector3.zero, placementBounds.size);
            Gizmos.matrix = Matrix4x4.identity;
        }
#endif
    }
}