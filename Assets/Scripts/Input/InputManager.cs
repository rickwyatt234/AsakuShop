using UnityEngine;
using UnityEngine.InputSystem;
using AsakuShop.Core;

namespace AsakuShop.Input
{
    public class InputManager : MonoBehaviour, IInputManager
    {
        private InputMappings inputActions;

        public Vector2 moveInput => inputActions.Player.Move.ReadValue<Vector2>();
        public Vector2 lookInput => inputActions.Player.Look.ReadValue<Vector2>();
        public bool jump => inputActions.Player.Jump.IsPressed();
        public bool run => inputActions.Player.Sprint.IsPressed();
        public bool crouch => inputActions.Player.Crouch.IsPressed();
        public bool interact => inputActions.Player.Interact.IsPressed();
        public bool examine => inputActions.Player.ItemExamine.IsPressed();
        public Vector2 rotateInput => inputActions.Player.RotateItem.ReadValue<Vector2>();

        public float rotatePreviewVertical => inputActions.Player.RotatePreviewVertical.ReadValue<float>();
        public float rotatePreviewHorizontal => inputActions.Player.RotatePreviewHorizontal.ReadValue<float>();
        public bool rotatePreviewModifier => inputActions.Player.RotatePreviewModifier.IsPressed(); // You need to add Shift input action

        public bool IsGamepadActive => Gamepad.current != null;

        public string GetInteractKeyName() => GetActionKeyName(inputActions.Player.Interact);
        public string GetExamineKeyName() => GetActionKeyName(inputActions.Player.ItemExamine);
        public string GetRotatePreviewKeyName() => GetRotateKeyName();
        public string GetRotatePreviewModifierKeyName() => GetActionKeyName(inputActions.Player.RotatePreviewModifier);

        private string GetActionKeyName(InputAction action)
        {
            if (action == null || action.bindings.Count == 0)
                return "?";

            bool preferGamepad = IsGamepadActive;

            // First pass: look for preferred device type
            foreach (var binding in action.bindings)
            {
                if (binding.isComposite)
                    continue;

                string path = binding.effectivePath;
                bool isGamepadBinding = path.Contains("Gamepad");

                // If we prefer gamepad and this is gamepad, or we prefer keyboard and this isn't gamepad
                if (isGamepadBinding == preferGamepad)
                {
                    return ExtractKeyName(path);
                }
            }

            // Fallback: return any binding we can find
            foreach (var binding in action.bindings)
            {
                if (!binding.isComposite)
                {
                    return ExtractKeyName(binding.effectivePath);
                }
            }

            return "?";
        }

        private string ExtractKeyName(string bindingPath)
        {
            if (bindingPath.Contains("Gamepad"))
            {
                if (bindingPath.Contains("buttonSouth")) return "A";
                if (bindingPath.Contains("buttonWest")) return "X";
                if (bindingPath.Contains("buttonNorth")) return "Y";
                if (bindingPath.Contains("buttonEast")) return "B";
                if (bindingPath.Contains("rightShoulder")) return "RB";
                if (bindingPath.Contains("leftShoulder")) return "LB";
                if (bindingPath.Contains("rightTrigger")) return "RT";
                if (bindingPath.Contains("leftTrigger")) return "LT";
            }
            else if (bindingPath.Contains("Keyboard"))
            {
                // Extract key name: "<Keyboard>/f" -> "f" -> "F"
                string[] parts = bindingPath.Split('/');
                if (parts.Length > 1)
                    return parts[1].ToUpper();
            }
            else if (bindingPath.Contains("Mouse"))
            {
                if (bindingPath.Contains("leftButton")) return "LMB";
                if (bindingPath.Contains("rightButton")) return "RMB";
                if (bindingPath.Contains("middleButton")) return "MMB";
                if (bindingPath.Contains("scroll")) return "Scroll";
            }

            return "?";
        }
        
        private string GetRotateKeyName()
        {
            // For keyboard/mouse, show Scroll
            if (!IsGamepadActive && Mouse.current != null)
                return "Scroll";
            
            // For gamepad, show the actual binding
            return GetActionKeyName(inputActions.Player.RotateItem);
        }

        private void Awake()
        {
            inputActions = new InputMappings();
        }

        private void OnEnable()
        {
            inputActions.Enable();
        }

        private void OnDisable()
        {
            inputActions.Disable();
        }
        
        public void DisableMovementInput()
        {
            inputActions.Player.Move.Disable();
        }
        public void EnableMovementInput()
        {
            inputActions.Player.Move.Enable();
        }
    }
}