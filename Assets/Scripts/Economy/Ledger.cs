using UnityEngine;
using AsakuShop.Items;
using System.Collections.Generic;
using System;

namespace AsakuShop.Economy
{
    // Ledger system to track all transactions (sales, expenses, refunds) 
    // for analytics and reporting.
    public class Ledger : MonoBehaviour
    {
        private static Ledger _instance;
        public static Ledger Instance
        {
            get
            {
                if (_instance == null)
                {
                    GameObject go = new GameObject("[Ledger]");
                    _instance = go.AddComponent<Ledger>();
                }
                return _instance;
            }
        }

        [System.Serializable]
        public struct Transaction
        {
            public enum TransactionType { Sale, Expense, Refund }

            public TransactionType type;
            public float amount;
            public string itemId;
            public string description;
            public float timestamp; // Time.time at transaction

            public Transaction(TransactionType type, float amount, string itemId, string description)
            {
                this.type = type;
                this.amount = amount;
                this.itemId = itemId;
                this.description = description;
                this.timestamp = Time.time;
            }
        }

        private List<Transaction> transactions = new();

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
#region Transaction Recording Overloads
        public void RecordSale(ItemInstance item, float price)
        {
            var transaction = new Transaction(
                Transaction.TransactionType.Sale,
                price,
                item?.Definition?.ItemId ?? "unknown",
                $"Sold {item?.Definition?.DisplayName ?? "Unknown Item"}"
            );
            transactions.Add(transaction);
            Debug.Log($"[LEDGER] Sale: {item?.Definition?.DisplayName} = ¥{price}");
        }

        public void RecordSale(List<ItemInstance> items, float totalPrice)
        {
            string itemNames = "";
            foreach (var item in items)
            {
                itemNames += item?.Definition?.DisplayName + ", ";
            }
            itemNames = itemNames.TrimEnd(',', ' ');

            var transaction = new Transaction(
                Transaction.TransactionType.Sale,
                totalPrice,
                "multi_item",
                $"Sold: {itemNames}"
            );
            transactions.Add(transaction);
            Debug.Log($"[LEDGER] Sale: {itemNames} = ¥{totalPrice}");
        }
        public void RecordExpense(string itemId, float cost, string description)
        {
            var transaction = new Transaction(Transaction.TransactionType.Expense, cost, itemId, description);
            transactions.Add(transaction);
            Debug.Log($"[LEDGER] Expense: {description} = ¥{cost}");
        }

        public void RecordRefund(ItemInstance item, float price)
        {
            var transaction = new Transaction(
                Transaction.TransactionType.Refund,
                price,
                item?.Definition?.ItemId ?? "unknown",
                $"Refunded {item?.Definition?.DisplayName ?? "Unknown Item"}"
            );
            transactions.Add(transaction);
            Debug.Log($"[LEDGER] Refund: {item?.Definition?.DisplayName} = ¥{price}");
        }
#endregion

        public float GetTotalSales()
        {
            float total = 0;
            foreach (var transaction in transactions)
            {
                if (transaction.type == Transaction.TransactionType.Sale)
                    total += transaction.amount;
            }
            return total;
        }

        public float GetTotalExpenses()
        {
            float total = 0;
            foreach (var transaction in transactions)
            {
                if (transaction.type == Transaction.TransactionType.Expense)
                    total += transaction.amount;
            }
            return total;
        }

        public float GetNetProfit() => GetTotalSales() - GetTotalExpenses();

        public List<Transaction> GetAllTransactions() => new(transactions);

        public void PrintLedger()
        {
            Debug.Log("=== LEDGER ===");
            foreach (var t in transactions)
            {
                Debug.Log($"[{t.type}] {t.description} = ¥{t.amount}");
            }
            Debug.Log($"Total Sales: ¥{GetTotalSales()}");
            Debug.Log($"Total Expenses: ¥{GetTotalExpenses()}");
            Debug.Log($"Net Profit: ¥{GetNetProfit()}");
        }
    }
}