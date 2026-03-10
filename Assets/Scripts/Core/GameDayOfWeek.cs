namespace AsakuShop.Core
{
    /// <summary>
    /// In-game days of the week. Day 0 maps to <see cref="Monday"/>.
    /// </summary>
    public enum GameDayOfWeek
    {
        /// <summary>Monday — day index mod 7 == 0.</summary>
        Monday,

        /// <summary>Tuesday — day index mod 7 == 1.</summary>
        Tuesday,

        /// <summary>Wednesday — day index mod 7 == 2.</summary>
        Wednesday,

        /// <summary>Thursday — day index mod 7 == 3.</summary>
        Thursday,

        /// <summary>Friday — day index mod 7 == 4.</summary>
        Friday,

        /// <summary>Saturday — day index mod 7 == 5.</summary>
        Saturday,

        /// <summary>Sunday — day index mod 7 == 6.</summary>
        Sunday,
    }
}
