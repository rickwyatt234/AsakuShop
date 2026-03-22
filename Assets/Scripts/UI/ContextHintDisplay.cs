using UnityEngine;
using TMPro;
using AsakuShop.Core;
using AsakuShop.Input;
using AsakuShop.Storage;
using AsakuShop.Player;
using AsakuShop.Items;

namespace AsakuShop.UI
{
    public class ContextHintDisplay : MonoBehaviour
    {
        public static ContextHintDisplay Instance { get; private set; }

        [SerializeField] private TextMeshProUGUI contextText;
        [SerializeField] private CanvasGroup contextCanvasGroup;
        [HideInInspector] public IInputManager input;
        [HideInInspector] public PlayerHands player;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (contextText == null)
                contextText = GetComponentInChildren<TextMeshProUGUI>();

            if (contextCanvasGroup == null)
                contextCanvasGroup = GetComponent<CanvasGroup>();

            input = FindFirstObjectByType<InputManager>();
            player = FindFirstObjectByType<PlayerHands>();

            HideContext();
        }

        private void Update()
        {
            if (player == null) return;
    
            // When holding, always show the held item's context
            if (player.IsHoldingInteractable)
            {
                IInteractable heldInteractable = GetHeldInteractable();
                if (heldInteractable != null)
                {
                    SetContext(GetHeldItemContext(heldInteractable));
                }
            }
        }

        private IInteractable GetHeldInteractable()
        {
            if (player.heldItem != null)
            {
                // Find the ItemPickup component on the held visual
                if (player.heldItemVisual != null)
                {
                    ItemPickup pickup = player.heldItemVisual.GetComponent<ItemPickup>();
                    if (pickup != null)
                        return pickup;
                }
            }
            if (player.heldContainer != null)
                return player.heldContainer;
            if (player.heldShelf != null)
                return player.heldShelf;
            return null;
        }

        public string GetContext(IInteractable interactable)
        {
            if (player.IsHoldingInteractable)
            {
                return GetHeldItemContext(interactable);
            }
            else
            {
                return GetHoverContext(interactable);
            }
        }

        public string GetHoverContext(IInteractable interactable)
        {
            string interactKey = input.GetInteractKeyName();
            string examineKey = input.GetExamineKeyName();
            // switch statement based on interactable type or tags to return appropriate context hints
            switch (interactable)
            {
                case ItemInstance itemInstance:
                    return $"[{interactKey}]: Pick up";
                case StorageContainer storageContainer:
                    return $"[{interactKey}]: Pick up\n" +
                           $"[{examineKey}]: Open";
                case ShelfComponent shelfComponent:
                    if (shelfComponent.IsShelfWallMounted(shelfComponent))
                        return $"[{examineKey}]: Pick up";
                    else
                        return $"[{interactKey}]: Pick up";
                default:
                    return "";
            }
        }

        public string GetHeldItemContext(IInteractable interactable)
        {
            string interactKey = input.GetInteractKeyName();
            string examineKey = input.GetExamineKeyName();
            string rotateKey = input.GetRotatePreviewKeyName();
            string rotateModifierKey = input.GetRotatePreviewModifierKeyName();

            switch (interactable)
            {
                case ItemInstance itemInstance:
                    return $"[{interactKey}]: Drop\n" +
                           $"[{examineKey}]: Examine\n" +
                           $"[{rotateKey}]: Rotate Vertically\n" +
                           $"[{rotateModifierKey}] + [{rotateKey}]: Rotate Horizontally";
                case StorageContainer storageContainer:
                    return $"[{interactKey}]: Drop\n" +
                           $"[{rotateKey}]: Rotate Vertically\n" +
                           $"[{rotateModifierKey}] + [{rotateKey}]: Rotate Horizontally";
                case ShelfComponent shelfComponent:
                    if (shelfComponent.IsShelfWallMounted(shelfComponent))
                        return $"[{examineKey}]: Pick up";
                    else
                        return $"[{interactKey}]: Drop";
                default:
                    return "";
            }
        }

        public void SetContext(string hint)
        {
            if (string.IsNullOrEmpty(hint))
            {
                HideContext();
                return;
            }

            if (contextText != null)
                contextText.text = hint;

            if (contextCanvasGroup != null)
                contextCanvasGroup.alpha = 1f;
        }

        public void HideContext()
        {
            if (contextText != null)
                contextText.text = "";

            if (contextCanvasGroup != null)
                contextCanvasGroup.alpha = 0f;
        }
    }
}