using UnityEngine;
using UnityEngine.UI;
using TMPro;
using AsakuShop.Items;
using AsakuShop.Core;

namespace AsakuShop.UI
{
    // Overlay UI that lets the player set the retail price for a shelved item type.
    //
    // Opened automatically whenever the player presses Examine on an item whose
    // ItemInstance.IsOnAShelf is true. Setting the price here writes to
    // ItemPriceRegistry, which propagates the change to all live instances of
    // the same item type and to all future instances created afterwards.
    //
    // Inspector wiring (assign in the Unity Editor):
    //   priceEditorPanel  – root panel GameObject (set inactive by default)
    //   itemNameText      – shows the item's DisplayName
    //   marketPriceText   – shows EffectiveMarketPrice for player reference
    //   priceInputText    – TMP_InputField (ContentType = Integer Number) for the new price
    //   itemGradeText     – shows the item's grade (e.g. "A", "B", "C") for player reference
    //   decreaseButton    – reduces the displayed price by 10%
    //   increaseButton    – increases the displayed price by 10%
    //   confirmButton     – applies the entered price and closes the panel
    //   cancelButton      – closes the panel without making any change
    public class PriceEditorUI : MonoBehaviour
    {
        public static PriceEditorUI Instance { get; private set; }

        [Header("Panel")]
        [SerializeField] private GameObject priceEditorPanel;
        [SerializeField] private CanvasGroup crosshairCanvasGroup;

        [Header("Labels")]
        [SerializeField] private TextMeshProUGUI itemNameText;
        [SerializeField] private TextMeshProUGUI marketPriceText;
        [SerializeField] private TextMeshProUGUI priceInputText;
        [SerializeField] private TextMeshProUGUI itemGradeText;


        [Header("Buttons")]
        [SerializeField] private Button decreaseTenPercentButton;
        [SerializeField] private Button increaseTenPercentButton;
        [SerializeField] private Button confirmButton;
        [SerializeField] private Button cancelButton;
        [SerializeField] private Button Increase1YenButton;
        [SerializeField] private Button Decrease1YenButton;
        [SerializeField] private Button Increase5YenButton;
        [SerializeField] private Button Decrease5YenButton;
        [SerializeField] private Button Increase10YenButton;
        [SerializeField] private Button Decrease10YenButton;
        [SerializeField] private Button Increase50YenButton;
        [SerializeField] private Button Decrease50YenButton;
        [SerializeField] private Button Increase100YenButton;
        [SerializeField] private Button Decrease100YenButton;
        [SerializeField] private Button Increase500YenButton;
        [SerializeField] private Button Decrease500YenButton;
        [SerializeField] private Button Increase1000YenButton;
        [SerializeField] private Button Decrease1000YenButton;
        [SerializeField] private Button Increase5000YenButton;
        [SerializeField] private Button Decrease5000YenButton;
        [SerializeField] private Button Increase10000YenButton;
        [SerializeField] private Button Decrease10000YenButton;


        private ItemInstance _currentItem;
        private GamePhase    _phaseBeforeEditing;

#region Unity Lifecycle
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (priceEditorPanel != null)
                priceEditorPanel.SetActive(false);

            CoreEvents.OnShelfItemPriceEditRequested += HandlePriceEditRequested;

            if (decreaseTenPercentButton != null) decreaseTenPercentButton.onClick.AddListener(OnDecrease10PercentPressed);
            if (increaseTenPercentButton != null) increaseTenPercentButton.onClick.AddListener(OnIncrease10PercentPressed);
            if (Increase1YenButton != null) Increase1YenButton.onClick.AddListener(() => AdjustPriceBy(1));
            if (Decrease1YenButton != null) Decrease1YenButton.onClick.AddListener(() => AdjustPriceBy(-1));
            if (Increase5YenButton != null) Increase5YenButton.onClick.AddListener(() => AdjustPriceBy(5));
            if (Decrease5YenButton != null) Decrease5YenButton.onClick.AddListener(() => AdjustPriceBy(-5));
            if (Increase10YenButton != null) Increase10YenButton.onClick.AddListener(() => AdjustPriceBy(10));
            if (Decrease10YenButton != null) Decrease10YenButton.onClick.AddListener(() => AdjustPriceBy(-10));
            if (Increase50YenButton != null) Increase50YenButton.onClick.AddListener(() => AdjustPriceBy(50));
            if (Decrease50YenButton != null) Decrease50YenButton.onClick.AddListener(() => AdjustPriceBy(-50));
            if (Increase100YenButton != null) Increase100YenButton.onClick.AddListener(() => AdjustPriceBy(100));
            if (Decrease100YenButton != null) Decrease100YenButton.onClick.AddListener(() => AdjustPriceBy(-100));
            if (Increase500YenButton != null) Increase500YenButton.onClick.AddListener(() => AdjustPriceBy(500));
            if (Decrease500YenButton != null) Decrease500YenButton.onClick.AddListener(() => AdjustPriceBy(-500));
            if (Increase1000YenButton != null) Increase1000YenButton.onClick.AddListener(() => AdjustPriceBy(1000));
            if (Decrease1000YenButton != null) Decrease1000YenButton.onClick.AddListener(() => AdjustPriceBy(-1000));
            if (Increase5000YenButton != null) Increase5000YenButton.onClick.AddListener(() => AdjustPriceBy(5000));
            if (Decrease5000YenButton != null) Decrease5000YenButton.onClick.AddListener(() => AdjustPriceBy(-5000));
            if (Increase10000YenButton != null) Increase10000YenButton.onClick.AddListener(() => AdjustPriceBy(10000));
            if (Decrease10000YenButton != null) Decrease10000YenButton.onClick.AddListener(() => AdjustPriceBy(-10000));
            if (confirmButton  != null) confirmButton.onClick.AddListener(OnConfirmPressed);
            if (cancelButton   != null) cancelButton.onClick.AddListener(OnCancelPressed);


        }

