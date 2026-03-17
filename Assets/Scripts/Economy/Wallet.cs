using UnityEngine;
using AsakuShop.Items;

namespace AsakuShop.Economy
{
    public class Wallet : MonoBehaviour
    {
        private static Wallet _instance;
        public static Wallet Instance
        {
            get
            {
                if (_instance == null)
                {
                    GameObject go = new GameObject("[Wallet]");
                    _instance = go.AddComponent<Wallet>();
                }
                return _instance;
            }
        }

        [SerializeField] private float initialBalance = 5000f;
        private float currentBalance;

        public delegate void BalanceChangedEvent(float newBalance);
        public event BalanceChangedEvent OnBalanceChanged;

        private void Awake()
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
            currentBalance = initialBalance;
        }

        public float GetBalance() => currentBalance;

        public bool CanAfford(float cost) => currentBalance >= cost;

        public bool TrySpendMoney(float amount)
        {
            if (!CanAfford(amount))
                return false;

            currentBalance -= amount;
            OnBalanceChanged?.Invoke(currentBalance);
            Debug.Log($"Spent ¥{amount}. Balance: ¥{currentBalance}");
            return true;
        }

        public void AddMoney(float amount)
        {
            currentBalance += amount;
            OnBalanceChanged?.Invoke(currentBalance);
            Debug.Log($"Earned ¥{amount}. Balance: ¥{currentBalance}");
        }

        public void SetBalance(float amount)
        {
            currentBalance = amount;
            OnBalanceChanged?.Invoke(currentBalance);
        }
    }
}