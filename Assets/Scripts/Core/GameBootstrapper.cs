using UnityEngine;

namespace AsakuShop.Core
{
    /// <summary>
    /// Entry point that ensures the core singleton systems exist and are wired
    /// together before any other script runs. Place on a GameObject in a
    /// "Bootstrap" scene or on a persistent prefab.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>[DefaultExecutionOrder(-1000)]</c> guarantees this runs before all
    /// other scripts in the project (other core scripts use -900).
    /// </para>
    /// <para>
    /// If the singletons already exist (e.g. after a scene reload), the guard
    /// checks prevent duplicate creation.
    /// </para>
    /// </remarks>
    [DefaultExecutionOrder(-1000)]
    public class GameBootstrapper : MonoBehaviour
    {
        // ── Static accessors ─────────────────────────────────────────────────────

        /// <summary>Reference to the singleton <see cref="GameClock"/>.</summary>
        public static GameClock Clock { get; private set; }

        /// <summary>Reference to the singleton <see cref="GameStateController"/>.</summary>
        public static GameStateController State { get; private set; }

        /// <summary>Reference to the singleton <see cref="SaveManager"/>.</summary>
        public static SaveManager Save { get; private set; }

        // ── Unity lifecycle ──────────────────────────────────────────────────────

        private void Awake()
        {
            EnsureClock();
            EnsureStateController();
            EnsureSaveManager();

            // Wire the clock reference into the state controller if not already set.
            if (State.Clock == null)
                State.Clock = Clock;

            // Kick off the initial phase transition: Boot → MainMenu.
            if (State.CurrentPhase == GamePhase.Boot)
                State.RequestTransition(GamePhase.MainMenu);
        }

        // ── Private helpers ──────────────────────────────────────────────────────

        /// <summary>
        /// Creates a persistent <see cref="GameClock"/> GameObject if one does
        /// not already exist; assigns <see cref="Clock"/>.
        /// </summary>
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

        /// <summary>
        /// Creates a persistent <see cref="GameStateController"/> GameObject if
        /// one does not already exist; assigns <see cref="State"/>.
        /// </summary>
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

        /// <summary>
        /// Creates a persistent <see cref="SaveManager"/> GameObject if one does
        /// not already exist; assigns <see cref="Save"/>.
        /// </summary>
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
    }
}
