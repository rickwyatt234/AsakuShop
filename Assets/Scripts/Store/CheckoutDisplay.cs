using UnityEngine;
using TMPro;
using System.Collections.Generic;
using AsakuShop.Items;

namespace AsakuShop.Store
{
    public class CheckoutDisplay : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI totalPriceDisplay;
        [SerializeField] private TextMeshProUGUI itemListDisplay;
        [SerializeField] private TextMeshProUGUI messageDisplay;

        private void Start()
        {
            ClearDisplay();
        }

        public void UpdateDisplay(List<ItemInstance> items, float total)
        {
            // Update total
            if (totalPriceDisplay != null)
                totalPriceDisplay.text = $"Total: ¥{total:F0}";

            // Update item list
            if (itemListDisplay != null)
            {
                string itemText = "Items:\n";
                foreach (var item in items)
                {
                    itemText += $"- {item.Definition.DisplayName}\n";
                }
                itemListDisplay.text = itemText;
            }

            // Clear any previous messages
            if (messageDisplay != null)
                messageDisplay.text = "";
        }

        public void ShowReceiptMessage(string message)
        {
            if (messageDisplay != null)
                messageDisplay.text = message;
        }

        private void ClearDisplay()
        {
            if (totalPriceDisplay != null)
                totalPriceDisplay.text = "Total: ¥0";
            if (itemListDisplay != null)
                itemListDisplay.text = "Items:\n";
            if (messageDisplay != null)
                messageDisplay.text = "";
        }
    }
}