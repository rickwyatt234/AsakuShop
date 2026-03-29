using DG.Tweening;
using TMPro;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.UI;

namespace AsakuShop.Store
{
    // Manages the cash payment flow across two phases:
    //
    //   Phase 1 — Entry:
    //     1. Camera zooms in on the cash register keypad.
    //     2. Player reads the total and customer's tendered amount from the display,
    //        then types the tendered amount using world-space numpad buttons (via Append()).
    //     3. Player presses the Cash/Tend button (confirmButton) → cash drawer opens,
    //        camera shifts down to the drawer view.
    //
    //   Phase 2 — Change:
    //     4. Player clicks denomination drawer slots (via AddChange()) to accumulate change.
    //     5. When changeGiven >= changeOwed (or changeOwed == 0), the Final Confirm button activates.
    //     6. Player presses Final Confirm → drawer closes, camera zooms out, OnPaymentComplete fires.
    //
    // World-space numpad buttons: wire each Button.onClick to call Append("0")–Append("9").
    // Wire the backspace button to Append("back"), the Clear (C) button to ClearEntry(),
    // and each denomination drawer's IInteractable.OnInteract to AddChange(denominationAmount).
    //
    // All amounts are whole yen (int). No fractions, no decimals.
    public class CashRegister : MonoBehaviour
    {
        public enum Phase { Closed, Entry, Change }

        [Header("Camera — Keypad View")]
        [SerializeField, Tooltip("Cinemachine virtual camera that zooms in on the cash register keypad. " +
            "Its GameObject must stay ENABLED — priority controls when it dominates.")]
        private CinemachineCamera zoomCamera;
        [SerializeField] private int zoomedPriority  = 20;
        [SerializeField] private int defaultPriority = 0;

        [Header("UI — Entry Panel")]
        [SerializeField, Tooltip("Root panel shown during the keypad-entry phase.")]
        private GameObject entryPanel;

        [SerializeField, Tooltip("Shows the transaction total (e.g. 'Total: ¥1,200').")]
        private TMP_Text totalText;

        [SerializeField, Tooltip("Shows how much the customer is tendering (e.g. 'Customer gives: ¥2,000').")]
        private TMP_Text changeOwedText;

        [SerializeField, Tooltip("Live display of the amount being keyed in by the player.")]
        private TMP_Text displayText;


        [Header("UI — Change Panel")]
        [SerializeField, Tooltip("Root panel shown during the change-giving phase (drawer open).")]
        private GameObject changePanel;

        [SerializeField, Tooltip("Shows the change to return (e.g. 'Change owed: ¥800').")]
        private TMP_Text changeGivenText;

        [SerializeField, Tooltip("Shows how much change the player has accumulated so far (e.g. 'Given: ¥500').")]
        private TMP_Text changeAccumulatedText;
    

        [Header("Drawer")]
        [SerializeField, Tooltip("CashDrawer component on the physical drawer GameObject.")]
        private CashDrawer cashDrawer;

        [Header("Timing")]
        [Tooltip("Seconds to wait after the camera cut before enabling numpad input.")]
        [SerializeField] private float zoomSettleTime = 0.6f;

        // Fired when the player presses the Final Confirm button.
        public event System.Action OnPaymentComplete;

        private string enteredAmount  = string.Empty;
        private int    totalPrice;
        private int    customerTenders;
        private int    changeOwed;
        private int    changeGiven;
        private bool   inputEnabled;
        private Phase  currentPhase = Phase.Closed;
        public Phase CurrentPhase => currentPhase;

        private void Awake()
        {
            entryPanel?.SetActive(true);
            changePanel?.SetActive(true);

            if (zoomCamera != null)
            {
                zoomCamera.Priority = defaultPriority;
                if (!zoomCamera.gameObject.activeInHierarchy)
                    Debug.LogWarning($"[CashRegister] zoomCamera '{zoomCamera.name}' is inactive. " +
                        "Enable its GameObject and let Cinemachine priority control when it takes over.");
            }
        }

