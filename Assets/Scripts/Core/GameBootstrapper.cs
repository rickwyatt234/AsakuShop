using UnityEngine;

namespace AsakuShop.Core
{
    /// Entry point that ensures the core singleton systems exist and are wired
    /// together before any other script runs. Place on a GameObject in a
    /// "Bootstrap" scene or on a persistent prefab.
    [DefaultExecutionOrder(-1000)]
    public class GameBootstrapper : MonoBehaviour
    {
#region Static instances
        public static GameClock Clock { get; private set; }
        public static GameStateController State { get; private set; }
        public static SaveManager Save { get; private set; }
        public static AsakuShop.Economy.EconomyManager Economy { get; private set; }
        public static AsakuShop.Store.StoreManager Store { get; private set; }
#endregion

#region Unity methods
        private void Awake()
        {
            EnsureClock();
            EnsureStateController();
            EnsureSaveManager();
            EnsureEconomyManager();
            EnsureStoreManager();

            if (State.Clock == null)
                State.Clock = Clock;

            if (State.CurrentPhase == GamePhase.Boot)
                State.RequestTransition(GamePhase.MainMenu);

            // Lock mouse cursor
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void Start()
        {
            // Switch to main scene
            SceneLoader.LoadScene("MainScene");
        }
#endregion

#region Systems initialization
        private static void EnsureClock()
        {
            if (GameClock.Instance != null)
            {
                Clock = GameClock.Instance;
                return;
            }

            GameObject go = new GameObject("[GameClock]");
            Clock = go.AddComponent<GameClock>();
            // DontDestroyOnLoad is called inside GameClock.Awake().
        }

        private static void EnsureStateController()
        {
            if (GameStateController.Instance != null)
            {
                State = GameStateController.Instance;
                return;
            }

            GameObject go = new GameObject("[GameStateController]");
            State = go.AddComponent<GameStateController>();
            // DontDestroyOnLoad is called inside GameStateController.Awake().
        }

        private static void EnsureSaveManager()
        {
            if (SaveManager.Instance != null)
            {
                Save = SaveManager.Instance;
                return;
            }

            GameObject go = new GameObject("[SaveManager]");
            Save = go.AddComponent<SaveManager>();
            // DontDestroyOnLoad is called inside SaveManager.Awake().
        }

        private static void EnsureEconomyManager()
        {
            if (AsakuShop.Economy.EconomyManager.Instance != null)
            {
                Economy = AsakuShop.Economy.EconomyManager.Instance;
                return;
            }

            GameObject go = new GameObject("[EconomyManager]");
            Economy = go.AddComponent<AsakuShop.Economy.EconomyManager>();
            // DontDestroyOnLoad is called inside EconomyManager.Awake().
        }

        private static void EnsureStoreManager()
        {
            if (AsakuShop.Store.StoreManager.Instance != null)
            {
                Store = AsakuShop.Store.StoreManager.Instance;
                return;
            }

            GameObject go = new GameObject("[StoreManager]");
            Store = go.AddComponent<AsakuShop.Store.StoreManager>();
            // DontDestroyOnLoad is called inside StoreManager.Awake().
        }
#endregion
    }
}
