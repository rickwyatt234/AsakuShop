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
    //   priceInputField   – TMP_InputField (ContentType = Integer Number) for the new price
    //   decreaseButton    – reduces the displayed price by 10%
    //   increaseButton    – increases the displayed price by 10%
    //   confirmButton     – applies the entered price and closes the panel
    //   cancelButton      – closes the panel without making any change
    public class PriceEditorUI : MonoBehaviour
    {
        public static PriceEditorUI Instance { get; private set; }

        [Header("Panel")]
        [SerializeField] private GameObject priceEditorPanel;

        [Header("Labels")]
        [SerializeField] private TextMeshProUGUI itemNameText;
        [SerializeField] private TextMeshProUGUI marketPriceText;

        [Header("Input")]
        [SerializeField] private TMP_InputField priceInputField;

        [Header("Buttons")]
        [SerializeField] private Button decreaseButton;
        [SerializeField] private Button increaseButton;
        [SerializeField] private Button confirmButton;
        [SerializeField] private Button cancelButton;

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

            if (decreaseButton != null) decreaseButton.onClick.AddListener(OnDecreasePressed);
            if (increaseButton != null) increaseButton.onClick.AddListener(OnIncreasePressed);
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
                marketPriceText.text = $"Market Price: ¥{item.Definition.EffectiveMarketPrice}";

            if (priceInputField != null)
                priceInputField.text = Mathf.RoundToInt(item.CurrentPrice).ToString();

            if (priceEditorPanel != null)
                priceEditorPanel.SetActive(true);

            Cursor.visible   = true;
            Cursor.lockState = CursorLockMode.None;
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
        }
#endregion

#region Button Handlers
        private void OnIncreasePressed()
        {
            if (priceInputField == null) return;
            if (!float.TryParse(priceInputField.text, out float current)) return;

            int increased = Mathf.Max(1, Mathf.RoundToInt(current * 1.1f));
            priceInputField.text = increased.ToString();
        }

        private void OnDecreasePressed()
        {
            if (priceInputField == null) return;
            if (!float.TryParse(priceInputField.text, out float current)) return;

            int decreased = Mathf.Max(1, Mathf.RoundToInt(current * 0.9f));
            priceInputField.text = decreased.ToString();
        }

        private void OnConfirmPressed()
        {
            if (_currentItem == null) return;
            if (priceInputField == null) return;
            if (!float.TryParse(priceInputField.text, out float newPrice)) return;

            newPrice = Mathf.Max(0f, newPrice);
            ItemPriceRegistry.SetPrice(_currentItem.Definition.ItemId, newPrice);

            Close();
        }

        private void OnCancelPressed() => Close();
#endregion
    }
}