        // Opens the register in the keypad-entry phase, zooms the camera in, and displays amounts.
        // totalAmount – the transaction total the customer owes.
        // tenders     – the cash amount the customer is handing over.
        // Called by CheckoutCounter.BeginCashPayment().
        public void Open(int totalAmount, int tenders)
        {
            totalPrice      = totalAmount;
            customerTenders = tenders;
            changeOwed      = Mathf.Max(0, tenders - totalAmount);
            enteredAmount   = string.Empty;
            changeGiven     = 0;
            inputEnabled    = false;
            currentPhase    = Phase.Entry;

            RefreshDisplay();

            if (zoomCamera   != null) zoomCamera.Priority   = zoomedPriority;

            DOVirtual.DelayedCall(zoomSettleTime, () =>
            {
                if (currentPhase != Phase.Entry) return;
                inputEnabled = true;
            });
        }

        // Hides all panels and zooms the camera back out.
        // Called internally after Final Confirm, and by CheckoutCounter on early exit.
        public void Close()
        {
            inputEnabled = false;
            currentPhase = Phase.Closed;


            cashDrawer?.Close();

            if (zoomCamera   != null) zoomCamera.Priority   = defaultPriority;
        }

        // Appends a digit, or "back" to delete the last character (entry phase only).
        // Wire each numpad button's onClick to call this with the button's digit string.
        // Wire the backspace button to call Append("back").
        public void Append(string input)
        {
            if (!inputEnabled || currentPhase != Phase.Entry) return;

            if (input == "back")
            {
                if (enteredAmount.Length > 0)
                    enteredAmount = enteredAmount.Substring(0, enteredAmount.Length - 1);
            }
            else if (input.Length > 0 && char.IsDigit(input[0]))
            {
                enteredAmount += input;
            }

            RefreshDisplay();
        }

        // Clears the entire keyed entry (entry phase only).
        // Wire the Clear (C) button's onClick to this method.
        public void ClearEntry()
        {
            if (!inputEnabled || currentPhase != Phase.Entry) return;

            enteredAmount = string.Empty;
            RefreshDisplay();
        }

        // Adds a denomination to the running change total (change phase only).
        // Called by CashDrawerButton when the player interacts with a denomination drawer slot.
        public void AddChange(int denomination)
        {
            if (currentPhase != Phase.Change) return;

            changeGiven += denomination;
            RefreshChangeDisplay();
        }

        // ── Private Handlers ────────────────────────────────────────────────

        // Fired by the Cash/Tend button (confirmButton) in the entry phase.
        // Opens the drawer and transitions to the change-giving phase.
        public void HandleEntryConfirm()
        {
            if (!inputEnabled || currentPhase != Phase.Entry) return;

            if (!int.TryParse(enteredAmount, out int _))
            {
                Debug.LogWarning("[CashRegister] Could not parse entered amount.");
                return;
            }

            // TODO: Implement consequences when entered != customerTenders.
            // For now, any confirmed entry opens the drawer regardless of amount.

            cashDrawer?.Open();
            TransitionToChangePhase();
        }

        // Switches the UI and camera from the keypad view to the drawer view.
        private void TransitionToChangePhase()
        {
            inputEnabled = false;
            currentPhase = Phase.Change;
            changeGiven  = 0;


            RefreshChangeDisplay();

            // Shift camera down to the drawer area.
            if (zoomCamera   != null) zoomCamera.Priority   = defaultPriority;
        }

        // Fired by the Final Confirm button once enough change has been given.
        public void HandleFinalConfirm()
        {
            if (currentPhase != Phase.Change) return;

            cashDrawer?.Close();
            Close();
            OnPaymentComplete?.Invoke();
        }

        // ── Display Helpers ─────────────────────────────────────────────────

        private void RefreshDisplay()
        {
            if (totalText      != null) totalText.text      = $"Total: ¥{totalPrice:N0}";
            if (changeOwedText != null) changeOwedText.text = $"Customer gives: ¥{customerTenders:N0}";
            if (displayText    != null)
                displayText.text = int.TryParse(enteredAmount, out int parsed)
                    ? $"¥{parsed:N0}"
                    : "¥0";
        }

        private void RefreshChangeDisplay()
        {
            if (changeGivenText      != null) changeGivenText.text      = $"Change owed: ¥{changeOwed:N0}";
            if (changeAccumulatedText != null) changeAccumulatedText.text = $"Given: ¥{changeGiven:N0}";
        }
    }
}
