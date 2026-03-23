using UnityEngine;
using TMPro;
using AsakuShop.Core;
using AsakuShop.Input;
using AsakuShop.Storage;
using AsakuShop.Player;
using AsakuShop.Items;

namespace AsakuShop.UI
{
    public class ContextHintDisplay : Monobehaviour, IContextHintDisplay
    {
        private static ContextHintDisplay _instance;
        public static ContextHintDisplay Instance
        {
            get
            {
                if (_instance == null)
                {
                    GameObject go = new GameObject("[ContextHintDisplayController]");
                    _instance = go.AddComponent<ContextHintDisplay>();
                }
                return _instance;
            }
        }
        
        [SerializeField] private TextMeshProUGUI contextText;
        [SerializeField] private CanvasGroup contextCanvasGroup;
        [HideInInspector] public IInputManager input;
        [HideInInspector] public PlayerHands player;

        private void Awake()
        {
            _instance = this;
            UIService.ContextHint = this;
            DontDestroyOnLoad(gameObject);

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
            if (player.heldItem != null && player.heldItemPickup != null)
                return player.heldItemPickup;
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
                case ItemPickup itemPickup:
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
                case ItemPickup itemPickup:
                    // Check if player is looking at a shelf within stocking range
                    Ray ray = new Ray(player.playerCamera.position, player.playerCamera.forward);
                    if (Physics.Raycast(ray, out RaycastHit hit, 3f))
                    {
                        ShelfComponent shelf = hit.collider.GetComponent<ShelfComponent>();
                        if (shelf != null)
                            return $"[{interactKey}]: Stock\n" +
                                   $"[{examineKey}]: Examine\n" +
                                   $"[{rotateKey}]: Rotate Vertically\n" +
                                   $"[{rotateModifierKey}] + [{rotateKey}]: Rotate Horizontally";
                        StorageContainer container = hit.collider.GetComponent<StorageContainer>();
                        if (container != null)
                            return $"[{interactKey}]: Drop\n" +
                                   $"[{examineKey}]: Store\n" +
                                   $"[{rotateKey}]: Rotate Vertically\n" +
                                   $"[{rotateModifierKey}] + [{rotateKey}]: Rotate Horizontally";
                    }
                    // Default item context
                    return $"[{interactKey}]: Drop\n" +
                           $"[{examineKey}]: Examine\n" +
                           $"[{rotateKey}]: Rotate Vertically\n" +
                           $"[{rotateModifierKey}] + [{rotateKey}]: Rotate Horizontally";
                           
                case StorageContainer storageContainer:
                    return $"[{interactKey}]: Drop\n" +
                           $"[{rotateKey}]: Rotate Vertically\n" +
                           $"[{rotateModifierKey}] + [{rotateKey}]: Rotate Horizontally";
                           
                case ShelfComponent shelfComponent:
                    if (player.IsLookingAtSuitableShelfMountingPosition)
                        return $"[{interactKey}]: Mount";
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
