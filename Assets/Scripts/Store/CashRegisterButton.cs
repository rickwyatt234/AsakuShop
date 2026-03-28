using AsakuShop.Core;
using UnityEngine;

namespace AsakuShop.Store
{
    // Attach to an invisible child GameObject (with a BoxCollider) placed over each physical button
    // on the cash register mesh. Because the buttons are baked into the mesh and are not separate
    // GameObjects, this component fakes interaction by forwarding player clicks to the CashRegister.
    //
    // Setup for each button:
    //   1. Create an empty child GameObject on the cash register model, positioned over the mesh button.
    //   2. Add a BoxCollider sized to match the button face.
    //   3. Add this component and assign the CashRegister reference.
    //   4. Set buttonInput to the appropriate value:
    //        "0"–"9"        → digit keys on the numerical keypad
    //        "back"         → backspace / undo last digit
    //        "clear"        → clears the entire keyed entry
    //        "confirm"      → Cash/Tend button (confirms tendered amount, opens drawer)
    //        "finalconfirm" → Final Confirm button (completes the transaction)
    //
    // Ensure your player interaction system calls IInteractable.OnInteract() on click/press.
    public class CashRegisterButton : MonoBehaviour, IInteractable
    {
        [SerializeField, Tooltip("The CashRegister this button belongs to.")]
        private CashRegister register;

        [SerializeField, Tooltip(
            "Input to send:\n" +
            "  '0'–'9'        — digit keys\n" +
            "  'back'         — backspace / undo last digit\n" +
            "  'clear'        — clears the entire keyed entry\n" +
            "  'confirm'      — Cash/Tend button\n" +
            "  'finalconfirm' — Final Confirm button")]
        private string buttonInput;

        public void OnInteract()
        {
            if (register == null) return;

            switch (buttonInput)
            {
                case "confirm":
                    register.TryConfirm();
                    break;
                case "finalconfirm":
                    register.TryFinalConfirm();
                    break;
                case "clear":
                    register.ClearEntry();
                    break;
                default:
                    if (buttonInput.Length == 1 && char.IsDigit(buttonInput[0]) || buttonInput == "back")
                        register.Append(buttonInput);
                    else
                        Debug.LogWarning($"[CashRegisterButton] Unrecognised buttonInput '{buttonInput}' on {name}.");
                    break;
            }
        }

        public void OnExamine() { }
    }
}
