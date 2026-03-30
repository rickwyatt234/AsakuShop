using UnityEngine;
using AsakuShop.Items;
using AsakuShop.Input;
using AsakuShop.Core;
using TMPro;

namespace AsakuShop.UI
{
    //item examination mode. Handles zoomed-in item display, rotation, 
    // information panels, and state transitions.
    public class ItemExaminer : MonoBehaviour
    {
        public static ItemExaminer Instance { get; private set; }

        [Header("Examination Display")]
        [SerializeField] private Canvas examinationCanvas;
        [SerializeField] private Canvas playerUICanvas;
        [SerializeField] private Transform itemDisplayParent;
        [SerializeField] private float rotationSpeed = 50f;

        [Header("UI Panels")]
        [SerializeField] private CanvasGroup itemNamePanel;
        [SerializeField] private CanvasGroup itemInfoPanel;
        [SerializeField] private TextMeshProUGUI itemNameText;
        [SerializeField] private TextMeshProUGUI itemInfoText;
        public float examinationItemScale = 1.5f;

        [SerializeField] private float itemDisplayDistance = 1.5f;

        private ItemInstance currentExaminedItem;
        private GameObject examinedItemDisplay;
        private Transform runtimeDisplayAnchor;
        private GamePhase phaseBeforeExamination;
        private bool previousExamineState = false;
        // Accumulated yaw (Y) and pitch (X) driven by mouse input; rebuilt into a fresh
        // Quaternion each frame so rotations are always predictable regardless of prior orientation.
        private float examinationYaw;
        private float examinationPitch;
        [HideInInspector] public IInputManager input;

#region Singleton
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            input = FindFirstObjectByType<InputManager>();
            if (input == null)
            {
                Debug.LogError("[ItemExaminer] Could not find InputManager in scene!");
            }

            // Disable canvas by default
            if (examinationCanvas != null)
                examinationCanvas.gameObject.SetActive(false);

            CoreEvents.OnExamineRequested += HandleExamineRequested;
        }

        private void OnDestroy()
        {
            CoreEvents.OnExamineRequested -= HandleExamineRequested;
            if (runtimeDisplayAnchor != null)
                Destroy(runtimeDisplayAnchor.gameObject);
        }

        private void HandleExamineRequested(object payload)
        {
            if (payload is ItemInstance item)
                StartExamination(item);
        }
#endregion

#region Public Interface
        /// Starts the item examination UI. Called by PlayerHands when examine input is pressed
        public void StartExamination(ItemInstance item)
        {
            if (item == null)
            {
                Debug.LogWarning("[ItemExaminer] Cannot examine null item.");
                return;
            }

            if (input == null)
            {
                input = FindFirstObjectByType<InputManager>();
                if (input == null)
                {
                    Debug.LogError("[ItemExaminer] Could not find InputManager in scene!");
                    return;
                }
            }
            currentExaminedItem = item;
            phaseBeforeExamination = GameStateController.Instance.CurrentPhase;
            examinationYaw   = 0f;
            examinationPitch = 0f; // Reset rotation for each new examination

            // Transition to examination phase (clock will still tick, pause still works)
            GameStateController.Instance.RequestTransition(GamePhase.ItemExamination);
            // Display the examination UI
            ShowExaminationUI();

            previousExamineState = input.examine; // Initialize previous state to current to prevent immediate exit

        }

        /// Ends the item examination and returns to the previous phase.
        public void EndExamination()
        {
            HideExaminationUI();

            // Return to the phase we came from (Playing or Paused)
            if (phaseBeforeExamination == GamePhase.ItemExamination)
                phaseBeforeExamination = GamePhase.Playing;

            GameStateController.Instance.RequestTransition(phaseBeforeExamination);
            currentExaminedItem = null;
        }

