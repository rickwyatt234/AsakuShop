using UnityEngine;
using System.Collections.Generic;
using AsakuShop.Items;
using AsakuShop.Economy;

namespace AsakuShop.Store
{
    public class CheckoutTerminal : MonoBehaviour
    {
        [SerializeField] private Transform scannerPoint; // Where player scans items
        [SerializeField] private CheckoutDisplay display; // World-space UI

        private List<ItemInstance> scannedItems = new();
        private float runningTotal = 0f;

        public delegate void ItemScannedEvent(ItemInstance item, float newTotal);
        public event ItemScannedEvent OnItemScanned;

        public delegate void PaymentProcessedEvent(float amount);
        public event PaymentProcessedEvent OnPaymentProcessed;

        private void Start()
        {
            if (display == null)
                display = GetComponentInChildren<CheckoutDisplay>();
        }

        public bool TryScanItem(ItemInstance item)
        {
            if (item == null)
                return false;

            float itemPrice = PriceManager.Instance.GetSellPrice(item);
            scannedItems.Add(item);
            runningTotal += itemPrice;

            OnItemScanned?.Invoke(item, runningTotal);
            display?.UpdateDisplay(scannedItems, runningTotal);

            // Fire the event so customers know this item was scanned
            CheckoutEvents.FireItemScanned(item);

            Debug.Log($"[CHECKOUT] Scanned: {item.Definition.DisplayName} = ¥{itemPrice} | Total: ¥{runningTotal}");
            return true;
        }

        public bool TryProcessPayment()
        {
            if (scannedItems.Count == 0)
            {
                Debug.LogWarning("No items scanned for payment");
                return false;
            }

            if (!Wallet.Instance.CanAfford(runningTotal))
            {
                Debug.LogWarning($"Customer cannot afford ¥{runningTotal}");
                return false;
            }

            // Process payment
            Wallet.Instance.AddMoney(runningTotal);
            Ledger.Instance.RecordSale(scannedItems, runningTotal);

            OnPaymentProcessed?.Invoke(runningTotal);
            display?.ShowReceiptMessage($"Payment complete: ¥{runningTotal}");

            Debug.Log($"[CHECKOUT] Payment processed: ¥{runningTotal}");

            // Clear terminal
            ClearTerminal();
            return true;
        }

        public void ClearTerminal()
        {
            scannedItems.Clear();
            runningTotal = 0f;
            display?.UpdateDisplay(scannedItems, runningTotal);
        }

        public Transform GetScannerPoint() => scannerPoint;
        public float GetRunningTotal() => runningTotal;
        public List<ItemInstance> GetScannedItems() => new(scannedItems);
    }
}