namespace AsakuShop.Core
{
    /// <summary>
    /// Represents every possible high-level state the game can be in.
    /// <see cref="GameStateController"/> owns and transitions this value.
    /// </summary>
    public enum GamePhase
    {
        /// <summary>Application is starting up; systems are initialising.</summary>
        Boot,

        /// <summary>The main menu is displayed.</summary>
        MainMenu,

        /// <summary>The player is selecting a wake time for the upcoming day.</summary>
        ChooseWakeTime,

        /// <summary>The player is travelling to a wholesale market.</summary>
        TravelToMarket,

        /// <summary>The player is inside a wholesale market.</summary>
        AtMarket,

        /// <summary>The player is travelling back to the store.</summary>
        TravelToStore,

        /// <summary>The store is open and serving customers.</summary>
        StoreOpen,

        /// <summary>The store is closed; the player can craft, plan, or rest.</summary>
        StoreClosed,

        /// <summary>The crafting / preparation menu is open. Clock is paused.</summary>
        CraftingMenu,

        /// <summary>The end-of-day summary screen is displayed.</summary>
        EndOfDaySummary,

        /// <summary>The player is sleeping; time is fast-forwarded to the wake time.</summary>
        Sleep,

        /// <summary>The game is paused. Clock is paused.</summary>
        Paused,
    }
}