        /// Checks if currently in examination mode.
        public bool IsExamining => GameStateController.Instance.CurrentPhase == GamePhase.ItemExamination;
#endregion

#region Private UI Methods
        private void ShowExaminationUI()
        {
            if (examinationCanvas != null)
                examinationCanvas.gameObject.SetActive(true);

            // Disable the player's main UI while examining an item
            if (playerUICanvas != null)
                playerUICanvas.gameObject.SetActive(false);

            // Instantiate the item model
            if (currentExaminedItem?.Definition?.WorldPrefab != null)
            {
                // Always use a world-space anchor parented to the camera so
                // the 3D model renders correctly regardless of canvas render mode.
                Camera mainCam = Camera.main;
                if (mainCam != null)
                {
                    if (runtimeDisplayAnchor == null)
                    {
                        runtimeDisplayAnchor = new GameObject("ExaminationDisplayAnchor").transform;
                        runtimeDisplayAnchor.SetParent(mainCam.transform, false);
                        runtimeDisplayAnchor.localPosition = new Vector3(0f, 0f, itemDisplayDistance);
                        runtimeDisplayAnchor.localRotation = Quaternion.identity;
                    }
                }

                Transform displayParent = runtimeDisplayAnchor != null ? runtimeDisplayAnchor : itemDisplayParent;

                examinedItemDisplay = Instantiate(
                    currentExaminedItem.Definition.WorldPrefab,
                    displayParent
                );
                examinedItemDisplay.transform.localPosition = Vector3.zero;
                examinedItemDisplay.transform.localRotation = Quaternion.identity;
                examinedItemDisplay.transform.localScale = Vector3.one * examinationItemScale;

                // Disable physics on the display model
                Rigidbody rb = examinedItemDisplay.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.isKinematic = true;
                    rb.useGravity = false;
                }

                // Disable colliders so the display model does not interfere with raycasts
                foreach (Collider col in examinedItemDisplay.GetComponentsInChildren<Collider>())
                    col.enabled = false;

                foreach (Renderer renderer in examinedItemDisplay.GetComponentsInChildren<Renderer>())
                {
                    renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                }
            }

            // Update text panels
            if (itemNameText != null)
                itemNameText.text = currentExaminedItem.Definition.DisplayName;

            if (itemInfoText != null)
            {
                string info = $"Grade: {currentExaminedItem.CurrentGrade.ToDisplayString()}\n"
                    + $"Price: ¥{currentExaminedItem.CurrentPrice}\n"
                    + $"Weight: {currentExaminedItem.Definition.WeightKg}kg\n"
                    + $"{currentExaminedItem.Definition.Description}";
                itemInfoText.text = info;
            }

            // Fade in panels
            FadeInPanels();
        }

        private void HideExaminationUI()
        {
            FadeOutPanels();

            if (examinedItemDisplay != null)
                Destroy(examinedItemDisplay);

            // Re-enable the player's main UI after examining an item
            if (playerUICanvas != null)
                playerUICanvas.gameObject.SetActive(true);

            if (examinationCanvas != null)
                examinationCanvas.gameObject.SetActive(false);
        }

        private void FadeInPanels()
        {
            if (itemNamePanel != null)
                itemNamePanel.alpha = 1f;
            if (itemInfoPanel != null)
                itemInfoPanel.alpha = 1f;
        }

        private void FadeOutPanels()
        {
            if (itemNamePanel != null)
                itemNamePanel.alpha = 0f;
            if (itemInfoPanel != null)
                itemInfoPanel.alpha = 0f;
        }
#endregion

#region Input Handling
        private void Update()
        {
            if (!IsExamining)
                return;

            HandleExaminationInput();
        }

        private void HandleExaminationInput()
        {
            // Get rotation input from mouse delta or gamepad right stick
            Vector2 rotationInput = input.rotateInput;
            
            if (rotationInput.sqrMagnitude > 0.01f && examinedItemDisplay != null)
            {
                // Accumulate yaw (horizontal) and pitch (vertical) as plain floats.
                // Building the Quaternion fresh every frame from these two scalars avoids
                // gimbal-lock and orientation-dependent weirdness that comes from chaining
                // sequential Rotate() calls on an already-rotated object.
                examinationYaw   += rotationInput.x * rotationSpeed * Time.deltaTime;
                examinationPitch -= rotationInput.y * rotationSpeed * Time.deltaTime; // inverted so mouse-up tilts the item up

                examinedItemDisplay.transform.localRotation =
                    Quaternion.Euler(examinationPitch, examinationYaw, 0f);
            }

            bool currentExamineState = input.examine;
            bool examinePressed = currentExamineState && !previousExamineState;
            previousExamineState = currentExamineState;
            
            // Exit examination on ItemExamine button press
            if (examinePressed)
            {
                EndExamination();
            }
        }
#endregion
    }
}
