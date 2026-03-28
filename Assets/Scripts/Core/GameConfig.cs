using UnityEngine;

namespace AsakuShop.Core
{
    public class GameConfig : ScriptableObject
    {
#region Singleton pattern
        private static GameConfig _instance;

        public static GameConfig Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = Resources.Load<GameConfig>("GameConfig");

                    if (_instance == null)
                    {
                        Debug.LogError("GameConfig not found in Resources!\nPlease create one using Tools > Checkout Frenzy > Game Config.");
                    }
                }
                return _instance;
            }
        }
#endregion


#region Configurable fields
        [Header("Store Settings")]
        [SerializeField, Tooltip("The default name for the store.")]
        private string defaultStoreName = "AsakuShop!";

        [SerializeField, Tooltip("The maximum number of characters allowed for the store name.")]
        private int storeNameMaxCharacters = 15;

        [SerializeField, Tooltip("The initial amount of money the player starts with.")]
        private int startingMoney = 2000;

        [SerializeField, Tooltip("The minimum time (in seconds) between customer spawns.")]
        private float minSpawnTime = 5f;

        [SerializeField, Tooltip("The maximum time (in seconds) between customer spawns.")]
        private float maxSpawnTime = 15f;

        [SerializeField, Tooltip("The base maximum number of customers that can be in the store at the same time.")]
        private int baseMaxCustomers = 5;



        [Header("Level System")]
        [SerializeField, Tooltip("Base experience required for level 1.")]
        private int baseExperience = 5;

        [SerializeField, Tooltip("Exponential growth factor for experience requirements.")]
        private float growthFactor = 1.1f;



        [Header("Customer Dialogues")]
        [SerializeField, Tooltip("Dialogue lines used when the customer can't find a product.")]
        private Dialogue notFoundDialogue;

        [SerializeField, Tooltip("Dialogue lines used when the customer thinks a product is too expensive.")]
        private Dialogue overpricedDialogue;
#endregion




#region Properties
        public string DefaultStoreName => defaultStoreName;
        public int StoreNameMaxCharacters => storeNameMaxCharacters;
        public int StartingMoney => startingMoney;
        public float GetRandomSpawnTime => Random.Range(minSpawnTime, maxSpawnTime);
        public int BaseMaxCustomers => baseMaxCustomers;

        public int BaseExperience => baseExperience;
        public float GrowthFactor => growthFactor;

        public Dialogue NotFoundDialogue => notFoundDialogue;
        public Dialogue OverpricedDialogue => overpricedDialogue;
#endregion
    }
}