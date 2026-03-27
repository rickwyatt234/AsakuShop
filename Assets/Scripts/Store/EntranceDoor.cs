using DG.Tweening;
using UnityEngine;
using AsakuShop.Core;

namespace AsakuShop.Store
{
    [RequireComponent(typeof(BoxCollider))]
    public class EntranceDoor : MonoBehaviour, IInteractable
    {
        [SerializeField, Tooltip("Local euler angles when fully open.")]
        private Vector3 openAngle = new Vector3(0f, -90f, 0f);

        [SerializeField, Tooltip("Paired door that opens/closes alongside this one (optional).")]
        private EntranceDoor pairDoor;

        [SerializeField] private float animDuration = 0.35f;
        
        [SerializeField, Tooltip("Offset from door center to hinge point (e.g., left edge: -width/2, 0, 0).")]
        private Vector3 hingeOffset = Vector3.zero;

        private BoxCollider boxCollider;
        private bool isOpen;
        private bool isAnimating;
        private Transform hingePivot;

        public bool IsOpen => isOpen;

        private void Awake()
        {
            boxCollider = GetComponent<BoxCollider>();
            
            // Create hinge pivot if offset is set
            if (hingeOffset != Vector3.zero)
            {
                hingePivot = new GameObject("HingePivot").transform;
                hingePivot.SetParent(transform.parent);
                hingePivot.localPosition = transform.localPosition + hingeOffset;
                hingePivot.localRotation = transform.localRotation;
                
                // Parent the door to the hinge pivot
                transform.SetParent(hingePivot);
                transform.localPosition = -hingeOffset;
            }
            else
            {
                hingePivot = transform;
            }
        }

        public void OpenIfClosed()
        {
            if (!isOpen) Open();
            if (pairDoor != null && !pairDoor.IsOpen) pairDoor.Open();
        }

        public void Open()
        {
            if (isAnimating || isOpen) return;
            isAnimating = true;
            boxCollider.enabled = false;

            hingePivot.DOLocalRotate(openAngle, animDuration)
                .SetEase(Ease.OutQuad)
                .OnComplete(() =>
                {
                    isOpen      = true;
                    isAnimating = false;
                    boxCollider.enabled = true;
                });
        }

        public void Close()
        {
            if (isAnimating || !isOpen) return;
            isAnimating = true;
            boxCollider.enabled = false;

            hingePivot.DOLocalRotate(Vector3.zero, animDuration)
                .SetEase(Ease.OutQuad)
                .OnComplete(() =>
                {
                    isOpen      = false;
                    isAnimating = false;
                    boxCollider.enabled = true;
                });
        }

        public void OnInteract()
        {
            if (isOpen)
            {
                Close();
                if (pairDoor != null) pairDoor.Close();
            }
            else
            {
                Open();
                if (pairDoor != null) pairDoor.Open();
            }
        }

        public void OnExamine() { }
    }
}