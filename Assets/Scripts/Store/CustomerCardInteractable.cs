using AsakuShop.Core;
using UnityEngine;

namespace AsakuShop.Store
{
    // Temporarily added to the current customer's GameObject while a card payment is pending.
    // The player interacts with the customer's card (this component) to activate the card reader.
    [DisallowMultipleComponent]
    public class CustomerCardInteractable : MonoBehaviour, IInteractable
    {
        private CheckoutCounter counter;

        public void Initialize(CheckoutCounter counter)
        {
            this.counter = counter;
        }

        public void OnInteract()
        {
            if (counter == null || counter.CurrentState != CheckoutCounter.State.CardPay) return;
            counter.BeginCardPayment();
        }

        public void OnExamine() { }
    }
}