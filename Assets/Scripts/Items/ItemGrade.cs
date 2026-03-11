using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AsakuShop.Items
{
    /// <summary>
    /// Quality tier of an item, running from <see cref="F"/> (worst) to
    /// <see cref="SS"/> (best). Grade affects the sell price, customer
    /// satisfaction score, and crafting output quality.
    /// </summary>
    public enum ItemGrade
    {
        /// <summary>Lowest grade — poor quality, lowest price multiplier.</summary>
        F  = 0,
        /// <summary>Below-average grade.</summary>
        D  = 1,
        /// <summary>Average / standard grade.</summary>
        C  = 2,
        /// <summary>Above-average grade — good quality, premium price.</summary>
        B  = 3,
        /// <summary>High-quality grade — noticeably better than average.</summary>
        A  = 4,
        /// <summary>Superb grade — rare, commands a high price premium.</summary>
        S  = 5,
        /// <summary>
        /// Legendary grade — exceptionally rare. UI should display a sparkle
        /// effect alongside the grade indicator.
        /// </summary>
        SS = 6,
    }

    /// <summary>
    /// Extension methods and utility helpers for <see cref="ItemGrade"/>.
    /// </summary>
    public static class ItemGradeExtensions
    {
        // ── Numeric conversion ────────────────────────────────────────────────────

        /// <summary>
        /// Returns the numeric value (0–6) that corresponds to
        /// <paramref name="grade"/>.
        /// </summary>
        /// <param name="grade">The grade to convert.</param>
        /// <returns>An integer in the range 0–6.</returns>
        public static int ToNumeric(this ItemGrade grade)
        {
            return (int)grade;
        }

        /// <summary>
        /// Converts a raw integer to the corresponding <see cref="ItemGrade"/>,
        /// clamping the value to the valid range 0–6.
        /// </summary>
        /// <param name="value">The integer to convert.</param>
        /// <returns>The <see cref="ItemGrade"/> that maps to the clamped value.</returns>
        public static ItemGrade FromNumeric(int value)
        {
            return (ItemGrade)Mathf.Clamp(value, (int)ItemGrade.F, (int)ItemGrade.SS);
        }

        // ── Display helpers ───────────────────────────────────────────────────────

        /// <summary>
        /// Returns the short letter label shown in the UI, e.g. <c>"A"</c> or
        /// <c>"SS"</c>.
        /// </summary>
        /// <param name="grade">The grade to display.</param>
        /// <returns>A one-or-two-character string label.</returns>
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

        /// <summary>
        /// Returns the UI colour associated with <paramref name="grade"/>.
        /// <list type="bullet">
        ///   <item><description>F — dark grey</description></item>
        ///   <item><description>D — grey</description></item>
        ///   <item><description>C — green</description></item>
        ///   <item><description>B — blue</description></item>
        ///   <item><description>A — red</description></item>
        ///   <item><description>S — yellow</description></item>
        ///   <item><description>SS — white (UI should add a sparkle effect)</description></item>
        /// </list>
        /// </summary>
        /// <param name="grade">The grade to retrieve a colour for.</param>
        /// <returns>A <see cref="Color"/> for use in UI elements.</returns>
        public static Color ToColor(this ItemGrade grade)
        {
            return grade switch
            {
                ItemGrade.F  => new Color(0.3f, 0.3f, 0.3f),
                ItemGrade.D  => new Color(0.6f, 0.6f, 0.6f),
                ItemGrade.C  => new Color(0.2f, 0.8f, 0.2f),
                ItemGrade.B  => new Color(0.2f, 0.5f, 1.0f),
                ItemGrade.A  => new Color(1.0f, 0.2f, 0.2f),
                ItemGrade.S  => new Color(1.0f, 0.85f, 0.0f),
                ItemGrade.SS => new Color(1.0f, 1.0f, 1.0f),
                _            => Color.white,
            };
        }

        // ── Calculation helpers ───────────────────────────────────────────────────

        /// <summary>
        /// Computes the average grade of a collection of grades by averaging
        /// their numeric values and rounding to the nearest integer. Used by the
        /// crafting system to determine output quality from ingredient grades.
        /// </summary>
        /// <param name="grades">The grades to average. Must not be null.</param>
        /// <returns>
        /// The <see cref="ItemGrade"/> closest to the arithmetic mean, clamped to
        /// the valid range. Returns <see cref="ItemGrade.F"/> for an empty
        /// sequence.
        /// </returns>
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

        /// <summary>
        /// Returns the next grade down from <paramref name="grade"/>. Applying
        /// decay to <see cref="ItemGrade.F"/> returns <see cref="ItemGrade.F"/>
        /// (floor). Called during the overnight spoilage pass.
        /// </summary>
        /// <param name="grade">The current grade of an item.</param>
        /// <returns>
        /// The grade one step lower, or <see cref="ItemGrade.F"/> if already at
        /// the minimum.
        /// </returns>
        public static ItemGrade Decay(this ItemGrade grade)
        {
            int next = (int)grade - 1;
            return FromNumeric(next);
        }
    }
}
