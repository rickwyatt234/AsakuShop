using System.Collections.Generic;
using System.Linq;
using AsakuShop.Items;
using UnityEngine;

namespace AsakuShop.Storage
{
    public class FIFOHelper : MonoBehaviour
    {
        public static ItemInstance RemoveWorstQuality(List<ItemInstance> items)
        {
            if (items == null || items.Count == 0)
                return null;

            ItemInstance worstItem = items.OrderBy(item => item.CurrentGrade).First();
            items.Remove(worstItem);
            return worstItem;
        }

        public static ItemInstance RemoveBestQuality(List<ItemInstance> items)
        {
            if (items == null || items.Count == 0)
                return null;

            ItemInstance bestItem = items.OrderByDescending(item => item.CurrentGrade).First();
            items.Remove(bestItem);
            return bestItem;
        }

        public static ItemInstance RemoveOldestItem(List<ItemInstance> items)
        {
            if (items == null || items.Count == 0)
                return null;
    
            ItemInstance oldestItem = items.First();
            items.Remove(oldestItem);
            return oldestItem;
        }
    }
}
