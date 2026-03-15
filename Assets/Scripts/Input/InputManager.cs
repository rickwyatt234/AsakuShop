using UnityEngine;

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
    }
}