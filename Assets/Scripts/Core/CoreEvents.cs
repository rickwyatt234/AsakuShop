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

        // UI decoupling events
        public static event Action<object> OnInventoryOpenRequested;
        public static event Action<object> OnStorageUIRefreshRequested;
        public static event Action<object> OnExamineRequested;
        public static event Action<IInteractable> OnContextHintRequested;
        public static event Action OnContextHintHideRequested;

        // Fired when the player examines a shelved item and the price editor should open.
        // Payload is the ItemInstance whose price is to be set.
        public static event Action<object> OnShelfItemPriceEditRequested;

        // Store events
        public static event Action OnStoreOpened;
        public static event Action OnStoreClosed;

        // Customer events (object payload avoids cross-assembly type references)
        public static event Action<object> OnCustomerEntered;
        public static event Action<object> OnCustomerLeft;

        // Economy events (object payload avoids cross-assembly type references)
        public static event Action<object> OnItemSold;
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
        internal static void RaiseMidnightReached(int dayIndex)
            => OnMidnightReached?.Invoke(dayIndex);

        // UI decoupling raise helpers
        public static void RaiseInventoryOpenRequested(object container)
            => OnInventoryOpenRequested?.Invoke(container);
        public static void RaiseStorageUIRefreshRequested(object container)
            => OnStorageUIRefreshRequested?.Invoke(container);
        public static void RaiseExamineRequested(object item)
            => OnExamineRequested?.Invoke(item);
        public static void RaiseContextHintRequested(IInteractable interactable)
            => OnContextHintRequested?.Invoke(interactable);
        public static void RaiseContextHintHideRequested()
            => OnContextHintHideRequested?.Invoke();

        public static void RaiseShelfItemPriceEditRequested(object item)
            => OnShelfItemPriceEditRequested?.Invoke(item);

        // Store raise helpers
        public static void RaiseStoreOpened()
            => OnStoreOpened?.Invoke();
        public static void RaiseStoreClosed()
            => OnStoreClosed?.Invoke();

        // Customer raise helpers
        public static void RaiseCustomerEntered(object customer)
            => OnCustomerEntered?.Invoke(customer);
        public static void RaiseCustomerLeft(object customer)
            => OnCustomerLeft?.Invoke(customer);

        // Economy raise helpers
        public static void RaiseItemSold(object saleEntry)
            => OnItemSold?.Invoke(saleEntry);
#endregion

#region Cleanup Helper
        public static void ClearAll()
        {
            OnPhaseChanged              = null;
            OnTimeAdvanced              = null;
            OnDayStarted                = null;
            OnDayEnded                  = null;
            OnBeforeSave                = null;
            OnAfterLoad                 = null;
            OnMidnightReached           = null;
            OnInventoryOpenRequested    = null;
            OnStorageUIRefreshRequested = null;
            OnExamineRequested          = null;
            OnContextHintRequested      = null;
            OnContextHintHideRequested  = null;
            OnShelfItemPriceEditRequested = null;
            OnStoreOpened               = null;
            OnStoreClosed               = null;
            OnCustomerEntered           = null;
            OnCustomerLeft              = null;
            OnItemSold                  = null;
        }
#endregion
    }
}
