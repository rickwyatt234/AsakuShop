using DG.Tweening;
using UnityEngine;
using AsakuShop.Items;

namespace AsakuShop.Store
{
    // Represents an item placed on the checkout belt.
    // Attach this component at runtime (via CheckoutCounter.PlaceProducts).
    // Supports world-space drag snapping to scan/bag zones.
    [RequireComponent(typeof(BoxCollider))]
    public class CheckoutItem : MonoBehaviour
    {
        public ItemInstance Item { get; private set; }

        private event System.Action onScan;

        [Header("Snap Zones (assigned by CheckoutCounter)")]
        [SerializeField] private Transform scanZoneTarget;
        [SerializeField] private Transform bagZoneTarget;

        private const float SNAP_ANIM_DURATION = 0.2f;

        // Initializes this CheckoutItem with an item instance and a scan callback.
        public void Initialize(ItemInstance item, System.Action onScanCallback,
                               Transform scanZone = null, Transform bagZone = null)
        {
            Item = item;
            onScan += onScanCallback;
            scanZoneTarget = scanZone;
            bagZoneTarget  = bagZone;
        }

        //Snaps the item to the scan zone, then fires the scan event.
        public void SnapToScanZone()
        {
            if (scanZoneTarget == null)
            {
                Scan();
                return;
            }

            transform.DOMove(scanZoneTarget.position, SNAP_ANIM_DURATION)
                .SetEase(Ease.OutQuad)
                .OnComplete(Scan);
        }

        //Snaps the item to the bag zone (post-scan position).
        public void SnapToBagZone()
        {
            if (bagZoneTarget == null) return;

            transform.DOMove(bagZoneTarget.position, SNAP_ANIM_DURATION)
                .SetEase(Ease.OutQuad);
        }

        //Manually triggers the scan callback.
        public void Scan()
        {
            if (onScan != null)
                onScan.Invoke();
            else
                Debug.LogWarning($"[CheckoutItem] No scan callback registered for '{name}'.");
        }
    }
}
