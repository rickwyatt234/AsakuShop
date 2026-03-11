using System;
using AsakuShop.Core;
using UnityEngine;

namespace AsakuShop.Items
{
    /// <summary>
    /// Serializable snapshot of a single <see cref="ItemInstance"/> used by
    /// the save/load system. Storage systems implement
    /// <see cref="AsakuShop.Core.ISaveParticipant"/> and persist their
    /// inventories as arrays of <see cref="ItemSaveData"/> blobs.
    /// </summary>
    [Serializable]
    public class ItemSaveData
    {
        // ── Fields ────────────────────────────────────────────────────────────────

        /// <summary>Preserved <see cref="ItemInstance.InstanceId"/> for identity matching.</summary>
        public string InstanceId;

        /// <summary>
        /// The <see cref="ItemDefinition.ItemId"/> used to look up the definition
        /// in <see cref="ItemRegistry"/> when restoring.
        /// </summary>
        public string ItemId;

        /// <summary>
        /// Numeric encoding of <see cref="ItemInstance.CurrentGrade"/> via
        /// <see cref="ItemGradeExtensions.ToNumeric"/>.
        /// </summary>
        public int GradeValue;

        /// <summary>Preserved <see cref="ItemInstance.PurchasePrice"/>.</summary>
        public float PurchasePrice;

        /// <summary>
        /// <see cref="GameTime.DayIndex"/> of the acquisition time.
        /// </summary>
        public int AcquiredDayIndex;

        /// <summary><see cref="GameTime.Hour"/> of the acquisition time.</summary>
        public int AcquiredHour;

        /// <summary><see cref="GameTime.Minute"/> of the acquisition time.</summary>
        public int AcquiredMinute;

        // ── Factory methods ───────────────────────────────────────────────────────

        /// <summary>
        /// Creates a <see cref="ItemSaveData"/> snapshot from a live
        /// <see cref="ItemInstance"/>.
        /// </summary>
        /// <param name="instance">The instance to snapshot. Must not be <c>null</c>.</param>
        /// <returns>A fully populated save snapshot.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="instance"/> is <c>null</c>.
        /// </exception>
        public static ItemSaveData From(ItemInstance instance)
        {
            if (instance == null) throw new ArgumentNullException(nameof(instance));

            return new ItemSaveData
            {
                InstanceId       = instance.InstanceId,
                ItemId           = instance.Definition.ItemId,
                GradeValue       = instance.CurrentGrade.ToNumeric(),
                PurchasePrice    = instance.PurchasePrice,
                AcquiredDayIndex = instance.AcquiredTime.DayIndex,
                AcquiredHour     = instance.AcquiredTime.Hour,
                AcquiredMinute   = instance.AcquiredTime.Minute,
            };
        }

        /// <summary>
        /// Reconstructs an <see cref="ItemInstance"/> from a saved snapshot by
        /// looking up the <see cref="ItemDefinition"/> in the provided registry.
        /// </summary>
        /// <param name="data">The snapshot to restore. Must not be <c>null</c>.</param>
        /// <param name="registry">
        /// The <see cref="ItemRegistry"/> used to resolve the item definition.
        /// Must not be <c>null</c>.
        /// </param>
        /// <param name="fallbackTime">
        /// The in-game time to use as the acquisition time when the saved time
        /// data is unavailable or cannot be parsed.
        /// </param>
        /// <returns>
        /// A reconstructed <see cref="ItemInstance"/>, or <c>null</c> if the
        /// <see cref="ItemId"/> cannot be resolved in the registry.
        /// </returns>
        public static ItemInstance Restore(ItemSaveData data, ItemRegistry registry, GameTime fallbackTime)
        {
            if (data == null)
            {
                Debug.LogWarning("[ItemSaveData] Restore called with null data.");
                return null;
            }

            if (registry == null)
            {
                Debug.LogWarning("[ItemSaveData] Restore called with null registry.");
                return null;
            }

            if (!registry.TryGet(data.ItemId, out ItemDefinition definition))
            {
                Debug.LogWarning($"[ItemSaveData] Could not find ItemId '{data.ItemId}' in ItemRegistry — skipping restore.");
                return null;
            }

            GameTime acquiredTime = GameTime.FromMinutes(
                data.AcquiredDayIndex,
                data.AcquiredHour * 60 + data.AcquiredMinute);

            ItemGrade grade = ItemGradeExtensions.FromNumeric(data.GradeValue);

            return new ItemInstance(
                data.InstanceId,
                definition,
                grade,
                data.PurchasePrice,
                acquiredTime);
        }
    }
}
