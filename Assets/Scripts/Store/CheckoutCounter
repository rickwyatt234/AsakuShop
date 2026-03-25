using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using TMPro;
using UnityEngine;
using Unity.Cinemachine;
using AsakuShop.Core;
using AsakuShop.Economy;
using AsakuShop.Items;

namespace AsakuShop.Store
{
    // The player-operated checkout counter. The player interacts with it to
    // become the cashier, then drags items across the scanner to ring up a customer.
    // All money values are int yen. No fractions, no decimals, no dollars.
    public class CheckoutCounter : MonoBehaviour, IInteractable
    {
        // ── Inspector ─────────────────────────────────────────────────────────
        [Header("Queue")]
        [SerializeField] private Vector3 checkoutPoint;
        [SerializeField] private Vector3 liningDirection = Vector3.left;

        [Header("Belt & Packing")]
        [SerializeField] private Bounds  placementBounds;
        [SerializeField] private int     maxPlacementAttempts = 100;
        [SerializeField] private Vector3 packingPoint;

        [Header("Player Snap")]
        [SerializeField] private Transform cashierStandPoint;

        [Header("Cinemachine")]
        [SerializeField] private CinemachineCamera cashierCamera;

        [Header("Monitor")]
        [SerializeField] private TextMeshPro monitorText;

        [Header("Drag Handler & Snap Zones")]
        [SerializeField] private CheckoutDragHandler checkoutDragHandler;
        [SerializeField] private Transform           scanZoneTransform;
        [SerializeField] private Transform           bagZoneTransform;

        // ── State ─────────────────────────────────────────────────────────────
        public enum State { Standby, Placing, Scanning, CashPay, CardPay }
        public State CurrentState { get; private set; }

        // ── Queue ─────────────────────────────────────────────────────────────
        public List<ICheckoutCustomer> LiningCustomers { get; private set; } = new List<ICheckoutCustomer>();
        public int GetQueueNumber(ICheckoutCustomer customer) => LiningCustomers.IndexOf(customer);

        // ── Belt items ───────────────────────────────────────────────────────
        private List<CheckoutItem> beltItems = new List<CheckoutItem>();

        // ── Transaction state ─────────────────────────────────────────────────
        private int      totalPrice;      // yen
        private int      customerPayment; // yen
        private ICheckoutCustomer currentCustomer;

        // ── Checkout mode: is the player at the counter? ─────────────────────
        private bool playerAtCounter;

        // Set to true by ConfirmPayment() when the player confirms via input action or UI.
        private bool confirmPayment;

        // ── Yen denomination list (for CashPay reference) ────────────────────
        private static readonly int[] YenDenominations = { 10000, 5000, 1000, 500, 100, 50, 10, 5, 1 };

        // ── Lifecycle ─────────────────────────────────────────────────────────
        private void Awake()
        {
            UpdateMonitorText();

            if (checkoutDragHandler != null)
            {
                if (scanZoneTransform == null)
                    Debug.LogWarning("[CheckoutCounter] scanZoneTransform is not assigned. Items will scan immediately without snap animation.", this);
                if (bagZoneTransform == null)
                    Debug.LogWarning("[CheckoutCounter] bagZoneTransform is not assigned. Items will not snap to a bag zone after scanning.", this);
            }
        }

        private void Start()
        {
            StoreManager.Instance?.RegisterCounter(this);
        }

        private void Update()
        {
            // ESC handling removed — exit is now driven by the Cancel input action
            // added to InputMappings (handled via IInputManager by the caller).
        }

        // ── IInteractable ────────────────────────────────────────────────────

        public void OnInteract()
        {
            if (playerAtCounter) return;
            EnterCheckoutMode();
        }

        public void OnExamine() { }

        // ── Checkout mode ─────────────────────────────────────────────────────

        private void EnterCheckoutMode()
        {
            playerAtCounter = true;

            if (cashierCamera != null) cashierCamera.gameObject.SetActive(true);

            // Lock player movement while at the counter.
            PlayerService.InputManager?.DisableMovementInput();

            // Wire up the drag handler snap zones now that we are in checkout mode.
            checkoutDragHandler?.Configure(scanZoneTransform, bagZoneTransform);

            Debug.Log("[CheckoutCounter] Player entered checkout mode.");
        }

        private void ExitCheckoutMode()
        {
            playerAtCounter = false;

            if (cashierCamera != null) cashierCamera.gameObject.SetActive(false);

            // Release player movement lock.
            PlayerService.InputManager?.EnableMovementInput();

            Debug.Log("[CheckoutCounter] Player exited checkout mode.");
        }

        // ── Queue positioning ─────────────────────────────────────────────────

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

        // ── Place products on belt ────────────────────────────────────────────

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
                                        scanZoneTransform, bagZoneTransform);
                beltItems.Add(checkoutItem);
            }

            SetState(State.Scanning);
        }

        // ── Scanning ──────────────────────────────────────────────────────────

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

        // ── Payment ───────────────────────────────────────────────────────────

        private IEnumerator ProcessPayment()
        {
            bool useCash = Random.value < 0.6f;
            SetState(useCash ? State.CashPay : State.CardPay);

            customerPayment = useCash
                ? RoundUpToNearestDenomination(totalPrice)
                : totalPrice; // card: exact

            UpdateMonitorText();

            // Wait until the player confirms payment via the Confirm input action
            // (wired up in InputMappings and handled by the caller / UI), or
            // until the player leaves the counter (playerAtCounter becomes false).
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

        // Called externally (e.g. from an input action handler or UI button) to confirm
        // the current payment and advance the checkout flow.
        public void ConfirmPayment() => confirmPayment = true;

        // Called externally (e.g. from the Cancel input action) to exit checkout mode.
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
            Gizmos.color = Color.magenta;
            Vector3 worldCheckoutPt = transform.TransformPoint(checkoutPoint);
            Gizmos.DrawWireSphere(worldCheckoutPt, 0.2f);

            // Packing point
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(transform.TransformPoint(packingPoint), 0.2f);

            // Placement bounds
            Vector3 worldCenter = transform.TransformPoint(placementBounds.center);
            Gizmos.matrix = Matrix4x4.TRS(worldCenter, transform.rotation, Vector3.one);
            Gizmos.color  = Color.yellow;
            Gizmos.DrawWireCube(Vector3.zero, placementBounds.size);
            Gizmos.matrix = Matrix4x4.identity;
        }
#endif
    }
}
