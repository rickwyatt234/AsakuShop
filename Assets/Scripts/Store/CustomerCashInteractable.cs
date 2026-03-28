using AsakuShop.Core;
using UnityEngine;

namespace AsakuShop.Store
{
    // Temporarily added to the current customer's GameObject while a cash payment is pending.
    // The player interacts with the customer's money (this component) to open the cash register.
    [DisallowMultipleComponent]
    public class CustomerCashInteractable : MonoBehaviour, IInteractable
    {
        private CheckoutCounter counter;

        public void Initialize(CheckoutCounter counter)
        {
            this.counter = counter;
        }

        public void OnInteract()
        {
            if (counter == null || counter.CurrentState != CheckoutCounter.State.CashPay) return;
            counter.BeginCashPayment();
        }

        public void OnExamine() { }
    }
}
