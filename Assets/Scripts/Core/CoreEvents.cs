using System;

namespace AsakuShop.Core
{
    // Central hub for all core game events. Uses plain C# Actions so listeners can be added 
    // from any assembly.
    // Use ClearAll() on scene teardown to prevent stale listeners from surviving a scene transition.
    public static class CoreEvents
    {
#region Events
        public static event Action<GamePhase, GamePhase> OnPhaseChanged;
        public static event Action<int> OnTimeAdvanced;
        public static event Action<int> OnDayStarted;
        public static event Action<int> OnDayEnded;
        public static event Action OnBeforeSave;
        public static event Action OnAfterLoad;
        public static event Action<int> OnMidnightReached;

        // Fired by StorageContainer instead of calling StorageInventoryUI directly
        public static event Action<object> OnInventoryOpenRequested;
        // Fired by PlayerHands.TryStoreItem instead of calling StorageInventoryUI.RefreshUI directly
        public static event Action<object> OnStorageUIRefreshRequested;
        // Fired by PlayerHands instead of calling ItemExaminer.StartExamination directly
        public static event Action<object> OnExamineRequested;
#endregion

#region Raise Helpers
        internal static void RaisePhaseChanged(GamePhase previous, GamePhase next)
            => OnPhaseChanged?.Invoke(previous, next);
        internal static void RaiseTimeAdvanced(int deltaMinutes)
            => OnTimeAdvanced?.Invoke(deltaMinutes);
        internal static void RaiseDayStarted(int dayIndex)
            => OnDayStarted?.Invoke(dayIndex);
        internal static void RaiseDayEnded(int dayIndex)
            => OnDayEnded?.Invoke(dayIndex);
        internal static void RaiseBeforeSave()
            => OnBeforeSave?.Invoke();
        internal static void RaiseAfterLoad()
            => OnAfterLoad?.Invoke();
        internal static void FireMidnightReached(int dayIndex)
            => OnMidnightReached?.Invoke(dayIndex);

        public static void RaiseInventoryOpenRequested(object container)
            => OnInventoryOpenRequested?.Invoke(container);
        public static void RaiseStorageUIRefreshRequested(object container)
            => OnStorageUIRefreshRequested?.Invoke(container);
        public static void RaiseExamineRequested(object item)
            => OnExamineRequested?.Invoke(item);
#endregion

#region Cleanup Helper
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
#endregion
    }
}
