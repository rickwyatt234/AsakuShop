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

        public string GetContext(IInteractable interactable)
        {
            string interactKey = input.GetInteractKeyName();
            string examineKey = input.GetExamineKeyName();
            string rotateKey = input.GetRotatePreviewKeyName();
            string rotateModifierKey = input.GetRotatePreviewModifierKeyName();
            // switch statement based on interactable type or tags to return appropriate context hints
            switch (interactable)
            {
                case ItemInstance itemInstance:
                    return $"[{interactKey}]: pick up {itemInstance.Definition.DisplayName}\n" +
                           $"[{examineKey}]: examine";
                case StorageContainer storageContainer:
                    return $"[{interactKey}]: open {storageContainer.containerName}\n" +
                           $"[{examineKey}]: examine";
                case ShelfComponent shelfComponent:
                    return $"[{interactKey}]: interact with shelf\n" +
                           $"[{examineKey}]: examine\n" +
                           $"Hold [{rotateModifierKey}] + Move mouse to rotate preview";
                default:
                    return $"[{interactKey}]: interact\n" +
                           $"[{examineKey}]: examine";
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