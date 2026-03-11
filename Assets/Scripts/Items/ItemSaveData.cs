using System;
using AsakuShop.Core;
using UnityEngine;

namespace AsakuShop.Items
{
    // Serializable snapshot of a single ItemInstance used by the save/load system. Storage systems implement
    // AsakuShop.Core.ISaveParticipant and persist their inventories as arrays of ItemSaveData blobs.
    [Serializable]
    public class ItemSaveData
    {
        //Preserved ItemInstance.InstanceId for identity matching
        public string InstanceId;

        //The ItemDefinition.ItemId used to look up the definition in ItemRegistry when restoring
        public string ItemId;

        //Numeric encoding of ItemInstance.CurrentGrade via ItemGradeExtensions.ToNumeric
        public int GradeValue;

        //Preserved ItemInstance.PurchasePrice
        public float PurchasePrice;

        //GameTime.DayIndex of the acquisition time.
        public int AcquiredDayIndex;

        //GameTime.Hour of the acquisition time.
        public int AcquiredHour;

        //GameTime.Minuteof the acquisition time.
        public int AcquiredMinute;

        /// Creates a ItemSaveData snapshot from a live ItemInstance
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


        /// Reconstructs an ItemInstance from a saved snapshot by looking up the ItemDefinition in the provided registry.
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