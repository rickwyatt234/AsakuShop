using UnityEngine;
using UnityEngine.EventSystems;

namespace AsakuShop.Store
{
    //world-space drag of CheckoutItem objects.
    // Attach to a manager GameObject on the counter (active only during checkout mode).
    public class CheckoutDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
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

        public void OnBeginDrag(PointerEventData eventData) => TryBeginDrag(); //Only sets draggingItem if we hit a valid CheckoutItem, otherwise does nothing.
        public void OnDrag(PointerEventData eventData) => ContinueDrag(); //Only updates position if draggingItem is set, otherwise does nothing.
        public void OnEndDrag(PointerEventData eventData) => EndDrag(); //Only applies physics and snapping if draggingItem is set, otherwise does nothing.

        private void TryBeginDrag()
        {
            Ray ray = mainCam.ScreenPointToRay(UnityEngine.Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, checkoutItemLayerMask))
            {
                draggingItem = hit.collider.GetComponentInParent<CheckoutItem>();
                if (draggingItem != null)
                {
                    draggingRb = draggingItem.GetComponent<Rigidbody>();
                    if (draggingRb != null)
                    {
                        draggingRb.isKinematic = true;
                        draggingRb.useGravity  = false;
                    }

                    // Set up drag plane parallel to camera view
                    dragPlane = new Plane(mainCam.transform.forward, draggingItem.transform.position);
                }
            }
        }

        private void ContinueDrag()
        {
            if (draggingItem == null) return;

            Ray ray = mainCam.ScreenPointToRay(UnityEngine.Input.mousePosition);
            if (dragPlane.Raycast(ray, out float enter))
            {
                Vector3 hitPoint = ray.GetPoint(enter);
                draggingItem.transform.position = hitPoint;
            }
        }

        private void EndDrag()
        {
            if (draggingItem == null) return;
            if (draggingRb != null)
            {
                draggingRb.isKinematic = false;
                draggingRb.useGravity  = true;
            }

            //Check for snap zones
            Collider[] nearbyColliders = Physics.OverlapSphere(draggingItem.transform.position, snapThreshold);
            foreach (Collider col in nearbyColliders)
            {
               if (col.transform == scanZoneTransform)
                {
                    if (draggingItem.CanSnapToScanZone())
                    {
                            draggingItem.OnScanned();
                    }
                }
                else if (col.transform == bagZoneTransform)
                {
                    if (draggingItem.CanSnapToBagZone())
                    {
                        draggingItem.OnBagged();
                    }
                }
            }
            draggingItem = null;
            draggingRb   = null;

            // // Check proximity to snap zones
            // bool snappedToScan = scanZoneTransform != null
            //     && Vector3.Distance(draggingItem.transform.position, scanZoneTransform.position) <= snapThreshold;
            // bool snappedToBag  = bagZoneTransform  != null
            //     && Vector3.Distance(draggingItem.transform.position, bagZoneTransform.position) <= snapThreshold;

            // if (snappedToScan)
            //     draggingItem.SnapToScanZone();
            // else if (snappedToBag)
            //     draggingItem.SnapToBagZone();
            // // else: item stays where dropped

            // draggingItem = null;
            // draggingRb   = null;
        }

        //Called by CheckoutCounter to configure snap zones at runtime.
        public void Configure(Transform scanZone, Transform bagZone)
        {
            scanZoneTransform = scanZone;
            bagZoneTransform  = bagZone;
        }








    }
}
