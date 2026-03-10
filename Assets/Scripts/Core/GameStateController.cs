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
        [Tooltip("GameClock whose pause stack is managed by this controller.")]
        public GameClock Clock;

        // ── Private state ─────────────────────────────────────────────────────────

        /// <summary>
        /// The phase that was active just before <see cref="GamePhase.Paused"/>
        /// was entered; used by <see cref="ResumeGame"/>.
        /// </summary>
        private GamePhase _prePausePhase = GamePhase.Boot;

        /// <summary>Stores which phase was active before entering EndOfDaySummary, to know whether to return to Playing or go to Sleep.</summary>
        private bool _summaryTriggeredByBed = false;

        /// <summary>Stores the phase to return to after EndOfDaySummary dismissal.</summary>
        private GamePhase _postSummaryPhase = GamePhase.Playing;

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

            CoreEvents.OnMidnightReached += HandleMidnightReached;
        }

        private void OnDestroy()
        {
            CoreEvents.OnMidnightReached -= HandleMidnightReached;
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
        /// because the player can pause from any active phase at any time.
        /// Cannot be called from <see cref="GamePhase.Boot"/>,
        /// <see cref="GamePhase.MainMenu"/>, or when already paused.
        /// </summary>
        public void PauseGame()
        {
            if (CurrentPhase == GamePhase.Paused
                || CurrentPhase == GamePhase.Boot
                || CurrentPhase == GamePhase.MainMenu) return;

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
        /// <c>true</c> when time should advance; <c>false</c> for all other phases.
        /// Clock runs only during <see cref="GamePhase.Playing"/>.
        /// </returns>
        public bool IsClockRunning()
        {
            return CurrentPhase == GamePhase.Playing;
        }

        /// <summary>
        /// Called when the player interacts with a bed. Behavior depends on current time:
        /// <list type="bullet">
        ///   <item><description>Before midnight: transitions to EndOfDaySummary (then Sleep after dismissal).</description></item>
        ///   <item><description>After midnight (Hour == 0 of the next logical day): skips summary, goes directly to Sleep.</description></item>
        /// </list>
        /// </summary>
        public void TriggerBedInteraction()
        {
            if (Clock == null) return;

            bool isAfterMidnight = Clock.CurrentTime.Hour == TimeConstants.MidnightHour && Clock.CurrentTime.DayIndex > 0;

            if (isAfterMidnight)
            {
                // Skip summary — go straight to sleep
                _summaryTriggeredByBed = false;
                RequestTransition(GamePhase.Sleep);
            }
            else
            {
                // Show summary first, then sleep
                _summaryTriggeredByBed = true;
                _postSummaryPhase = GamePhase.Sleep;
                RequestTransition(GamePhase.EndOfDaySummary);
            }
        }

        /// <summary>
        /// Called by UI when the player dismisses the EndOfDaySummary screen.
        /// Transitions to Sleep (if bed-triggered) or back to Playing (if midnight-triggered).
        /// </summary>
        public void DismissEndOfDaySummary()
        {
            if (CurrentPhase != GamePhase.EndOfDaySummary)
            {
                Debug.LogWarning("[GameStateController] DismissEndOfDaySummary called but not in EndOfDaySummary phase.");
                return;
            }
            RequestTransition(_postSummaryPhase);
        }

        // ── Private helpers ──────────────────────────────────────────────────────

        /// <summary>
        /// Updates the clock pause stack after a phase transition.
        /// Entering <see cref="GamePhase.Playing"/> pops the controller's pause token;
        /// entering any other phase pushes it.
        /// </summary>
        private void UpdateClockPaused()
        {
            if (Clock == null) return;

            if (CurrentPhase == GamePhase.Playing)
                Clock.PopClockPause("GameStateController");
            else
                Clock.PushClockPause("GameStateController");
        }

        /// <summary>
        /// Handles the automatic midnight trigger for <see cref="GamePhase.EndOfDaySummary"/>.
        /// Fired by <see cref="CoreEvents.OnMidnightReached"/> once per day.
        /// Only acts when currently in <see cref="GamePhase.Playing"/>.
        /// </summary>
        private void HandleMidnightReached(int dayIndex)
        {
            if (CurrentPhase != GamePhase.Playing) return;

            _summaryTriggeredByBed = false;
            _postSummaryPhase = GamePhase.Playing;
            RequestTransition(GamePhase.EndOfDaySummary);
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
                    return to == GamePhase.Playing;

                case GamePhase.Playing:
                    return to == GamePhase.CraftingMenu
                        || to == GamePhase.EndOfDaySummary
                        || to == GamePhase.Sleep
                        || to == GamePhase.Paused;

                case GamePhase.CraftingMenu:
                    return to == GamePhase.Playing;

                case GamePhase.EndOfDaySummary:
                    return to == GamePhase.Playing
                        || to == GamePhase.Sleep;

                case GamePhase.Sleep:
                    return to == GamePhase.Playing;

                // Paused is handled by ResumeGame; direct transitions via RequestTransition are illegal.
                case GamePhase.Paused:
                    return false;

                default:
                    return false;
            }
        }
    }
}
