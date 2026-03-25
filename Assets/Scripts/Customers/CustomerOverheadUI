using System.Collections;
using TMPro;
using UnityEngine;

namespace AsakuShop.Customers
{
    // World-space, camera-facing overhead label shown above a customer.
    // Attach a child Canvas (World Space) with a TextMeshProUGUI to the head bone.
    public class CustomerOverheadUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI dialogText;

        private Camera mainCam;
        private Coroutine clearRoutine;

        private void Awake()
        {
            if (dialogText != null) dialogText.text = string.Empty;
        }

        private void LateUpdate()
        {
            if (mainCam == null) mainCam = Camera.main;
            if (mainCam == null) return;
            transform.forward = mainCam.transform.forward;
        }

        //Displays a speech-bubble style message that auto-clears after <paramref name="duration"/> seconds.
        public void ShowDialog(string text, float duration = 4f)
        {
            if (dialogText == null) return;
            if (clearRoutine != null) StopCoroutine(clearRoutine);
            dialogText.text = text;
            clearRoutine    = StartCoroutine(ClearAfter(duration));
        }

        //Displays a persistent status label (does not auto-clear).
        public void ShowStatus(string text)
        {
            if (dialogText == null) return;
            if (clearRoutine != null) { StopCoroutine(clearRoutine); clearRoutine = null; }
            dialogText.text = text;
        }

        //Immediately clears the displayed text.
        public void Hide()
        {
            if (dialogText == null) return;
            if (clearRoutine != null) { StopCoroutine(clearRoutine); clearRoutine = null; }
            dialogText.text = string.Empty;
        }

        private IEnumerator ClearAfter(float delay)
        {
            yield return new WaitForSeconds(delay);
            if (dialogText != null) dialogText.text = string.Empty;
            clearRoutine = null;
        }
    }
}
