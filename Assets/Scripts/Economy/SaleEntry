using System;

namespace AsakuShop.Economy
{
    // Immutable record of a single item sale transaction.
    [Serializable]
    public class SaleEntry
    {
        //ItemDefinition.ItemId of the item sold.
        public string ItemId;

        //Human-readable display name at time of sale.
        public string DisplayName;

        //The price the customer actually paid (yen).
        public int SalePrice;

        //The item's wholesale/acquisition cost (yen), from ItemDefinition.BasePrice rounded.
        public int BasePrice;

        //Profit margin on this single unit: SalePrice - BasePrice.
        public int Margin => SalePrice - BasePrice;

        //Wall-clock timestamp when the sale occurred.
        public string TimestampIso; // stored as ISO string for JsonUtility compat

        public SaleEntry(string itemId, string displayName, int salePrice, int basePrice)
        {
            ItemId       = itemId;
            DisplayName  = displayName;
            SalePrice    = salePrice;
            BasePrice    = basePrice;
            TimestampIso = DateTime.Now.ToString("o");
        }

        public override string ToString()
            => $"[{DisplayName} | Sold: ¥{SalePrice:N0} | Cost: ¥{BasePrice:N0} | Margin: ¥{Margin:N0}]";
    }
}
