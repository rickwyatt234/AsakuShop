namespace AsakuShop.Core
{
    /// <summary>
    /// All top-level game phases. A phase only exists if it meaningfully changes
    /// what systems are active, what input is accepted, or what the clock does.
    /// Location (market, store, street) and store open/closed state are NOT phases —
    /// they are tracked by their respective systems.
    /// </summary>
    public enum GamePhase
    {
        /// <summary>Application is initializing. No gameplay. Clock stopped.</summary>
        Boot,

        /// <summary>Main menu / title screen. No gameplay. Clock stopped.</summary>
        MainMenu,

        /// <summary>
        /// The active game day. The player has full sandbox freedom:
        /// walking around, visiting markets, running the store, crafting, etc.
        /// Clock ticks continuously while in this phase.
        /// </summary>
        Playing,

        /// <summary>
        /// Full-screen crafting recipe selection menu. Clock is paused while open.
        /// Once the player confirms a recipe, transitions back to Playing while
        /// a HUD fill bar shows crafting progress — that progress is NOT a phase.
        /// </summary>
        CraftingMenu,

        /// <summary>
        /// End-of-day summary screen. Triggered at midnight automatically, or when
        /// the player interacts with a bed before midnight. Clock stopped.
        /// After dismissal: returns to Playing (if midnight-triggered) or transitions
        /// to Sleep (if bed-triggered before midnight).
        /// </summary>
        EndOfDaySummary,

        /// <summary>
        /// Player is asleep. No input accepted. Clock fast-forwards to the
        /// player-specified wake time. Transitions to Playing on completion.
        /// Note: if the player goes to bed AFTER midnight, EndOfDaySummary is
        /// skipped and this phase is entered directly.
        /// </summary>
        Sleep,

        /// <summary>
        /// Game is paused. Overlays any phase except Boot and MainMenu.
        /// Clock stopped. Restores to previous phase on resume.
        /// Can be triggered from anywhere at any time.
        /// </summary>
        Paused
    }
}
