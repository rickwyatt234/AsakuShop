using AsakuShop.Core;
using UnityEngine;

namespace AsakuShop.Store
{
    // Attach to an invisible child GameObject (with a BoxCollider) placed over each physical button
    // on the card reader mesh. Because the buttons are baked into the mesh and are not separate
    // GameObjects, this component fakes interaction by forwarding player clicks to the PaymentTerminal.
    //
    // Setup for each button:
    //   1. Create an empty child GameObject on the card reader model, positioned over the mesh button.
    //   2. Add a BoxCollider sized to match the button face.
    //   3. Add this component and assign the PaymentTerminal reference.
    //   4. Set buttonInput to the appropriate value:
    //        "0"–"9"   → digit keys on the numerical keypad
    //        "back"    → backspace / red cancel button
    //        "confirm" → green confirm button (triggers entry confirm or final confirm automatically)
    //
    // Ensure your player interaction system calls IInteractable.OnInteract() on click/press.
    public class CardReaderButton : MonoBehaviour, IInteractable
    {
        [SerializeField, Tooltip("The PaymentTerminal this button belongs to.")]
        private PaymentTerminal terminal;

        [SerializeField, Tooltip(
            "Input to send:\n" +
            "  '0'–'9'   — digit keys\n" +
            "  'back'    — backspace / red cancel button\n" +
            "  'confirm' — green confirm button")]
        private string buttonInput;

        public void OnInteract()
        {
            if (terminal == null) return;

            if (buttonInput == "confirm")
                if (terminal.CurrentState == PaymentTerminal.Phase.Entry)
                    terminal.HandleEntryConfirm();
                else if (terminal.CurrentState == PaymentTerminal.Phase.Success)
                    terminal.HandleFinalConfirm();
            else
                terminal.Append(buttonInput);
        }

        public void OnExamine() { }
    }
}
