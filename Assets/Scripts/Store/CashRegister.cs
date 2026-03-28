using DG.Tweening;
using TMPro;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.UI;

namespace AsakuShop.Store
{
    // Manages the cash payment flow:
    //   1. Camera zooms in on the cash register.
    //   2. Player reads the total and customer's tendered amount from the display,
    //      then types the tendered amount on the world-space numpad buttons (via Append()).
    //   3. Player presses Confirm → the cash drawer opens → OnPaymentComplete fires.
    //
    // World-space numpad buttons: wire each digit Button.onClick to call Append("0")–Append("9").
    // Wire the backspace Button.onClick to call Append("back").
    // Wire the Clear button to ClearEntry() and the Confirm button to the confirmButton field.
    //
    // All amounts are whole yen (int). No fractions, no decimals.
    public class CashRegister : MonoBehaviour
    {
        [Header("Camera")]
        [SerializeField, Tooltip("Cinemachine virtual camera that zooms in on the cash register. " +
            "Its GameObject must stay ENABLED — priority controls when it dominates.")]
        private CinemachineCamera zoomCamera;
        [SerializeField] private int zoomedPriority  = 20;
        [SerializeField] private int defaultPriority = 0;

        [Header("UI — Labels")]
        [SerializeField, Tooltip("Shows the transaction total (e.g. 'Total: ¥1,200').")]
        private TMP_Text totalText;
        [SerializeField, Tooltip("Shows how much the customer is tendering (e.g. 'Customer gives: ¥2,000').")]
        private TMP_Text changeOwedText;
        [SerializeField, Tooltip("Shows the change to return (e.g. 'Change: ¥800').")]
        private TMP_Text changeGivenText;
        [SerializeField, Tooltip("Live display of the amount being keyed in by the player.")]
        private TMP_Text displayText;

        [Header("UI — Buttons")]
        [SerializeField, Tooltip("Removes the last keyed digit (backspace). Wire onClick → Append(\"back\").")]
        private Button undoButton;
        [SerializeField, Tooltip("Clears the entire keyed entry. Wire onClick → ClearEntry().")]
        private Button clearButton;
        [SerializeField, Tooltip("Confirms the payment and opens the cash drawer.")]
        private Button confirmButton;

        [Header("Panel")]
        [SerializeField] private GameObject registerPanel;

        [Header("Drawer")]
        [SerializeField, Tooltip("CashDrawer component on the physical drawer GameObject.")]
        private CashDrawer cashDrawer;

        [Header("Timing")]
        [Tooltip("Seconds to wait after the camera cut before enabling numpad input.")]
        [SerializeField] private float zoomSettleTime = 0.6f;

        // Fired when the player presses Confirm.
        public event System.Action OnPaymentComplete;

        private string enteredAmount  = string.Empty;
        private int    totalPrice;
        private int    customerTenders;
        private int    changeOwed;
        private bool   inputEnabled;

        private void Awake()
        {
            registerPanel?.SetActive(false);

            undoButton?.onClick.AddListener(() => Append("back"));
            clearButton?.onClick.AddListener(ClearEntry);
            confirmButton?.onClick.AddListener(HandleConfirm);

            if (zoomCamera != null)
            {
                zoomCamera.Priority = defaultPriority;

                if (!zoomCamera.gameObject.activeInHierarchy)
                    Debug.LogWarning($"[CashRegister] zoomCamera '{zoomCamera.name}' is inactive. " +
                        "Enable its GameObject and let Cinemachine priority control when it takes over.");
            }
        }

        // Opens the register, zooms the camera in, and displays the transaction amounts.
        // totalAmount   – the transaction total the customer owes.
        // tenders       – the cash amount the customer is handing over.
        // Called by CheckoutCounter.BeginCashPayment().
        public void Open(int totalAmount, int tenders)
        {
            totalPrice      = totalAmount;
            customerTenders = tenders;
            changeOwed      = Mathf.Max(0, tenders - totalAmount);
            enteredAmount   = string.Empty;
            inputEnabled    = false;

            registerPanel?.SetActive(true);
            RefreshDisplay();
            RefreshConfirmButton();

            if (zoomCamera != null) zoomCamera.Priority = zoomedPriority;

            DOVirtual.DelayedCall(zoomSettleTime, () =>
            {
                inputEnabled = true;
                RefreshConfirmButton();
            });
        }

        // Hides the panel and zooms the camera back out.
        // Called internally after Confirm, and by CheckoutCounter on early exit.
        public void Close()
        {
            inputEnabled = false;
            registerPanel?.SetActive(false);

            if (zoomCamera != null) zoomCamera.Priority = defaultPriority;
        }

        // Appends a digit, or "back" to delete the last character.
        // Wire each numpad button's onClick to call this method with the button's digit string.
        // Wire the backspace button to call Append("back").
        public void Append(string input)
        {
            if (!inputEnabled) return;

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
            RefreshConfirmButton();
        }

        // Clears the entire keyed entry.
        // Wire the Clear button's onClick to this method.
        public void ClearEntry()
        {
            if (!inputEnabled) return;

            enteredAmount = string.Empty;
            RefreshDisplay();
            RefreshConfirmButton();
        }

        private void HandleConfirm()
        {
            if (!inputEnabled) return;

            if (!int.TryParse(enteredAmount, out int entered))
            {
                Debug.LogWarning("[CashRegister] Could not parse entered amount.");
                return;
            }

            // TODO: Implement consequences when entered != customerTenders.
            // For now, any confirmed entry opens the drawer and completes the payment.

            cashDrawer?.Open();
            Close();
            OnPaymentComplete?.Invoke();
        }

        private void RefreshDisplay()
        {
            if (totalText       != null) totalText.text       = $"Total: ¥{totalPrice:N0}";
            if (changeOwedText  != null) changeOwedText.text  = $"Customer gives: ¥{customerTenders:N0}";
            if (changeGivenText != null) changeGivenText.text = $"Change: ¥{changeOwed:N0}";
            if (displayText     != null)
                displayText.text = int.TryParse(enteredAmount, out int parsed)
                    ? $"¥{parsed:N0}"
                    : "¥0";
        }

        private void RefreshConfirmButton()
        {
            if (confirmButton == null) return;
            // Enabled once the camera has settled and the player has keyed in something
            // (or no change is owed so the transaction is trivially confirmed).
            confirmButton.interactable = inputEnabled &&
                (changeOwed == 0 || !string.IsNullOrEmpty(enteredAmount));
        }
    }
}
