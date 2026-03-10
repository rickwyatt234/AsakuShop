using System;
using System.Collections.Generic;

namespace AsakuShop.Core
{
    /// <summary>
    /// Serialisable wrapper written to disk by <see cref="SaveManager"/>.
    /// Increment <see cref="SaveManager.CurrentSaveVersion"/> and add a
    /// migration branch in <c>MigrateIfNeeded</c> whenever the schema changes.
    /// </summary>
    [Serializable]
    public class SaveData
    {
        /// <summary>
        /// Schema version. Compared against
        /// <see cref="SaveManager.CurrentSaveVersion"/> on load to detect
        /// files that need migration.
        /// </summary>
        public int SaveVersion;

        /// <summary>
        /// Real-world UTC date/time at which the save was written, formatted as
        /// ISO 8601 round-trip string (e.g. <c>"2025-06-01T08:30:00.0000000Z"</c>).
        /// </summary>
        public string SaveTimestamp;

        /// <summary>Saved value of <see cref="GameTime.DayIndex"/>.</summary>
        public int DayIndex;

        /// <summary>Saved value of <see cref="GameTime.DayOfWeek"/> as a string.</summary>
        public string DayOfWeek;

        /// <summary>Saved value of <see cref="GameTime.Hour"/> (0–23).</summary>
        public int Hour;

        /// <summary>Saved value of <see cref="GameTime.Minute"/> (0–59).</summary>
        public int Minute;

        /// <summary>
        /// The <see cref="GamePhase"/> that was active when the game was saved.
        /// </summary>
        public GamePhase LastPhase;

        /// <summary>
        /// Per-system state blobs. The key is
        /// <see cref="ISaveParticipant.SaveKey"/>; the value is a JSON string
        /// produced by serialising that system's <see cref="ISaveParticipant.CaptureState"/> result.
        /// </summary>
        public Dictionary<string, string> SystemData = new Dictionary<string, string>();
    }
}
