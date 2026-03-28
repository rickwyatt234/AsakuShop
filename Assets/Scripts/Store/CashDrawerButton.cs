using AsakuShop.Core;
using UnityEngine;

namespace AsakuShop.Store
{
    // Attach to each denomination-slot collider that overlays the cash register drawers.
    // Each slot represents a single yen denomination (e.g. ¥1000, ¥500, ¥100, ¥50, ¥10, ¥5, ¥1).
    // When the player interacts with a slot during the change-giving phase, CashRegister.AddChange()
    // is called to accumulate that denomination toward the total change owed.
    //
    // Setup:
    //   1. Create empty child GameObjects on the cash register — one per denomination drawer.
    //   2. Give each a BoxCollider sized to cover the physical drawer on the mesh.
    //   3. Add this component and assign the CashRegister reference and denomination value.
    //   4. Ensure your player interaction system calls IInteractable.OnInteract() on click/press.
    public class CashDrawerButton : MonoBehaviour, IInteractable
    {
        [SerializeField, Tooltip("The cash register this denomination drawer belongs to.")]
        private CashRegister register;

        [SerializeField, Tooltip("The yen denomination this drawer dispenses (e.g. 1000, 500, 100, 50, 10, 5, 1).")]
        private int denominationAmount;

        public void OnInteract()
        {
            register?.AddChange(denominationAmount);
        }

        public void OnExamine() { }
    }
}
