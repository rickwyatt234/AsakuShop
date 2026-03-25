using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AsakuShop.Economy
{
    // Accumulates all financial activity for a single in-game day.
    // All values are in whole yen (int).
    [Serializable]
    public class DailyLedger
    {
        [SerializeField] private int dayIndex;
        [SerializeField] private List<SaleEntry> sales = new List<SaleEntry>();
        [SerializeField] private int totalSpend;

        public int DayIndex         => dayIndex;
        public IReadOnlyList<SaleEntry> Sales => sales;

        //Total yen received from customers today.
        public int TotalRevenue     => sales.Sum(s => s.SalePrice);

        //Total cost of goods sold today (what the player paid for each item)
        public int TotalCogs        => sales.Sum(s => s.BasePrice);

        //Revenue minus cost of goods.
        public int GrossProfit      => TotalRevenue - TotalCogs;

        //Money the player spent today (purchases, upgrades, etc.).
        public int TotalSpend       => totalSpend;

        //Gross profit minus total spend.
        public int NetProfit        => GrossProfit - totalSpend;

        //Number of individual items sold.
        public int TransactionCount => sales.Count;

        //DisplayName of the most-sold item by quantity. Empty string if no sales.
        public string MostSoldItem
        {
            get
            {
                if (sales.Count == 0) return string.Empty;
                return sales
                    .GroupBy(s => s.DisplayName)
                    .OrderByDescending(g => g.Count())
                    .First().Key;
            }
        }

        public DailyLedger(int dayIndex)
        {
            this.dayIndex = dayIndex;
        }

        //Record a completed item sale.
        public void RecordSale(SaleEntry entry)
        {
            if (entry == null) return;
            sales.Add(entry);
        }

        //Record money the player spent today (deducted from net profit).
        public void RecordSpend(int amount)
        {
            if (amount <= 0) return;
            totalSpend += amount;
        }

        public override string ToString()
        {
            return $"=== Day {dayIndex} Ledger ===\n" +
                   $"  Transactions : {TransactionCount}\n" +
                   $"  Revenue      : ¥{TotalRevenue:N0}\n" +
                   $"  COGS         : ¥{TotalCogs:N0}\n" +
                   $"  Gross Profit : ¥{GrossProfit:N0}\n" +
                   $"  Spent        : ¥{TotalSpend:N0}\n" +
                   $"  Net Profit   : ¥{NetProfit:N0}\n" +
                   $"  Best Seller  : {(string.IsNullOrEmpty(MostSoldItem) ? "N/A" : MostSoldItem)}";
        }
    }
}
