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
        private float snapThreshold = 0.3f;
        public event System.Action onScanned;
        public event System.Action onBagged;

        [Header("Snap Zones (assigned by CheckoutCounter)")]
        [SerializeField] private Transform scanZoneTarget;
        [SerializeField] private Transform bagZoneTarget;

        private const float SNAP_ANIM_DURATION = 0.2f;

        // Initializes this CheckoutItem with an item instance and a scan callback.
        public void Initialize(ItemInstance item, System.Action onScanCallback, System.Action onBagCallback,
                               Transform scanZone = null, Transform bagZone = null)
        {
            Item = item;
            onScanned += onScanCallback; // Scan callback is required, as scanning is mandatory for checkout. If not provided, the item will still snap to the scan zone but won't trigger any scan logic.
            onBagged   += onBagCallback; // Default bagged behavior is same as scanned, but can be overridden by subscribing to onBagged separately.
            scanZoneTarget = scanZone;
            bagZoneTarget  = bagZone;
        }


#region Snap Logic (called by CheckoutDragHandler)
        public bool CanSnapToScanZone()
        {
            if (scanZoneTarget == null) return false;

            //if item hasn't been scanned yet, allow snap if within threshold distance of scan zone. If item has already been scanned, it should only be allowed to snap to bag zone, not scan zone.
            if (onScanned != null)
                return false;
            return Vector3.Distance(transform.position, scanZoneTarget.position) <= snapThreshold;
        }

        public bool CanSnapToBagZone()
        {
            if (bagZoneTarget == null) return false;
            //if item has been scanned, allow snap if within threshold distance of bag zone. If item hasn't been scanned yet, it should only be allowed to snap to scan zone, not bag zone.
            if (onScanned == null)
                return false;
            return Vector3.Distance(transform.position, bagZoneTarget.position) <= snapThreshold;
        }

        //Snaps the item to the scan zone, then fires the scan event.
        public void SnapToScanZone()
        {
            if (scanZoneTarget == null) 
            {
                Debug.LogWarning($"[CheckoutItem] No scan zone target assigned for '{name}'. Cannot snap to scan zone.");
                return;
            }
            else
            {
                transform.DOMove(scanZoneTarget.position, SNAP_ANIM_DURATION)
                    .SetEase(Ease.OutQuad)
                    .OnComplete(Scan);
            }

        }

        //Snaps the item to the bag zone (post-scan position).
        public void SnapToBagZone()
        {
            if (bagZoneTarget == null) 
            {
                Debug.LogWarning($"[CheckoutItem] No bag zone target assigned for '{name}'. Cannot snap to bag zone.");
                return;
            }
            else
            {
                transform.DOMove(bagZoneTarget.position, SNAP_ANIM_DURATION)
                    .SetEase(Ease.OutQuad)
                    .OnComplete(Bag);
            }

        }
#endregion
 
 
#region Scan/Bag Callbacks 
        //Manually triggers the scan callback.
        public void Scan()
        {
            if (onScanned != null)
                onScanned.Invoke();
            else
                Debug.LogWarning($"[CheckoutItem] No scan callback registered for '{name}'.");
        }
        public void Bag()
        {
            if (onBagged != null)
                onBagged.Invoke();
            else
                Debug.LogWarning($"[CheckoutItem] No bag callback registered for '{name}'.");
        }
#endregion
    }
}
