using AsakuShop.Core;
using UnityEngine;

namespace AsakuShop.Store
{
    // Attach to each physical drawer inside the 3D cash register model.
    // The player interacts with a drawer to add that denomination to the change being counted out.
    public class CashDrawerButton : MonoBehaviour, IInteractable
    {
        [SerializeField, Tooltip("The cash register this drawer belongs to.")]
        private CashRegister register;

        [SerializeField, Tooltip("The yen denomination this drawer dispenses (e.g. 1000, 500, 100).")]
        private int denominationAmount;

        public void OnInteract()
        {
            register?.Draw(denominationAmount);
        }

        public void OnExamine() { }
    }
}
