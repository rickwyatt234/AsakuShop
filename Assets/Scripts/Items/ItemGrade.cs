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
        private static readonly Dictionary<ItemGrade, float> GradeMarkups = new()
        {
            { ItemGrade.F,  0.75f },
            { ItemGrade.D,  0.9f },
            { ItemGrade.C,  1.0f },
            { ItemGrade.B,  1.25f },
            { ItemGrade.A,  1.5f },
            { ItemGrade.S,  2.0f },
            { ItemGrade.SS, 3.0f },
        };

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

        /// Returns the price markup multiplier for this grade (e.g. 1.25 for B)
        public static float GetPriceMarkup(this ItemGrade grade)
        {
            return GradeMarkups.TryGetValue(grade, out var markup) ? markup : 1.0f;
        }

        /// Returns a markup string for UI display, e.g. "x1.25"
        public static string GetMarkupString(this ItemGrade grade)
        {
            float markup = grade.GetPriceMarkup();
            switch (grade)
            {
                case ItemGrade.F:
                    return $"x{markup} :'(";
                case ItemGrade.D:
                    return $"x{markup} . . .";
                case ItemGrade.C:
                    return $"x{markup}";
                case ItemGrade.B:
                    return $"x{markup}!";
                case ItemGrade.A:
                    return $"x{markup}!!";
                case ItemGrade.S:
                    return $"x{markup}!!!";
                case ItemGrade.SS:
                    return $"x{markup}!! :O !!";
                default:
                    return string.Empty;
            }
        }

        /// Returns a rich-text colored grade string for TextMeshPro, e.g. "<color=#FF3333>A</color>"
        public static string ToColoredString(this ItemGrade grade)
        {
            Color color = grade.ToColor();
            string hex = ColorUtility.ToHtmlStringRGB(color);
            return $"<color=#{hex}>{grade.ToDisplayString()}</color>";
        }

        /// Computes the average grade of a collection of grades by averaging
        /// their numeric values and rounding to the nearest integer.
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