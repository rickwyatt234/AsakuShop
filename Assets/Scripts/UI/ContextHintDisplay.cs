using UnityEngine;
using TMPro;
using AsakuShop.Core;
using AsakuShop.Input;
using AsakuShop.Items;

namespace AsakuShop.UI
{
    public class ContextHintDisplay : MonoBehaviour
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
        [HideInInspector] public IPickupTarget player;

        private void Awake()
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);

            if (contextText == null)
                contextText = GetComponentInChildren<TextMeshProUGUI>();

            if (contextCanvasGroup == null)
                contextCanvasGroup = GetComponent<CanvasGroup>();

            input = FindFirstObjectByType<InputManager>();

            HideContext();
        }

        private void Start()
        {
            player = PlayerService.PickupTarget;
        }

        private void Update()
        {
            if (player == null) return;
    
            // When holding, always show the held item's context
            if (player.IsHoldingInteractable)
            {
                IInteractable heldInteractable = player.GetHeldInteractable();
                if (heldInteractable != null)
                {
                    SetContext(GetHeldItemContext(heldInteractable));
                }
            }
        }

        public string GetContext(IInteractable interactable)
        {
            if (player != null && player.IsHoldingInteractable)
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
                case IShelfHoldable shelfHoldable:
                    Component shelfComponent = shelfHoldable as Component;
                    Rigidbody shelfRb = shelfComponent?.GetComponent<Rigidbody>();
                    bool wallMounted = shelfRb != null && shelfRb.isKinematic && IsNearWall(shelfComponent);
                    return wallMounted ? $"[{examineKey}]: Pick up" : $"[{interactKey}]: Pick up";
                case IHoldable holdable:
                    return $"[{interactKey}]: Pick up\n" +
                           $"[{examineKey}]: Open";
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
                    Ray ray = new Ray(Camera.main.transform.position, Camera.main.transform.forward);
                    if (Physics.Raycast(ray, out RaycastHit hit, 3f))
                    {
                        IShelfHoldable shelfHoldable = hit.collider.GetComponent<IShelfHoldable>();
                        if (shelfHoldable != null)
                            return $"[{interactKey}]: Stock\n" +
                                   $"[{examineKey}]: Examine\n" +
                                   $"[{rotateKey}]: Rotate Vertically\n" +
                                   $"[{rotateModifierKey}] + [{rotateKey}]: Rotate Horizontally";
                        IHoldable container = hit.collider.GetComponent<IHoldable>();
                        if (container != null && !(container is IShelfHoldable))
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
                           
                case IShelfHoldable shelfHoldable:
                    if (player != null && player.IsLookingAtSuitableShelfMountingPosition)
                        return $"[{interactKey}]: Mount";
                    else
                        return $"[{interactKey}]: Drop";

                case IHoldable holdable:
                    return $"[{interactKey}]: Drop\n" +
                           $"[{rotateKey}]: Rotate Vertically\n" +
                           $"[{rotateModifierKey}] + [{rotateKey}]: Rotate Horizontally";
                           
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

        private bool IsNearWall(Component c)
        {
            if (c == null) return false;

            int wallLayer = LayerMask.NameToLayer("Wall");
            Vector3[] directions = {
                -c.transform.forward, c.transform.forward,
                c.transform.right,    -c.transform.right,
                c.transform.up,       -c.transform.up
            };

            foreach (Vector3 dir in directions)
            {
                if (Physics.Raycast(new Ray(c.transform.position, dir), out RaycastHit hit, 2f) &&
                    hit.collider.gameObject.layer == wallLayer)
                    return true;
            }
            return false;
        }
    }
}
