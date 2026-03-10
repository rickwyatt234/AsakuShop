using System;

namespace AsakuShop.Core
{
    /// <summary>
    /// Central hub for all core game events. Uses plain C# <see cref="Action"/>
    /// delegates (not UnityEvents) so that listeners can be added from any
    /// assembly without a Unity dependency on the event bus itself.
    /// </summary>
    /// <remarks>
    /// Call <see cref="ClearAll"/> during scene teardown to prevent stale
    /// delegate references from carrying across scenes.
    /// </remarks>
    public static class CoreEvents
    {
        /// <summary>
        /// Fired by <see cref="GameStateController"/> whenever the active
        /// <see cref="GamePhase"/> changes.
        /// </summary>
        /// <remarks>Parameters: (previousPhase, newPhase).</remarks>
        public static event Action<GamePhase, GamePhase> OnPhaseChanged;

        /// <summary>
        /// Fired by <see cref="GameClock"/> every time in-game time advances.
        /// </summary>
        /// <remarks>Parameter: deltaMinutes — the number of in-game minutes that just elapsed.</remarks>
        public static event Action<int> OnTimeAdvanced;

        /// <summary>
        /// Fired by <see cref="GameClock"/> when the in-game date rolls over to
        /// a new day (midnight crossed or wake-time fast-forward).
        /// </summary>
        /// <remarks>Parameter: the new <c>DayIndex</c>.</remarks>
        public static event Action<int> OnDayStarted;

        /// <summary>
        /// Fired by <see cref="GameClock"/> just before the in-game date rolls
        /// over (i.e. just before <see cref="OnDayStarted"/>).
        /// </summary>
        /// <remarks>Parameter: the day index that is ending.</remarks>
        public static event Action<int> OnDayEnded;

        /// <summary>
        /// Fired by <see cref="SaveManager"/> immediately before game state is
        /// serialised to disk. Listeners should flush any pending changes.
        /// </summary>
        public static event Action OnBeforeSave;

        /// <summary>
        /// Fired by <see cref="SaveManager"/> after all participant state has
        /// been deserialised and applied. Listeners can now read restored data.
        /// </summary>
        public static event Action OnAfterLoad;

        /// <summary>
        /// Fired once per day exactly when the clock rolls to midnight (Hour=0, Minute=0).
        /// GameStateController listens to this to trigger EndOfDaySummary automatically.
        /// </summary>
        /// <remarks>Parameter: the <c>DayIndex</c> of the new day.</remarks>
        public static event Action<int> OnMidnightReached;

        // ── Raise helpers (internal use only) ───────────────────────────────────

        /// <summary>Raises <see cref="OnPhaseChanged"/>.</summary>
        internal static void RaisePhaseChanged(GamePhase previous, GamePhase next)
            => OnPhaseChanged?.Invoke(previous, next);

        /// <summary>Raises <see cref="OnTimeAdvanced"/>.</summary>
        internal static void RaiseTimeAdvanced(int deltaMinutes)
            => OnTimeAdvanced?.Invoke(deltaMinutes);

        /// <summary>Raises <see cref="OnDayStarted"/>.</summary>
        internal static void RaiseDayStarted(int dayIndex)
            => OnDayStarted?.Invoke(dayIndex);

        /// <summary>Raises <see cref="OnDayEnded"/>.</summary>
        internal static void RaiseDayEnded(int dayIndex)
            => OnDayEnded?.Invoke(dayIndex);

        /// <summary>Raises <see cref="OnBeforeSave"/>.</summary>
        internal static void RaiseBeforeSave()
            => OnBeforeSave?.Invoke();

        /// <summary>Raises <see cref="OnAfterLoad"/>.</summary>
        internal static void RaiseAfterLoad()
            => OnAfterLoad?.Invoke();

        /// <summary>Fires <see cref="OnMidnightReached"/>. Called by <see cref="GameClock"/> only.</summary>
        internal static void FireMidnightReached(int dayIndex)
            => OnMidnightReached?.Invoke(dayIndex);

        // ── Cleanup ──────────────────────────────────────────────────────────────

        /// <summary>
        /// Nulls all event delegate chains. Call this on scene teardown to
        /// ensure no stale listeners survive a scene transition.
        /// </summary>
        public static void ClearAll()
        {
            OnPhaseChanged = null;
            OnTimeAdvanced = null;
            OnDayStarted   = null;
            OnDayEnded     = null;
            OnBeforeSave   = null;
            OnAfterLoad    = null;
            OnMidnightReached = null;
        }
    }
}
