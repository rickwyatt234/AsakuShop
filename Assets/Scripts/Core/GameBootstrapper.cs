using System;
using System.Collections.Generic;
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

#region External manager bootstrappers
        // Other assemblies (Economy, Store, …) register a factory here via
        // [RuntimeInitializeOnLoadMethod(SubsystemRegistration)] so that
        // GameBootstrapper can create their singletons in the correct order
        // without introducing cyclic assembly references.
        private static readonly List<Action> _bootstrappers = new List<Action>();

        public static void RegisterBootstrapper(Action bootstrapper)
        {
            if (bootstrapper != null)
                _bootstrappers.Add(bootstrapper);
        }
#endregion

#region Unity methods
        private void Awake()
        {
            EnsureClock();
            EnsureStateController();
            EnsureSaveManager();
            EnsureExternalManagers();

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

        private static void EnsureExternalManagers()
        {
            foreach (Action bootstrapper in _bootstrappers)
                bootstrapper();
        }
#endregion
    }
}
