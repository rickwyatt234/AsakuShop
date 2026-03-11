using System;
using System.Collections.Generic;

namespace AsakuShop.Core
{
    [Serializable]
    public class SaveData
    {

        public int SaveVersion;
        public string SaveTimestamp;
        public int DayIndex;
        public string DayOfWeek;
        public int Hour;
        public int Minute;
        public GamePhase LastPhase;
        public Dictionary<string, string> SystemData = new Dictionary<string, string>();
    }
}