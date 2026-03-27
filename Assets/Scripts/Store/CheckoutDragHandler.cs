using UnityEngine;

namespace AsakuShop.Store
{
    //world-space drag of CheckoutItem objects.
    // Attach to a manager GameObject on the counter (active only during checkout mode).
    public class CheckoutDragHandler : MonoBehaviour
    {
        [SerializeField] private LayerMask checkoutItemLayerMask;
        [SerializeField] private float snapThreshold = 0.3f;

        [Header("Snap Zone Transforms (set by CheckoutCounter)")]
        [SerializeField] private Transform scanZoneTransform;
        [SerializeField] private Transform bagZoneTransform;

        private CheckoutItem draggingItem;
        private Camera mainCam;
        private Plane dragPlane;
        private Rigidbody draggingRb;

        private void Awake()
        {
            mainCam = Camera.main;
        }

        private void Update()
        {
            if (UnityEngine.Input.GetMouseButtonDown(0))
                TryBeginDrag();

            if (draggingItem != null && UnityEngine.Input.GetMouseButton(0))
                ContinueDrag();

            if (draggingItem != null && UnityEngine.Input.GetMouseButtonUp(0))
                EndDrag();
        }

        private void TryBeginDrag()
        {
            Ray ray = mainCam.ScreenPointToRay(UnityEngine.Input.mousePosition);
            if (!Physics.Raycast(ray, out RaycastHit hit, 100f, checkoutItemLayerMask)) return;

            var item = hit.collider.GetComponent<CheckoutItem>();
            if (item == null) return;

            draggingItem = item;
            dragPlane    = new Plane(Vector3.up, hit.point);

            draggingRb = draggingItem.GetComponent<Rigidbody>();
            if (draggingRb != null)
            {
                draggingRb.isKinematic = true;
                draggingRb.useGravity  = false;
            }
        }

        private void ContinueDrag()
        {
            Ray ray = mainCam.ScreenPointToRay(UnityEngine.Input.mousePosition);
            if (dragPlane.Raycast(ray, out float distance))
            {
                Vector3 worldPoint = ray.GetPoint(distance);
                draggingItem.transform.position = worldPoint;
            }
        }

        private void EndDrag()
        {
            if (draggingRb != null)
            {
                draggingRb.isKinematic = false;
                draggingRb.useGravity  = true;
            }

            // Check proximity to snap zones
            bool snappedToScan = scanZoneTransform != null
                && Vector3.Distance(draggingItem.transform.position, scanZoneTransform.position) <= snapThreshold;
            bool snappedToBag  = bagZoneTransform  != null
                && Vector3.Distance(draggingItem.transform.position, bagZoneTransform.position) <= snapThreshold;

            if (snappedToScan)
                draggingItem.SnapToScanZone();
            else if (snappedToBag)
                draggingItem.SnapToBagZone();
            // else: item stays where dropped

            draggingItem = null;
            draggingRb   = null;
        }

        //Called by CheckoutCounter to configure snap zones at runtime.
        public void Configure(Transform scanZone, Transform bagZone)
        {
            scanZoneTransform = scanZone;
            bagZoneTransform  = bagZone;
        }
    }
}
