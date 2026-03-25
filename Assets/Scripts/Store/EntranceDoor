using DG.Tweening;
using UnityEngine;
using AsakuShop.Core;

namespace AsakuShop.Store
{
    // Entrance double-door. Optionally paired with a second EntranceDoor that
    // opens/closes in tandem. Customers trigger OpenIfClosed() via raycast.
    [RequireComponent(typeof(BoxCollider))]
    public class EntranceDoor : MonoBehaviour, IInteractable
    {
        [SerializeField, Tooltip("Local euler angles when fully open.")]
        private Vector3 openAngle = new Vector3(0f, -90f, 0f);

        [SerializeField, Tooltip("Paired door that opens/closes alongside this one (optional).")]
        private EntranceDoor pairDoor;

        [SerializeField] private float animDuration = 0.35f;

        private BoxCollider boxCollider;
        private bool isOpen;
        private bool isAnimating;

        public bool IsOpen => isOpen;

        private void Awake()
        {
            boxCollider = GetComponent<BoxCollider>();
        }

        //Opens this door (and the pair if assigned) if not already open.
        public void OpenIfClosed()
        {
            if (!isOpen) Open();
            if (pairDoor != null && !pairDoor.IsOpen) pairDoor.Open();
        }

        //Opens this door only.
        public void Open()
        {
            if (isAnimating || isOpen) return;
            isAnimating = true;
            boxCollider.enabled = false;

            transform.DOLocalRotate(openAngle, animDuration)
                .SetEase(Ease.OutQuad)
                .OnComplete(() =>
                {
                    isOpen      = true;
                    isAnimating = false;
                    boxCollider.enabled = true;
                });
        }

        //Closes this door only.
        public void Close()
        {
            if (isAnimating || !isOpen) return;
            isAnimating = true;
            boxCollider.enabled = false;

            transform.DOLocalRotate(Vector3.zero, animDuration)
                .SetEase(Ease.OutQuad)
                .OnComplete(() =>
                {
                    isOpen      = false;
                    isAnimating = false;
                    boxCollider.enabled = true;
                });
        }

        // IInteractable — let the player manually use the door
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
