using UnityEngine;
using System.Collections.Generic;

namespace AsakuShop.Core
{
    /// Attach to a GameObject in the Bootstrap scene or letGameBootstrapper create it automatically.
    /// Time advances in Update() unless ClockPaused is true. GameStateController calls
    /// PushClockPause() and PopClockPause() to manage the pause state whenever the active phase changes.
    [DefaultExecutionOrder(-900)]
    public class GameClock : MonoBehaviour
    {
        public static GameClock Instance { get; private set; }

#region Serialised fields
        [Tooltip("In-game minutes per real-world second.")]
        public float TimeScale = TimeConstants.DefaultTimeScale;

        [Tooltip("Current in-game time (read-only at runtime).")]
        public GameTime CurrentTime;
#endregion

#region Pause management
        // Tracks the sources of clock pauses. If any source is active, the clock is paused.
        private readonly HashSet<string> _pauseSources = new HashSet<string>();

        // True if the clock is currently paused by any source.
        public bool ClockPaused => _pauseSources.Count > 0;

        // Call to pause the clock, providing a string identifier for the source of the pause (e.g. a game phase name).
        public void PushClockPause(string source)
        {
            _pauseSources.Add(source);
        }

        // Call to unpause the clock for a given source. The source string must match the one used in PushClockPause().
        public void PopClockPause(string source)
        {
            _pauseSources.Remove(source);
        }
#endregion

#region Internal state for time advancement

        //fraction minutes that have accumulated since the last whole-minute advancement. 
        //Updated every frame by Update() and used to determine when to advance the in-game time.
        private float _accumulatedMinutes;

        // Configurable wake time used by FastForwardToWakeTime(). Stored as separate hour and minute 
        // fields for easier UI integration.
        private int _wakeTimeHour   = TimeConstants.DefaultWakeTimeHour;
        private int _wakeTimeMinute = TimeConstants.DefaultWakeTimeMinute;

        // Tracks whether the midnight summary event has been fired for the current day. 
        // Used to ensure it only fires once per day.
        private bool _midnightSummaryFiredToday = false;
#endregion

#region Unity Methods 
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogError("[GameClock] Duplicate instance detected — destroying self.");
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Update()
        {
            if (ClockPaused) return;

            // Accumulate in-game minutes based on real time passed and the time scale.
            _accumulatedMinutes += Time.deltaTime * TimeScale;

            // Convert whole accumulated in-game minutes into time advancement.
            int minutesToAdvance = Mathf.FloorToInt(_accumulatedMinutes);
            if (minutesToAdvance <= 0) return;

            _accumulatedMinutes -= minutesToAdvance;
            AdvanceTimeByMinutes(minutesToAdvance);

            if (CurrentTime.Hour == TimeConstants.MidnightHour && CurrentTime.Minute == 0 && !_midnightSummaryFiredToday)
            {
                _midnightSummaryFiredToday = true;
                CoreEvents.RaiseMidnightReached(CurrentTime.DayIndex);
            }
        }
#endregion

#region Public methods
        public void AdvanceTimeByMinutes(int minutes)
        {
            if (minutes <= 0) return;

            int currentTotalMinutes = CurrentTime.ToTotalMinutes() + minutes;
            int dayIndex            = CurrentTime.DayIndex;

            // Roll over each full day that is contained in the delta.
            while (currentTotalMinutes >= TimeConstants.MinutesPerDay)
            {
                CoreEvents.RaiseDayEnded(dayIndex);
                currentTotalMinutes -= TimeConstants.MinutesPerDay;
                dayIndex++;
                _midnightSummaryFiredToday = false;
                CoreEvents.RaiseDayStarted(dayIndex);
            }

            CurrentTime = GameTime.FromMinutes(dayIndex, currentTotalMinutes);
            CoreEvents.RaiseTimeAdvanced(minutes);
        }

        public void SetWakeTime(int hour, int minute)
        {
            _wakeTimeHour   = Mathf.Clamp(hour,   0, TimeConstants.HoursPerDay   - 1);
            _wakeTimeMinute = Mathf.Clamp(minute, 0, TimeConstants.MinutesPerHour - 1);
        }

        public void FastForwardToWakeTime()
        {
            int currentMinutes = CurrentTime.ToTotalMinutes();
            int wakeMinutes    = _wakeTimeHour * TimeConstants.MinutesPerHour + _wakeTimeMinute;

            // Calculate forward delta — always at least 1 minute into the future.
            int delta = wakeMinutes - currentMinutes;
            if (delta <= 0) delta += TimeConstants.MinutesPerDay;

            AdvanceTimeByMinutes(delta);

            // Reset so the new day's midnight can fire its summary event properly.
            _midnightSummaryFiredToday = false;
        }
#endregion

#region Debugging
        //Display time in corner of screen for debugging purposes.
        //GUI layout, but there is already the player state being shown here, so offsetting it a bit to avoid overlap.
        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(0, 20, 200, 20));
            GUILayout.Label($"Time: {CurrentTime}");
            GUILayout.EndArea();
        }
#endregion
    }
}