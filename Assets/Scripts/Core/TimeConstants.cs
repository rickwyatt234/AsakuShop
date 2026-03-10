namespace AsakuShop.Core
{
    /// <summary>
    /// Compile-time constants shared across the AsakuShop.Core namespace.
    /// Use these instead of magic numbers in all time-related logic.
    /// </summary>
    public static class TimeConstants
    {
        // ── Calendar ────────────────────────────────────────────────────────────

        /// <summary>Number of minutes in one hour.</summary>
        public const int MinutesPerHour = 60;

        /// <summary>Number of hours in one day.</summary>
        public const int HoursPerDay = 24;

        /// <summary>Number of minutes in one full day (1440).</summary>
        public const int MinutesPerDay = MinutesPerHour * HoursPerDay;

        /// <summary>Number of days in one week.</summary>
        public const int DaysPerWeek = 7;

        // ── Time-scale presets ───────────────────────────────────────────────────

        /// <summary>
        /// Default time-scale: 1 in-game minute passes per real-world second.
        /// </summary>
        public const float DefaultTimeScale = 1.0f;

        /// <summary>
        /// Time-scale used while the player is sleeping — effectively instant
        /// fast-forward.
        /// </summary>
        public const float SleepTimeScale = 999.0f;

        /// <summary>
        /// Time-scale applied while the player is travelling between locations.
        /// </summary>
        public const float TravelTimeScale = 10.0f;

        // ── Default schedule times (24-hour) ─────────────────────────────────────

        /// <summary>Default wake-time hour (5 AM).</summary>
        public const int DefaultWakeTimeHour = 5;

        /// <summary>Default wake-time minute (0 minutes past the hour).</summary>
        public const int DefaultWakeTimeMinute = 0;

        /// <summary>Default store open hour (7 AM).</summary>
        public const int DefaultStoreOpenHour = 7;

        /// <summary>Default store open minute (0 minutes past the hour).</summary>
        public const int DefaultStoreOpenMinute = 0;

        /// <summary>Default store close hour (11 PM).</summary>
        public const int DefaultStoreCloseHour = 23;

        /// <summary>Default store close minute (0 minutes past the hour).</summary>
        public const int DefaultStoreCloseMinute = 0;

        /// <summary>The hour value (24-hour) at which midnight occurs and EndOfDaySummary auto-triggers.</summary>
        public const int MidnightHour = 0;
    }
}
