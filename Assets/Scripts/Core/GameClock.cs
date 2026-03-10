using UnityEngine;
using System.Collections.Generic;

namespace AsakuShop.Core
{
    /// <summary>
    /// The single source of truth for in-game time. Persists across scene loads.
    /// Attach to a GameObject in the Bootstrap scene or let
    /// <see cref="GameBootstrapper"/> create it automatically.
    /// </summary>
    /// <remarks>
    /// Time advances in <c>Update()</c> unless <see cref="ClockPaused"/> is
    /// <c>true</c>. <see cref="GameStateController"/> calls
    /// <see cref="PushClockPause"/> and <see cref="PopClockPause"/> to manage
    /// the pause state whenever the active phase changes.
    /// </remarks>
    [DefaultExecutionOrder(-900)]
    public class GameClock : MonoBehaviour
    {
        // ── Singleton ────────────────────────────────────────────────────────────

        /// <summary>Singleton instance of the <see cref="GameClock"/>.</summary>
        public static GameClock Instance { get; private set; }

        // ── Serialised fields ────────────────────────────────────────────────────

        /// <summary>
        /// How many in-game minutes pass per real-world second.
        /// Default is <see cref="TimeConstants.DefaultTimeScale"/> (1).
        /// Travel and sleep systems temporarily override this value.
        /// </summary>
        [Tooltip("In-game minutes per real-world second.")]
        public float TimeScale = TimeConstants.DefaultTimeScale;

        /// <summary>The current in-game moment.</summary>
        [Tooltip("Current in-game time (read-only at runtime).")]
        public GameTime CurrentTime;

        // ── Pause-stack ───────────────────────────────────────────────────────────

        private readonly HashSet<string> _pauseSources = new HashSet<string>();

        /// <summary>Returns true if any system has requested a clock pause.</summary>
        public bool ClockPaused => _pauseSources.Count > 0;

        /// <summary>
        /// Adds a named pause source. The clock will stop ticking until all sources are removed.
        /// Safe to call multiple times with the same key.
        /// </summary>
        /// <param name="source">Unique name identifying the system requesting the pause.</param>
        public void PushClockPause(string source)
        {
            _pauseSources.Add(source);
        }

        /// <summary>
        /// Removes a named pause source. Clock resumes when no sources remain.
        /// </summary>
        /// <param name="source">The name of the pause source to remove.</param>
        public void PopClockPause(string source)
        {
            _pauseSources.Remove(source);
        }

        // ── Private state ─────────────────────────────────────────────────────────

        /// <summary>Accumulated fractional in-game minutes not yet applied to <see cref="CurrentTime"/>.</summary>
        private float _accumulatedMinutes;

        /// <summary>Wake time stored by <see cref="SetWakeTime"/>.</summary>
        private int _wakeTimeHour   = TimeConstants.DefaultWakeTimeHour;
        private int _wakeTimeMinute = TimeConstants.DefaultWakeTimeMinute;

        /// <summary>Prevents the midnight EndOfDaySummary from firing more than once per day.</summary>
        private bool _midnightSummaryFiredToday = false;

        // ── Unity lifecycle ──────────────────────────────────────────────────────

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

            _accumulatedMinutes += Time.deltaTime * TimeScale;

            // Convert whole accumulated in-game minutes into time advancement.
            int minutesToAdvance = Mathf.FloorToInt(_accumulatedMinutes);
            if (minutesToAdvance <= 0) return;

            _accumulatedMinutes -= minutesToAdvance;
            AdvanceTimeByMinutes(minutesToAdvance);

            if (CurrentTime.Hour == TimeConstants.MidnightHour && CurrentTime.Minute == 0 && !_midnightSummaryFiredToday)
            {
                _midnightSummaryFiredToday = true;
                CoreEvents.FireMidnightReached(CurrentTime.DayIndex);
            }
        }

        // ── Public API ───────────────────────────────────────────────────────────

        /// <summary>
        /// Immediately advances in-game time by <paramref name="minutes"/> minutes.
        /// Handles hour, day, and day-of-week rollovers correctly and fires the
        /// appropriate <see cref="CoreEvents"/>.
        /// </summary>
        /// <param name="minutes">Number of in-game minutes to advance (must be ≥ 0).</param>
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

        /// <summary>
        /// Stores a target wake time used by <see cref="FastForwardToWakeTime"/>.
        /// </summary>
        /// <param name="hour">Target hour in 24-hour format (0–23).</param>
        /// <param name="minute">Target minute (0–59).</param>
        public void SetWakeTime(int hour, int minute)
        {
            _wakeTimeHour   = Mathf.Clamp(hour,   0, TimeConstants.HoursPerDay   - 1);
            _wakeTimeMinute = Mathf.Clamp(minute, 0, TimeConstants.MinutesPerHour - 1);
        }

        /// <summary>
        /// Calculates the number of minutes from <see cref="CurrentTime"/> to the
        /// stored wake time (always forward, possibly crossing midnight), then calls
        /// <see cref="AdvanceTimeByMinutes"/>. Fires
        /// <see cref="CoreEvents.OnDayStarted"/> if a new day is crossed.
        /// </summary>
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
    }
}
