using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AsakuShop.Store
{
    // Card/tap-to-pay terminal UI. All input is whole yen 
    public class PaymentTerminal : MonoBehaviour
    {
        [SerializeField] private TMP_Text displayText;
        [SerializeField] private Button   confirmButton;
        [SerializeField] private float    slideDuration = 0.3f;

        public event System.Action<int> OnConfirm;

        private string        enteredAmount = string.Empty;
        private RectTransform rect;
        private float         originalPosY;
        private bool          active;

        private void Awake()
        {
            rect         = GetComponent<RectTransform>();
            originalPosY = rect.anchoredPosition.y;

            confirmButton?.onClick.AddListener(ConfirmAmount);
            gameObject.SetActive(false);
        }

        // Appends a digit or processes "back" to delete the last character.
        // Does not accept "." — yen is integer only.
        public void Append(string input)
        {
            if (!active) return;

            if (input == "back")
            {
                if (enteredAmount.Length > 0)
                    enteredAmount = enteredAmount.Substring(0, enteredAmount.Length - 1);
            }
            else if (char.IsDigit(input[0]))
            {
                enteredAmount += input;
            }
            // Deliberately ignore "." — no decimals in yen

            RefreshDisplay();
        }

        private void ConfirmAmount()
        {
            if (!active) return;
            if (int.TryParse(enteredAmount, out int amount))
                OnConfirm?.Invoke(amount);
            else
                Debug.LogWarning("[PaymentTerminal] Could not parse entered amount.");
        }

        private void RefreshDisplay()
        {
            if (displayText == null) return;
            displayText.text = string.IsNullOrEmpty(enteredAmount)
                ? "¥0"
                : $"¥{int.Parse(enteredAmount):N0}";
        }

        //Slides the terminal up and enables input.
        public void Open()
        {
            enteredAmount = string.Empty;
            RefreshDisplay();
            gameObject.SetActive(true);
            active = false;

            rect.DOAnchorPosY(0f, slideDuration)
                .SetEase(Ease.OutQuad)
                .OnComplete(() => active = true);
        }

        //Disables input and slides the terminal down.
        public void Close()
        {
            active = false;
            rect.DOAnchorPosY(originalPosY, slideDuration)
                .SetEase(Ease.InQuad)
                .OnComplete(() => gameObject.SetActive(false));
        }
    }
}
