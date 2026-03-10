using UnityEngine;

namespace AsakuShop.Core
{
    /// <summary>
    /// The single source of truth for in-game time. Persists across scene loads.
    /// Attach to a GameObject in the Bootstrap scene or let
    /// <see cref="GameBootstrapper"/> create it automatically.
    /// </summary>
    /// <remarks>
    /// Time advances in <c>Update()</c> unless <see cref="ClockPaused"/> is
    /// <c>true</c>. <see cref="GameStateController"/> sets this flag whenever the
    /// active phase is <see cref="GamePhase.Paused"/> or
    /// <see cref="GamePhase.CraftingMenu"/>.
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

        /// <summary>
        /// When <c>true</c> the clock does not advance in <c>Update()</c>.
        /// Set by <see cref="GameStateController"/> for paused / crafting phases.
        /// </summary>
        [Tooltip("Freeze in-game time when true.")]
        public bool ClockPaused;

        /// <summary>The current in-game moment.</summary>
        [Tooltip("Current in-game time (read-only at runtime).")]
        public GameTime CurrentTime;

        // ── Private state ─────────────────────────────────────────────────────────

        /// <summary>Accumulated fractional in-game minutes not yet applied to <see cref="CurrentTime"/>.</summary>
        private float _accumulatedMinutes;

        /// <summary>Wake time stored by <see cref="SetWakeTime"/>.</summary>
        private int _wakeTimeHour   = TimeConstants.DefaultWakeTimeHour;
        private int _wakeTimeMinute = TimeConstants.DefaultWakeTimeMinute;

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
        }
    }
}
