using DG.Tweening;
using UnityEngine;
using AsakuShop.Core;

namespace AsakuShop.Store
{
    // Single interior door. Uses DOTween to animate open/close rotation.
    // Implements IInteractable so the player can manually open/close it.
    [RequireComponent(typeof(BoxCollider))]
    public class Door : MonoBehaviour, IInteractable
    {
        [SerializeField, Tooltip("Local euler angles when fully open.")]
        private Vector3 openAngle = new Vector3(0f, -90f, 0f);

        [SerializeField] private float animDuration = 0.4f;

        private BoxCollider boxCollider;
        private bool isOpen;
        private bool isAnimating;

        private void Awake()
        {
            boxCollider = GetComponent<BoxCollider>();
        }

        //Opens the door if it is currently closed.
        public void OpenIfClosed()
        {
            if (!isOpen) Open();
        }

        //Opens the door with a DOTween rotation animation.
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

        //Closes the door with a DOTween rotation animation.
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

        // IInteractable
        public void OnInteract()
        {
            if (isOpen) Close();
            else Open();
        }

        public void OnExamine() { }
    }
}
