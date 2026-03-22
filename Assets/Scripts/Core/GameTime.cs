using System;

namespace AsakuShop.Core
{
    [Serializable]
    public struct GameTime
    {

        public int DayIndex;
        public GameDayOfWeek DayOfWeek;
        public int Hour;
        public int Minute;

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

        public string DisplayDayOfWeek => DayOfWeek.ToString();

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

        public int ToTotalMinutes()
        {
            return Hour * TimeConstants.MinutesPerHour + Minute;
        }

        public bool IsEarlierThan(GameTime other)
        {
            return ToTotalMinutes() < other.ToTotalMinutes();
        }

        public override string ToString()
        {
            return $"{DisplayDayOfWeek} {DisplayTime} (Day {DayIndex})";
        }


    }
}