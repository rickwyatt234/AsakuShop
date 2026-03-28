using DG.Tweening;
using TMPro;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.UI;

namespace AsakuShop.Store
{
    // Manages the card payment flow:
    //   1. Camera zooms in on the card reader.
    //   2. Player keys in the transaction total and presses Confirm.
    //   3. A success screen appears on the terminal.
    //   4. Player presses the final Confirm button → camera zooms out → OnPaymentComplete fires.
    //
    // All amounts are whole yen (int). No fractions, no decimals.
    public class PaymentTerminal : MonoBehaviour
    {
        [Header("Camera")]
        [SerializeField, Tooltip("Cinemachine virtual camera that zooms in on the card reader.")]
        private CinemachineCamera zoomCamera;
        [SerializeField] private int zoomedPriority  = 20;
        [SerializeField] private int defaultPriority = 0;

        [Header("Entry Panel — keypad & amount display")]
        [SerializeField] private GameObject entryPanel;
        [SerializeField] private TMP_Text   displayText;
        [SerializeField] private Button     confirmEntryButton;

        [Header("Success Panel — shown after correct amount is entered")]
        [SerializeField] private GameObject successPanel;
        [SerializeField] private TMP_Text   successText;
        [SerializeField] private Button     finalConfirmButton;

        [Header("Timing")]
        [Tooltip("Seconds to wait after the camera cut before enabling keypad input.")]
        [SerializeField] private float zoomSettleTime = 0.6f;

        // Fired when the player presses the final Confirm button on the success screen.
        public event System.Action OnPaymentComplete;

        private string enteredAmount  = string.Empty;
        private int    requiredAmount;
        private bool   inputEnabled;

        private void Awake()
        {
            entryPanel?.SetActive(false);
            successPanel?.SetActive(false);

            confirmEntryButton?.onClick.AddListener(HandleEntryConfirm);
            finalConfirmButton?.onClick.AddListener(HandleFinalConfirm);

            if (zoomCamera != null)
            {
                zoomCamera.Priority = defaultPriority;

                // The camera GameObject must be ENABLED at all times.
                // Cinemachine's priority system selects the dominant camera — do NOT deactivate it.
                // Set the GameObject active and leave priority management to Open() / Close().
                if (!zoomCamera.gameObject.activeInHierarchy)
                    Debug.LogWarning($"[PaymentTerminal] zoomCamera '{zoomCamera.name}' is inactive. " +
                        "Enable its GameObject and let Cinemachine priority control when it takes over.");
            }
        }

        // Opens the terminal, zooms the camera in, and prepares the keypad for the given amount.
        // Called by CheckoutCounter.BeginCardPayment().
        public void Open(int amount)
        {
            requiredAmount = amount;
            enteredAmount  = string.Empty;
            inputEnabled   = false;

            entryPanel?.SetActive(true);
            successPanel?.SetActive(false);
            RefreshDisplay();

            if (zoomCamera != null) zoomCamera.Priority = zoomedPriority;

            // Give the camera blend time to finish before accepting input.
            DOVirtual.DelayedCall(zoomSettleTime, () => inputEnabled = true);
        }

        // Hides both panels and zooms the camera back out.
        // Called internally after the final confirm, and by CheckoutCounter on early exit.
        public void Close()
        {
            inputEnabled = false;
            entryPanel?.SetActive(false);
            successPanel?.SetActive(false);

            if (zoomCamera != null) zoomCamera.Priority = defaultPriority;
        }

        // Appends a digit, or "back" to delete the last character.
        // Deliberately ignores "." — yen is integer only.
        // Wire each numpad button's onClick to call this method with the button's digit string.
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
        }

        // Programmatically triggers the active confirm action.
        // Use this from a CardReaderButton (buttonInput = "confirm") placed over the green button on the mesh.
        // If the success panel is showing, presses the Final Confirm; otherwise presses the Entry Confirm.
        public void TryConfirm()
        {
            if (successPanel != null && successPanel.activeSelf)
                HandleFinalConfirm();
            else
                HandleEntryConfirm();
        }

        private void HandleEntryConfirm()
        {
            if (!inputEnabled) return;

            if (!int.TryParse(enteredAmount, out int amount))
            {
                Debug.LogWarning("[PaymentTerminal] Could not parse entered amount.");
                return;
            }

            // TODO: Implement consequences for incorrect amount (amount != requiredAmount).
            // For now, any confirmed entry advances to the success screen.

            inputEnabled = false;
            entryPanel?.SetActive(false);
            successPanel?.SetActive(true);

            if (successText != null)
                successText.text = $"Payment Confirmed\n¥{requiredAmount:N0}";
        }

        private void HandleFinalConfirm()
        {
            Close();
            OnPaymentComplete?.Invoke();
        }

        private void RefreshDisplay()
        {
            if (displayText == null) return;
            displayText.text = string.IsNullOrEmpty(enteredAmount)
                ? "¥0"
                : $"¥{int.Parse(enteredAmount):N0}";
        }
    }
}
