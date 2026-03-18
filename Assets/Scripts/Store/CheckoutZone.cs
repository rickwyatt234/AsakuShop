using UnityEngine;

namespace AsakuShop.Store
{
    public class CheckoutZone : MonoBehaviour
    {
        public Transform GetCheckoutPosition() => checkoutPosition;
        public Transform GetCustomerQueuePosition() => customerQueuePosition;  // NEW
        
        [SerializeField] private Transform checkoutPosition;        // Player stands here
        [SerializeField] private Transform customerQueuePosition;   // Customers stand here (NEW)
        [SerializeField] private CheckoutTerminal terminal;
        [SerializeField] private Transform scannerPoint;
        [SerializeField] private BaggingArea baggingArea;
        [SerializeField] private CustomerItemDropZone dropZone;

        private ICheckoutPlayer player;
        private bool isPlayerCheckingOut = false;

        private void Start()
        {
            if (terminal == null)
                terminal = GetComponent<CheckoutTerminal>();
            if (baggingArea == null)
                baggingArea = GetComponentInChildren<BaggingArea>();
            if (dropZone == null)
                dropZone = GetComponentInChildren<CustomerItemDropZone>();
            
            // Validate both positions exist
            if (checkoutPosition == null)
                Debug.LogError("[CHECKOUT] checkoutPosition not assigned!");
            if (customerQueuePosition == null)
                Debug.LogError("[CHECKOUT] customerQueuePosition not assigned!");
        }

        public void TryStartCheckout(ICheckoutPlayer player)
        {
            Debug.Log("[CHECKOUT DEBUG - Zone] TryStartCheckout called");
            
            if (isPlayerCheckingOut)
            {
                Debug.Log("[CHECKOUT DEBUG - Zone] Already checking out, aborting");
                return;
            }

            this.player = player;
            isPlayerCheckingOut = true;

            Debug.Log("[CHECKOUT DEBUG - Zone] Calling player.SnapToCheckoutPosition()...");
            player.SnapToCheckoutPosition(checkoutPosition);
            
            Debug.Log("[CHECKOUT DEBUG - Zone] Calling player.LockMovement(true)...");
            player.LockMovement(true);

            Debug.Log("[CHECKOUT DEBUG - Zone] ✓ Checkout started");
        }

        public void TryEndCheckout()
        {
            if (!isPlayerCheckingOut)
                return;

            isPlayerCheckingOut = false;
            player.LockMovement(false);
            terminal.ClearTerminal();

            Debug.Log("[CHECKOUT] Checkout session ended");
        }

        public CheckoutTerminal GetTerminal() => terminal;
        public Transform GetScannerPoint() => scannerPoint;
        public BaggingArea GetBaggingArea() => baggingArea;
        public bool IsPlayerCheckingOut() => isPlayerCheckingOut;
    }
}