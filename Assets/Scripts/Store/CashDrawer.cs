using DG.Tweening;
using UnityEngine;

namespace AsakuShop.Store
{
    // Attach to the physical cash-drawer GameObject (child of the cash register model).
    // Call Open() to slide the drawer forward when a cash payment is confirmed.
    // Call Close() to slide it back (useful if you later need to reset the scene).
    //
    // Setup in the Inspector:
    //   • Set openOffset to the LOCAL direction and distance the drawer should travel
    //     (e.g. Z = 0.3 slides it 0.3 units along its local forward axis).
    //   • Adjust duration to taste.
    public class CashDrawer : MonoBehaviour
    {
        [SerializeField, Tooltip("Local-space translation applied when the drawer opens " +
            "(e.g. (0, 0, 0.3) slides it 0.3 units along its local forward axis).")]
        private Vector3 openOffset = new Vector3(0f, 0f, 0.3f);

        [SerializeField, Tooltip("Seconds the slide animation takes.")]
        private float duration = 0.4f;

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
            transform.DOLocalMove(closedPosition + openOffset, duration).SetEase(Ease.OutQuad);
        }

        // Slides the drawer closed. Safe to call even if already closed.
        public void Close()
        {
            if (!isOpen) return;
            isOpen = false;
            transform.DOLocalMove(closedPosition, duration).SetEase(Ease.InQuad);
        }
    }
}
