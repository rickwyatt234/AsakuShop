using UnityEngine;

namespace AsakuShop.Core
{
    // Owns and manages the current GamePhase. Persists across
    // scene loads. All code that wants to change the game phase must call
    //RequestTransition so that legality can be enforced and
    //the appropriate events fired.
    [DefaultExecutionOrder(-900)]
    public class GameStateController : MonoBehaviour
    {
        //Singleton
        public static GameStateController Instance { get; private set; }

        // The currently active game phase. Set only by RequestTransition() 
        public GamePhase CurrentPhase { get; private set; } = GamePhase.Boot;

        //Inspector Field
        [Tooltip("GameClock whose pause stack is managed by this controller.")]
        public GameClock Clock;

        // Stores which phase was active before entering Paused, to restore on resume.
        private GamePhase _prePausePhase = GamePhase.Boot;

        // Stores which phase was active before entering EndOfDaySummary, to know whether to return to Playing or go to Sleep.
        private bool _summaryTriggeredByBed = false;

        // Stores the phase to return to after EndOfDaySummary dismissal.
        private GamePhase _postSummaryPhase = GamePhase.Playing;

#region Unity Methods
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
#endregion

#region Public Methods
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

        public bool IsClockRunning()
        {
            return CurrentPhase == GamePhase.Playing;
        }

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

        public void DismissEndOfDaySummary()
        {
            if (CurrentPhase != GamePhase.EndOfDaySummary)
            {
                Debug.LogWarning("[GameStateController] DismissEndOfDaySummary called but not in EndOfDaySummary phase.");
                return;
            }
            RequestTransition(_postSummaryPhase);
        }
#endregion

#region Private Methods
        private void UpdateClockPaused()
        {
            if (Clock == null) return;

            // The clock should only be running during the Playing, ItemExamination, PriceSetting, and Checkout phases. It is paused in all other phases.
            if (CurrentPhase == GamePhase.Playing || CurrentPhase == GamePhase.ItemExamination || CurrentPhase == GamePhase.PriceSetting || CurrentPhase == GamePhase.Checkout)
                Clock.PopClockPause("GameStateController");
            else
                Clock.PushClockPause("GameStateController");
        }

        private void HandleMidnightReached(int dayIndex)
        {
            if (CurrentPhase != GamePhase.Playing) return;

            _summaryTriggeredByBed = false;
            _postSummaryPhase = GamePhase.Playing;
            RequestTransition(GamePhase.EndOfDaySummary);
        }

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
                        || to == GamePhase.ItemExamination
                        || to == GamePhase.PriceSetting
                        || to == GamePhase.EndOfDaySummary
                        || to == GamePhase.Sleep
                        || to == GamePhase.Paused
                        || to == GamePhase.Checkout;
                
                case GamePhase.Checkout:
                    return to == GamePhase.Playing
                        || to == GamePhase.Paused;

                case GamePhase.CraftingMenu:
                    return to == GamePhase.Playing;

                case GamePhase.EndOfDaySummary:
                    return to == GamePhase.Playing
                        || to == GamePhase.Sleep;

                case GamePhase.Sleep:
                    return to == GamePhase.Playing;

                case GamePhase.ItemExamination:
                    return to == GamePhase.Playing
                        || to == GamePhase.Paused;

                case GamePhase.PriceSetting:
                    return to == GamePhase.Playing
                        || to == GamePhase.Paused;

                // Paused is handled by ResumeGame; direct transitions via RequestTransition are illegal.
                case GamePhase.Paused:
                    return false;

                default:
                    return false;
            }
        }
#endregion


#region Debugging
        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(0, 60, 200, 20));
            GUILayout.Label("Current Phase: " + CurrentPhase);
            GUILayout.EndArea();
        }
#endregion
    }
}