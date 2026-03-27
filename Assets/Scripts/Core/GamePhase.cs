namespace AsakuShop.Core
{
    // Enumeration of the different phases of the game. Used by GameStateController to manage
    // Helps determine what systems should be active, what input should be accepted, and whether the clock should be ticking.
    public enum GamePhase
    {
        //Clock stopped. No gameplay.
        Boot,

        //Clock stopped.
        MainMenu,

        // The active game day.Clock ticks continuously.
        Playing,

        // Full-screen crafting recipe selection menu. Clock stopped.
        CraftingMenu,

        // End-of-day summary screen. Triggered at midnight automatically, or when
        // the player interacts with a bed before midnight. Clock stopped.
        EndOfDaySummary,

        /// Player is asleep. No input accepted. Clock fast-forwards to the
        /// player-specified wake time. Transitions to Playing on completion.
        Sleep,

        //Player is examining an item in detail. Triggered by pressing examine on an item in the world or inventory. Clock ticks continuously.
        ItemExamination,

        Checkout, // Player is at the checkout counter. Triggered by interacting with the counter. Clock ticks continuously.

        /// Game is paused. Overlays any phase except Boot and MainMenu.
        /// Clock stopped. Restores to previous phase on resume.
        /// Can be triggered from anywhere at any time.
        Paused
    }
}