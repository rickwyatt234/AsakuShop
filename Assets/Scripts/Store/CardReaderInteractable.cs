using AsakuShop.Core;
using UnityEngine;

namespace AsakuShop.Store
{
    // Attach to the card reader 3D object on the checkout counter.
    // When the player interacts with it during a CardPay state, the payment terminal zooms in.
    public class CardReaderInteractable : MonoBehaviour, IInteractable
    {
        [SerializeField, Tooltip("The checkout counter this card reader belongs to.")]
        private CheckoutCounter counter;

        public void OnInteract()
        {
            if (counter == null || counter.CurrentState != CheckoutCounter.State.CardPay) return;
            counter.BeginCardPayment();
        }

        public void OnExamine() { }
    }
}