        private void OnDestroy()
        {
            CoreEvents.OnShelfItemPriceEditRequested -= HandlePriceEditRequested;
        }

#endregion

#region Event Handler
        private void HandlePriceEditRequested(object payload)
        {
            if (payload is ItemInstance item)
                Open(item);
        }
#endregion

#region Public Interface
        // Opens the price editor panel for the given item instance.
        public void Open(ItemInstance item)
        {
            if (item == null) return;

            _currentItem = item;
            _phaseBeforeEditing = GameStateController.Instance.CurrentPhase;
            GameStateController.Instance.RequestTransition(GamePhase.PriceSetting);

            if (itemNameText != null)
                itemNameText.text = item.Definition.DisplayName;

            if (marketPriceText != null)
            {
                float gradeAdjustedPrice = Mathf.Round(item.Definition.EffectiveMarketPrice * item.CurrentGrade.GetPriceMarkup());
                marketPriceText.text = $"Market Price: ¥{gradeAdjustedPrice}";
            }

            if (priceInputText != null)
                priceInputText.text = Mathf.RoundToInt(item.CurrentPrice).ToString();

            if (priceEditorPanel != null)
                priceEditorPanel.SetActive(true);

            if (itemGradeText != null)
            {
                string grade = item.GradeToString();
                // Display the grade with color and markup multiplier, e.g. "A (x1.5)"
                itemGradeText.text = $"<color=#{ColorUtility.ToHtmlStringRGB(item.CurrentGrade.ToColor())}>{grade}</color> {item.CurrentGrade.GetMarkupString()}";
            }

            Cursor.visible   = true;
            Cursor.lockState = CursorLockMode.None;

            if (crosshairCanvasGroup != null)
                crosshairCanvasGroup.alpha = 0f;

            if (ItemHoverDisplay.Instance != null)
                ItemHoverDisplay.Instance.HideLabel();
            
        }

        // Closes the price editor panel without making any change.
        public void Close()
        {
            if (priceEditorPanel != null)
                priceEditorPanel.SetActive(false);

            _currentItem = null;

            Cursor.visible   = false;
            Cursor.lockState = CursorLockMode.Locked;

            // Guard against the edge case where phaseBeforeEditing is already PriceSetting.
            if (_phaseBeforeEditing == GamePhase.PriceSetting)
                _phaseBeforeEditing = GamePhase.Playing;

            GameStateController.Instance.RequestTransition(_phaseBeforeEditing);

            if (crosshairCanvasGroup != null)
                crosshairCanvasGroup.alpha = 1f;
        }
#endregion

#region Button Handlers
        private void OnIncrease10PercentPressed()
        {
            if (priceInputText == null) return;
            if (!float.TryParse(priceInputText.text, out float current)) return;

            int increased = Mathf.Max(1, Mathf.RoundToInt(current * 1.1f));
            priceInputText.text = increased.ToString();
        }

        private void OnDecrease10PercentPressed()
        {
            if (priceInputText == null) return;
            if (!float.TryParse(priceInputText.text, out float current)) return;

            int decreased = Mathf.Max(1, Mathf.RoundToInt(current * 0.9f));
            priceInputText.text = decreased.ToString();
        }

        private void AdjustPriceBy(int amount)
        {
            if (priceInputText == null) return;
            if (!float.TryParse(priceInputText.text, out float current)) return;

            int adjusted = Mathf.Max(0, Mathf.RoundToInt(current) + amount);
            priceInputText.text = adjusted.ToString();
        }

        private void OnConfirmPressed()
        {
            if (_currentItem == null) return;
            if (priceInputText == null) return;
            if (!float.TryParse(priceInputText.text, out float newPrice)) return;

            newPrice = Mathf.Max(0f, newPrice);
            ItemPriceRegistry.SetPrice(_currentItem.Definition.ItemId, newPrice);

            Close();
        }

        private void OnCancelPressed() => Close();
#endregion
    }
}
