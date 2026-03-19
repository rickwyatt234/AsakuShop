using UnityEngine;
using TMPro;
using AsakuShop.Core;
using AsakuShop.Input;
using AsakuShop.Storage;
using AsakuShop.Items;

namespace AsakuShop.UI
{
    public class ContextHintDisplay : MonoBehaviour
    {
        public static ContextHintDisplay Instance { get; private set; }

        [SerializeField] private TextMeshProUGUI contextText;
        [SerializeField] private CanvasGroup contextCanvasGroup;
        [HideInInspector] public IInputManager input;


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

            HideContext();
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
                    if (ShelfIsMounted)
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
                    if (LookingAtSuitableMountingPosition)
                        return $"[{interactKey}]: Mount Shelf\n";
                    else
                        return $"[{interactKey}]: Drop";
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
