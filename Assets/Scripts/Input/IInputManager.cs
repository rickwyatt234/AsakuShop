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
    }
}