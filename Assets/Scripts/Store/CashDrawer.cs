using DG.Tweening;
using UnityEngine;

namespace AsakuShop.Store
{
    // Attach to the physical cash-drawer GameObject (child of the cash register model).
    // Call Open() to slide the drawer forward all the way out, then wrap it up and backwards on top of the register itself
    // to save space.
    // Call Close() to wrap it forwards and down, then slide it back in.
    public class CashDrawer : MonoBehaviour
    {
        [SerializeField, Tooltip("Local-space translation applied when the drawer opens all the way.")]
        private Vector3 slideOpenOffset_1 = new Vector3(0f, 0f, 0.3f);
        [SerializeField, Tooltip("Local-space translation applied when the drawer wraps forward on top of the register.")]
        private Vector3 slideOpenOffset_2 = new Vector3(0f, 0f, 0.15f);

        [SerializeField, Tooltip("Seconds the slide animation takes.")]
        private float duration = 1f;

        private Vector3 closedPosition;
        private bool    isOpen;

        private void Awake()
        {
            closedPosition = transform.localPosition;
        }

        // Slides the drawer open. Safe to call even if already open.
        public void Open()
        {
            if (isOpen) return;
            isOpen = true;
            // Slide forward all the way, then wrap back on top of the register.
            transform.DOLocalMove(closedPosition + slideOpenOffset_1, duration).SetEase(Ease.OutQuad).OnComplete(() =>
            {
                transform.DOLocalMove(closedPosition + slideOpenOffset_2, duration).SetEase(Ease.OutQuad);
            });
        }

        // Slides the drawer closed. Safe to call even if already closed.
        public void Close()
        {
            if (!isOpen) return;
            isOpen = false;
            // Wrap forward on top of the register, then slide back in.
            transform.DOLocalMove(closedPosition + slideOpenOffset_2, duration).SetEase(Ease.OutQuad).OnComplete(() =>
            {
                transform.DOLocalMove(closedPosition, duration).SetEase(Ease.OutQuad);
            });
        }
    }
}
