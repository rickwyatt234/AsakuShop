using System;

namespace AsakuShop.Core
{
    /// <summary>
    /// An immutable, serializable struct that represents a single moment in
    /// in-game time: which day, which day-of-week, the hour (0–23), and the
    /// minute (0–59).
    /// </summary>
    [Serializable]
    public struct GameTime
    {
        // ── Fields ───────────────────────────────────────────────────────────────

        /// <summary>
        /// Total number of "Sleep" cycles since game start. Day 0 is the first
        /// day; increments each time midnight is crossed via sleep or time advance.
        /// </summary>
        public int DayIndex;

        /// <summary>
        /// Derived day of the week. Day 0 == Monday; wraps every
        /// <see cref="TimeConstants.DaysPerWeek"/> days.
        /// </summary>
        public GameDayOfWeek DayOfWeek;

        /// <summary>Hour in 24-hour format (0–23).</summary>
        public int Hour;

        /// <summary>Minute within the current hour (0–59).</summary>
        public int Minute;

        // ── Derived display properties ────────────────────────────────────────────

        /// <summary>
        /// Returns a human-readable 12-hour time string, e.g. <c>"7:05 AM"</c>
        /// or <c>"12:30 PM"</c>.
        /// </summary>
        public string DisplayTime
        {
            get
            {
                string period = Hour < 12 ? "AM" : "PM";
                int displayHour = Hour % 12;
                if (displayHour == 0) displayHour = 12;
                return $"{displayHour}:{Minute:D2} {period}";
            }
        }

        /// <summary>
        /// Returns the name of the current <see cref="DayOfWeek"/> as a string,
        /// e.g. <c>"Monday"</c>.
        /// </summary>
        public string DisplayDayOfWeek => DayOfWeek.ToString();

        // ── Constructors / factory ─────────────────────────────────────────────────

        /// <summary>
        /// Creates a <see cref="GameTime"/> from a day index and a total minutes
        /// value representing elapsed minutes since midnight.
        /// </summary>
        /// <param name="dayIndex">Absolute day index (0 = game start).</param>
        /// <param name="totalMinutesInDay">
        /// Minutes elapsed since midnight (0–1439). Values ≥ 1440 will roll over
        /// to the next day automatically.
        /// </param>
        /// <returns>A fully initialised <see cref="GameTime"/>.</returns>
        public static GameTime FromMinutes(int dayIndex, int totalMinutesInDay)
        {
            // Roll over extra days that may be embedded in totalMinutesInDay.
            int extraDays = totalMinutesInDay / TimeConstants.MinutesPerDay;
            int remainder = totalMinutesInDay % TimeConstants.MinutesPerDay;
            if (remainder < 0)
            {
                remainder += TimeConstants.MinutesPerDay;
                extraDays--;
            }

            int finalDay = dayIndex + extraDays;
            return new GameTime
            {
                DayIndex = finalDay,
                DayOfWeek = (GameDayOfWeek)(finalDay % TimeConstants.DaysPerWeek),
                Hour = remainder / TimeConstants.MinutesPerHour,
                Minute = remainder % TimeConstants.MinutesPerHour,
            };
        }

        // ── Conversion helpers ─────────────────────────────────────────────────────

        /// <summary>
        /// Converts the current <see cref="Hour"/> and <see cref="Minute"/> to
        /// total minutes elapsed since midnight (0–1439).
        /// </summary>
        /// <returns>Minutes since midnight.</returns>
        public int ToTotalMinutes()
        {
            return Hour * TimeConstants.MinutesPerHour + Minute;
        }

        /// <summary>
        /// Returns <c>true</c> when this time is earlier in the day than
        /// <paramref name="other"/>, comparing only <see cref="Hour"/> and
        /// <see cref="Minute"/> (same-day comparison).
        /// </summary>
        /// <param name="other">The <see cref="GameTime"/> to compare against.</param>
        /// <returns><c>true</c> if this moment is earlier than <paramref name="other"/>.</returns>
        public bool IsEarlierThan(GameTime other)
        {
            return ToTotalMinutes() < other.ToTotalMinutes();
        }

        // ── Object overrides ────────────────────────────────────────────────────────

        /// <summary>
        /// Returns a full human-readable string, e.g.
        /// <c>"Monday 7:05 AM (Day 0)"</c>.
        /// </summary>
        public override string ToString()
        {
            return $"{DisplayDayOfWeek} {DisplayTime} (Day {DayIndex})";
        }
    }
}
