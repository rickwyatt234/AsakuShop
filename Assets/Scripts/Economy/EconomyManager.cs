using System;
using System.Collections.Generic;
using UnityEngine;
using AsakuShop.Core;
using AsakuShop.Items;

namespace AsakuShop.Economy
{
    // Singleton that tracks the player's yen balance and daily financial ledgers.
    // All money is whole yen (int). No fractions.
    // Implements ISaveParticipant — registered with SaveManager automatically.
    public class EconomyManager : MonoBehaviour, ISaveParticipant
    {
        public static EconomyManager Instance { get; private set; }

        // Registers the bootstrap factory with GameBootstrapper before any scene loads.
        // This avoids a cyclic assembly reference between AsakuShop.Core and AsakuShop.Economy.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void RegisterBootstrapper()
        {
            GameBootstrapper.RegisterBootstrapper(() =>
            {
                if (Instance != null) return;
                GameObject go = new GameObject("[EconomyManager]");
                go.AddComponent<EconomyManager>();
                // DontDestroyOnLoad is called inside EconomyManager.Awake().
            });
        }

        // ── Balance ──────────────────────────────────────────────────────────
        [SerializeField] private int startingBalance = 50000;
        private int balance;

        /// <summary>Player's current yen balance.</summary>
        public int Balance
        {
            get => balance;
            private set => balance = Mathf.Max(0, value);
        }

        // ── Ledgers ──────────────────────────────────────────────────────────
        private List<DailyLedger> ledgers = new List<DailyLedger>();

        /// <summary>Read-only view of all completed day ledgers.</summary>
        public IReadOnlyList<DailyLedger> Ledgers => ledgers;

        /// <summary>The active ledger for today.</summary>
        public DailyLedger Today { get; private set; }

        // ── ISaveParticipant ──────────────────────────────────────────────────
        public string SaveKey => "economy";

#region Unity Lifecycle
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            balance = startingBalance;
            Today   = new DailyLedger(0);

            // Register so SaveManager captures and restores economy state on save/load.
            SaveManager.Instance?.Register(this);
        }

        private void OnEnable()
        {
            CoreEvents.OnDayStarted += StartNewDay;
        }

        private void OnDisable()
        {
            CoreEvents.OnDayStarted -= StartNewDay;
        }
#endregion


#region Public API

        // Records the sale of one item instance. Adds salePrice to the balance,
        // records a SaleEntry in today's ledger, and fires CoreEvents.OnItemSold.
        public void RecordSale(ItemInstance item, int salePrice)
        {
            if (item == null) return;
            salePrice = Mathf.Max(0, salePrice);

            int basePriceYen = Mathf.RoundToInt(item.Definition.BasePrice);
            var entry = new SaleEntry(
                item.Definition.ItemId,
                item.Definition.DisplayName,
                salePrice,
                basePriceYen
            );

            Today.RecordSale(entry);
            Balance += salePrice;

            CoreEvents.RaiseItemSold(entry);
        }

        // Deducts money from the balance and records it as a spend in today's ledger.
        public void RecordSpend(int amount)
        {
            if (amount <= 0) return;
            Balance -= amount;
            Today?.RecordSpend(amount);
        }

        // Seals the current day's ledger and starts a fresh one for the new day.
        // Called automatically by CoreEvents.OnDayStarted.
        public void StartNewDay(int dayIndex)
        {
            if (Today != null)
            {
                ledgers.Add(Today);
                Debug.Log($"[EconomyManager] Day {Today.DayIndex} sealed.\n{Today}");
            }
            Today = new DailyLedger(dayIndex);
        }
#endregion


#region ISaveParticipant Implementation

        [Serializable]
        private class EconomySaveData
        {
            public int Balance;
            public List<DailyLedger> Ledgers;
            public DailyLedger Today;
        }

        public object CaptureState()
        {
            return new EconomySaveData
            {
                Balance = this.balance,
                Ledgers = new List<DailyLedger>(ledgers),
                Today   = this.Today
            };
        }

        public void RestoreState(string json)
        {
            if (string.IsNullOrEmpty(json)) return;
            var data = JsonUtility.FromJson<EconomySaveData>(json);
            if (data == null) return;

            // Use the property setter so the Mathf.Max(0, value) clamp is applied.
            Balance = data.Balance;
            ledgers = data.Ledgers ?? new List<DailyLedger>();
            Today   = data.Today   ?? new DailyLedger(0);
        }
#endregion


#region Debugging
        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(0, 80, 300, 200));
            GUILayout.Label($"Balance: ¥{Balance}");
            GUILayout.EndArea();
        }
#endregion
    }

}

