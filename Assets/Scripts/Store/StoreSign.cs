using TMPro;
using UnityEngine;
using AsakuShop.Core;

namespace AsakuShop.Store
{
    // Interactable store sign that toggles the store open/closed state via StoreManager.
    // Implement IInteractable so the player's raycast system can trigger it.
    public class StoreSign : MonoBehaviour, IInteractable
    {
        [SerializeField] private StoreManager storeManager;

        [SerializeField, Tooltip("Optional world-space TMP label on the sign itself.")]
        private TextMeshPro signText;

        private void Awake()
        {
            if (storeManager == null)
                storeManager = StoreManager.Instance; // Try to auto-assign from singleton if not set
        }

        private void Start()
        {
            RefreshSignText();
        }

        //Called when the player interacts with the sign. Toggles store open state.
        public void OnInteract()
        {
            var mgr = storeManager != null ? storeManager : StoreManager.Instance;
            if (mgr == null)
            {
                Debug.LogWarning("[StoreSign] No StoreManager found.", this);
                return;
            }

            mgr.ToggleOpen();
            //Debug.Log($"[StoreSign] Store is now {(mgr.IsOpen ? "OPEN" : "CLOSED")}");
            RefreshSignText();
        }

        //Called when the player looks at the sign — no action needed here.
        public void OnExamine()
        {
            // Reserved for future tooltip/hint display.
        }

        private void RefreshSignText()
        {
            var mgr = storeManager != null ? storeManager : StoreManager.Instance;
            if (signText == null || mgr == null) return;
            signText.text = mgr.IsOpen ? "OPEN" : "CLOSED";
        }
    }
}
