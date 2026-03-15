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
#endregion

#region Unity methods
        private void Awake()
        {
            EnsureClock();
            EnsureStateController();
            EnsureSaveManager();
            if (State.Clock == null)
                State.Clock = Clock;

            if (State.CurrentPhase == GamePhase.Boot)
                State.RequestTransition(GamePhase.MainMenu);

            //Lock mouse cursor
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void Start()
        {
            //Switch to main scene
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
#endregion
    }
}