using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AsakuShop.Items
{
    public enum ItemGrade
    {
        F  = 0,
        D  = 1,
        C  = 2,
        B  = 3,
        A  = 4,
        S  = 5,
        SS = 6,
    }

    public static class ItemGradeExtensions
    {
        /// Returns the numeric value (0–6) that corresponds to grade
        public static int ToNumeric(this ItemGrade grade)
        {
            return (int)grade;
        }

        /// Converts a raw integer to the corresponding ItemGrade
        /// clamping the value to the valid range 0–6.
        public static ItemGrade FromNumeric(int value)
        {
            return (ItemGrade)Mathf.Clamp(value, (int)ItemGrade.F, (int)ItemGrade.SS);
        }


        // Returns the short letter label shown in the UI
        public static string ToDisplayString(this ItemGrade grade)
        {
            return grade switch
            {
                ItemGrade.F  => "F",
                ItemGrade.D  => "D",
                ItemGrade.C  => "C",
                ItemGrade.B  => "B",
                ItemGrade.A  => "A",
                ItemGrade.S  => "S",
                ItemGrade.SS => "SS",
                _            => grade.ToString(),
            };
        }

        public static Color ToColor(this ItemGrade grade)
        {
            return grade switch
            {
                ItemGrade.F  => new Color(0.3f, 0.3f, 0.3f), // Dark gray for lowest grade.
                ItemGrade.D  => new Color(0.6f, 0.6f, 0.6f), // Lighter gray for below-average grade.
                ItemGrade.C  => new Color(0.2f, 0.8f, 0.2f), // Green for average/standard grade.
                ItemGrade.B  => new Color(0.2f, 0.5f, 1.0f), // Blue for above-average grade.
                ItemGrade.A  => new Color(1.0f, 0.2f, 0.2f), // Red for high-quality grade.
                ItemGrade.S  => new Color(1.0f, 0.85f, 0.0f), // Gold for superb grade.
                ItemGrade.SS => new Color(1.0f, 1.0f, 1.0f), // White with UI pearlescent effect for legendary grade.
                _            => Color.white,
            };
        }

        /// Computes the average grade of a collection of grades by averaging
        /// their numeric values and rounding to the nearest integer. Used by the
        /// crafting system to determine output quality from ingredient grades.
        public static ItemGrade Average(IEnumerable<ItemGrade> grades)
        {
            if (grades == null) throw new ArgumentNullException(nameof(grades));

            var list = grades as IList<ItemGrade> ?? grades.ToList();
            if (list.Count == 0) return ItemGrade.F;

            double sum = 0;
            foreach (var g in list)
                sum += (int)g;

            int rounded = (int)Math.Round(sum / list.Count, MidpointRounding.AwayFromZero);
            return FromNumeric(rounded);
        }

        public static ItemGrade Decay(this ItemGrade grade)
        {
            int next = (int)grade - 1;
            return FromNumeric(next);
        }
    }
}