using UnityEngine;

namespace AsakuShop.Input
{
    public interface IInputManager
    {
        Vector2 moveInput { get; }
        Vector2 lookInput { get; }
        bool jump { get; }
        bool run { get; }
        bool crouch { get; }
        bool interact { get; }
        bool itemExamine { get; }
        Vector2 rotateInput { get; }
        float rotatePreviewVertical { get; }
        float rotatePreviewHorizontal { get; }
        bool rotatePreviewModifier { get; }

        bool IsGamepadActive { get; }

        string GetInteractKeyName();
        string GetItemExamineKeyName();
        string GetRotatePreviewKeyName();
        string GetRotatePreviewModifierKeyName();
        void DisableMovementInput();
        void EnableMovementInput();
    }
}