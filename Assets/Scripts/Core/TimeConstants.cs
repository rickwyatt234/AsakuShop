namespace AsakuShop.Core
{
    public static class TimeConstants
    {
#region Calendar

        // Number of minutes in one hour.
        public const int MinutesPerHour = 60;

        // Number of hours in one day.
        public const int HoursPerDay = 24;

        // Number of minutes in one full day (1440).
        public const int MinutesPerDay = MinutesPerHour * HoursPerDay;

        // Number of days in one week.
        public const int DaysPerWeek = 7;
#endregion

#region Time scales

        // Default time-scale: X in-game minute passes per real-world second.
        public const float DefaultTimeScale = 0.5f;

        // Time-scale used while the player is sleeping — effectively instant
        // fast-forward.
        public const float SleepTimeScale = 999.0f;

        // Time-scale applied while the player is travelling between locations.
        public const float TravelTimeScale = 10.0f;
#endregion

#region Default times (Probably won't be used)

        // Default wake-time hour (5 AM).
        public const int DefaultWakeTimeHour = 5;

        // Default wake-time minute (0 minutes past the hour).
        public const int DefaultWakeTimeMinute = 0;

        // Default store open hour (7 AM).
        public const int DefaultStoreOpenHour = 7;

        // Default store open minute (0 minutes past the hour).
        public const int DefaultStoreOpenMinute = 0;

        // Default store close hour (11 PM).
        public const int DefaultStoreCloseHour = 23;

        // Default store close minute (0 minutes past the hour).
        public const int DefaultStoreCloseMinute = 0;

        // The hour value (24-hour) at which midnight occurs and EndOfDaySummary auto-triggers.
        public const int MidnightHour = 0;
#endregion
    }
}