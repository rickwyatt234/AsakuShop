using UnityEngine;

namespace AsakuShop.Core
{
    /// <summary>
    /// Owns and manages the current <see cref="GamePhase"/>. Persists across
    /// scene loads. All code that wants to change the game phase must call
    /// <see cref="RequestTransition"/> so that legality can be enforced and
    /// the appropriate events fired.
    /// </summary>
    [DefaultExecutionOrder(-900)]
    public class GameStateController : MonoBehaviour
    {
        // ── Singleton ────────────────────────────────────────────────────────────

        /// <summary>Singleton instance of the <see cref="GameStateController"/>.</summary>
        public static GameStateController Instance { get; private set; }

        // ── Public state ─────────────────────────────────────────────────────────

        /// <summary>The currently active <see cref="GamePhase"/> (read-only).</summary>
        public GamePhase CurrentPhase { get; private set; } = GamePhase.Boot;

        // ── Inspector wiring ─────────────────────────────────────────────────────

        /// <summary>
        /// Reference to the <see cref="GameClock"/> that this controller drives.
        /// Populated automatically by <see cref="GameBootstrapper"/> if not set.
        /// </summary>
        [Tooltip("GameClock whose ClockPaused flag is managed by this controller.")]
        public GameClock Clock;

        // ── Private state ─────────────────────────────────────────────────────────

        /// <summary>
        /// The phase that was active just before <see cref="GamePhase.Paused"/>
        /// was entered; used by <see cref="ResumeGame"/>.
        /// </summary>
        private GamePhase _prePausePhase = GamePhase.Boot;

        // ── Unity lifecycle ──────────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogError("[GameStateController] Duplicate instance detected — destroying self.");
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        // ── Public API ───────────────────────────────────────────────────────────

        /// <summary>
        /// Attempts to transition to <paramref name="newPhase"/>. If the
        /// transition is not legal from the current phase, a warning is logged and
        /// the request is silently ignored.
        /// </summary>
        /// <param name="newPhase">The desired next <see cref="GamePhase"/>.</param>
        public void RequestTransition(GamePhase newPhase)
        {
            if (!IsLegalTransition(CurrentPhase, newPhase))
            {
                Debug.LogWarning(
                    $"[GameStateController] Illegal transition: {CurrentPhase} → {newPhase}. Request ignored.");
                return;
            }

            GamePhase previous = CurrentPhase;
            CurrentPhase = newPhase;

            UpdateClockPaused();
            CoreEvents.RaisePhaseChanged(previous, CurrentPhase);
        }

        /// <summary>
        /// Pauses the game: remembers the current phase, then immediately enters
        /// <see cref="GamePhase.Paused"/>. Bypasses the legal-transition check
        /// because the player can pause from any phase at any time.
        /// </summary>
        public void PauseGame()
        {
            if (CurrentPhase == GamePhase.Paused) return;

            _prePausePhase = CurrentPhase;
            GamePhase previous = CurrentPhase;
            CurrentPhase = GamePhase.Paused;

            UpdateClockPaused();
            CoreEvents.RaisePhaseChanged(previous, CurrentPhase);
        }

        /// <summary>
        /// Resumes the game from a paused state by restoring the phase that was
        /// active before <see cref="PauseGame"/> was called.
        /// </summary>
        public void ResumeGame()
        {
            if (CurrentPhase != GamePhase.Paused)
            {
                Debug.LogWarning("[GameStateController] ResumeGame called but game is not paused.");
                return;
            }

            GamePhase previous = CurrentPhase;
            CurrentPhase = _prePausePhase;

            UpdateClockPaused();
            CoreEvents.RaisePhaseChanged(previous, CurrentPhase);
        }

        /// <summary>
        /// Returns <c>true</c> when the in-game clock should be running (i.e. the
        /// current phase is not a phase that freezes time).
        /// </summary>
        /// <returns>
        /// <c>true</c> when time should advance; <c>false</c> for
        /// <see cref="GamePhase.Paused"/>, <see cref="GamePhase.CraftingMenu"/>,
        /// <see cref="GamePhase.Boot"/>, <see cref="GamePhase.MainMenu"/>,
        /// <see cref="GamePhase.Sleep"/>, and <see cref="GamePhase.EndOfDaySummary"/>.
        /// </returns>
        public bool IsClockRunning()
        {
            switch (CurrentPhase)
            {
                case GamePhase.Paused:
                case GamePhase.CraftingMenu:
                case GamePhase.Boot:
                case GamePhase.MainMenu:
                case GamePhase.Sleep:
                case GamePhase.EndOfDaySummary:
                    return false;
                default:
                    return true;
            }
        }

        // ── Private helpers ──────────────────────────────────────────────────────

        /// <summary>
        /// Syncs <see cref="GameClock.ClockPaused"/> to the current phase.
        /// The clock is paused for <see cref="GamePhase.Paused"/> and
        /// <see cref="GamePhase.CraftingMenu"/> only. Other non-running phases
        /// (Boot, MainMenu, Sleep, EndOfDaySummary) leave clock management to
        /// those systems' own logic.
        /// </summary>
        private void UpdateClockPaused()
        {
            if (Clock == null) return;

            Clock.ClockPaused = CurrentPhase == GamePhase.Paused
                             || CurrentPhase == GamePhase.CraftingMenu;
        }

        /// <summary>
        /// Returns <c>true</c> when the transition from <paramref name="from"/>
        /// to <paramref name="to"/> is listed in the legal transition table.
        /// </summary>
        private static bool IsLegalTransition(GamePhase from, GamePhase to)
        {
            switch (from)
            {
                case GamePhase.Boot:
                    return to == GamePhase.MainMenu;

                case GamePhase.MainMenu:
                    return to == GamePhase.ChooseWakeTime;

                case GamePhase.ChooseWakeTime:
                    return to == GamePhase.TravelToMarket
                        || to == GamePhase.TravelToStore
                        || to == GamePhase.StoreOpen;

                case GamePhase.TravelToMarket:
                    return to == GamePhase.AtMarket;

                case GamePhase.AtMarket:
                    return to == GamePhase.TravelToStore
                        || to == GamePhase.TravelToMarket;

                case GamePhase.TravelToStore:
                    return to == GamePhase.StoreOpen
                        || to == GamePhase.StoreClosed;

                case GamePhase.StoreOpen:
                    return to == GamePhase.StoreClosed
                        || to == GamePhase.CraftingMenu
                        || to == GamePhase.Paused;

                case GamePhase.StoreClosed:
                    return to == GamePhase.CraftingMenu
                        || to == GamePhase.EndOfDaySummary
                        || to == GamePhase.Paused
                        || to == GamePhase.TravelToMarket;

                case GamePhase.CraftingMenu:
                    return to == GamePhase.StoreOpen
                        || to == GamePhase.StoreClosed;

                case GamePhase.EndOfDaySummary:
                    return to == GamePhase.Sleep;

                case GamePhase.Sleep:
                    return to == GamePhase.ChooseWakeTime;

                // Paused is handled by ResumeGame; direct transitions are illegal.
                case GamePhase.Paused:
                    return false;

                default:
                    return false;
            }
        }
    }
}
