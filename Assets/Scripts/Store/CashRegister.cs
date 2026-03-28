using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.UI;

namespace AsakuShop.Store
{
    // Manages the cash payment flow:
    //   1. Camera zooms in on the cash register.
    //   2. Player clicks denomination drawers (CashDrawerButton) to build up change.
    //   3. When given change equals owed change (or no change is owed), the Confirm button activates.
    //   4. Player presses Confirm → camera zooms out → OnPaymentComplete fires.
    //
    // All amounts are whole yen (int). No fractions, no decimals.
    public class CashRegister : MonoBehaviour
    {
        [Header("Camera")]
        [SerializeField, Tooltip("Cinemachine virtual camera that zooms in on the cash register.")]
        private CinemachineCamera zoomCamera;
        [SerializeField] private int zoomedPriority  = 20;
        [SerializeField] private int defaultPriority = 0;

        [Header("UI")]
        [SerializeField] private GameObject registerPanel;
        [SerializeField] private TMP_Text   changeOwedText;
        [SerializeField] private TMP_Text   changeGivenText;
        [SerializeField] private Button     undoButton;
        [SerializeField] private Button     clearButton;
        [SerializeField] private Button     confirmButton;

        [Header("Timing")]
        [Tooltip("Seconds to wait after the camera cut before enabling drawer input.")]
        [SerializeField] private float zoomSettleTime = 0.6f;

        // Fired when the player presses Confirm with the correct change (or no change owed).
        public event System.Action OnPaymentComplete;

        private int               changeOwed;
        private int               changeGiven;
        private bool              allowDrawing;
        private readonly Stack<int> drawHistory = new Stack<int>();

        private void Awake()
        {
            registerPanel?.SetActive(false);

            undoButton?.onClick.AddListener(HandleUndo);
            clearButton?.onClick.AddListener(HandleClear);
            confirmButton?.onClick.AddListener(HandleConfirm);

            if (zoomCamera != null) zoomCamera.Priority = defaultPriority;
        }

        // Opens the register, zooms the camera in, and sets up the change to count out.
        // changeOwedAmount is (customerPayment – totalPrice). Pass 0 if no change is due.
        // Called by CheckoutCounter.BeginCashPayment().
        public void Open(int changeOwedAmount)
        {
            changeOwed   = Mathf.Max(0, changeOwedAmount);
            changeGiven  = 0;
            allowDrawing = false;
            drawHistory.Clear();

            registerPanel?.SetActive(true);
            UpdateDisplay();
            RefreshConfirmButton();

            if (zoomCamera != null) zoomCamera.Priority = zoomedPriority;

            // If no change is owed the confirm button is already active; still wait for the
            // camera blend to finish so the player knows what they're looking at.
            DOVirtual.DelayedCall(zoomSettleTime, () => allowDrawing = true);
        }

        // Hides the panel and zooms the camera back out.
        // Called internally after Confirm, and by CheckoutCounter on early exit.
        public void Close()
        {
            allowDrawing = false;
            registerPanel?.SetActive(false);

            if (zoomCamera != null) zoomCamera.Priority = defaultPriority;
        }

        // Called by CashDrawerButton when the player interacts with a denomination drawer.
        public void Draw(int amount)
        {
            if (!allowDrawing || amount <= 0) return;

            changeGiven += amount;
            drawHistory.Push(amount);

            UpdateDisplay();
            RefreshConfirmButton();
        }

        private void HandleUndo()
        {
            if (!allowDrawing || drawHistory.Count == 0) return;

            changeGiven -= drawHistory.Pop();
            changeGiven  = Mathf.Max(0, changeGiven);

            UpdateDisplay();
            RefreshConfirmButton();
        }

        private void HandleClear()
        {
            if (!allowDrawing) return;

            changeGiven = 0;
            drawHistory.Clear();

            UpdateDisplay();
            RefreshConfirmButton();
        }

        private void HandleConfirm()
        {
            if (!allowDrawing) return;

            // Guard: if change is owed, the given amount must exactly match.
            if (changeOwed > 0 && changeGiven != changeOwed) return;

            Close();
            OnPaymentComplete?.Invoke();
        }

        private void UpdateDisplay()
        {
            if (changeOwedText  != null) changeOwedText.text  = $"Change Owed: ¥{changeOwed:N0}";
            if (changeGivenText != null) changeGivenText.text = $"Given: ¥{changeGiven:N0}";
        }

        private void RefreshConfirmButton()
        {
            if (confirmButton == null) return;
            // Active when no change is needed, or when the player has counted out the exact amount.
            confirmButton.interactable = (changeOwed == 0 || changeGiven == changeOwed);
        }
    }
}
