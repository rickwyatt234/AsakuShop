using AsakuShop.Core;
using UnityEngine;

namespace AsakuShop.Store
{
    // Legacy component — denomination drawers are no longer used.
    // The cash register now accepts numpad input via world-space UI buttons
    // that call CashRegister.Append("0")–Append("9") and ClearEntry().
    //
    // This script is kept for reference. Attach CashDrawer to the physical
    // drawer GameObject instead if you need a sliding-open animation.
    [System.Obsolete("Denomination drawers replaced by world-space numpad UI buttons calling CashRegister.Append().")]
    public class CashDrawerButton : MonoBehaviour, IInteractable
    {
        [SerializeField, Tooltip("The cash register this drawer belongs to.")]
        private CashRegister register;

        [SerializeField, Tooltip("The yen denomination this drawer dispenses (e.g. 1000, 500, 100).")]
        private int denominationAmount;

        public void OnInteract() { }

        public void OnExamine() { }
    }
}
