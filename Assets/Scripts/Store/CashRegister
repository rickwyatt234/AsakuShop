using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace AsakuShop.Store
{
    // Cash register UI panel. All amounts are whole yen 
    public class CashRegister : MonoBehaviour
    {
        [SerializeField] private Button undoButton;
        [SerializeField] private Button clearButton;
        [SerializeField] private Button confirmButton;
        [SerializeField] private float  slideDuration = 0.3f;

        public event System.Action<int> OnDraw;
        public event System.Action       OnUndo;
        public event System.Action       OnClear;
        public event System.Action       OnConfirm;

        private bool          allowDrawing;
        private RectTransform rect;
        private float         originalPosY;

        private void Awake()
        {
            rect         = GetComponent<RectTransform>();
            originalPosY = rect.anchoredPosition.y;

            undoButton?.onClick.AddListener(()   => { if (allowDrawing) OnUndo?.Invoke(); });
            clearButton?.onClick.AddListener(()  => { if (allowDrawing) OnClear?.Invoke(); });
            confirmButton?.onClick.AddListener(() => { if (allowDrawing) OnConfirm?.Invoke(); });

            gameObject.SetActive(false);
        }

        //Adds a denomination amount to the given-change stack.
        public void Draw(int amount)
        {
            if (!allowDrawing) return;
            OnDraw?.Invoke(amount);
        }

        //Slides the panel up and enables input.
        public void Open()
        {
            gameObject.SetActive(true);
            rect.DOAnchorPosY(0f, slideDuration)
                .SetEase(Ease.OutQuad)
                .OnComplete(() => allowDrawing = true);
        }

        //Disables input and slides the panel down.
        public void Close()
        {
            allowDrawing = false;
            rect.DOAnchorPosY(originalPosY, slideDuration)
                .SetEase(Ease.InQuad)
                .OnComplete(() => gameObject.SetActive(false));
        }
    }
}
