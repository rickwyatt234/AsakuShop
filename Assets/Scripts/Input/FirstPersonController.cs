using AsakuShop.Core;
using UnityEngine;
using UnityEngine.UI;

namespace AsakuShop.Input
{
public class FirstPersonController : MonoBehaviour
{
#region MovementSettings
        [Header("Movement Settings")]
        public float walkSpeed = 3f;
        public float runSpeed = 5f;
        public float crouchSpeed = 1.5f;
        public float jumpSpeed = 4f;
        public float gravity = 9.81f;
        public float mouseSensitivity = 2f;
        public float strafeTiltAmount = 2f;
#endregion
    
#region References
        [Header("References")]
        public Transform playerCamera;
        public Transform cameraHolder;
        public Transform groundCheck;
        public LayerMask groundLayer;

            [HideInInspector] public CharacterController characterController;
            [HideInInspector] public IInputManager input;
            [HideInInspector] public Vector3 moveDirection;
            [HideInInspector] public bool isGrounded;
        
        private PlayerBaseState currentState;
        private PlayerStateFactory stateFactory;
        private float xRotation = 0f;
        private float currentTilt;
        private float tiltVelocity;

        public PlayerBaseState CurrentState { get => currentState; set => currentState = value; }
#endregion
    
#region Visual Settings
        [Header("Visual Settings")]
        public float normalFOV = 60f;
        public float runFOV = 70f;
        public float fovChangeSpeed = 8f;
        public float bobAmount = 0.001f;
        public float bobSpeed = 10f;
        public float recoilReturnSpeed = 5f;

            [HideInInspector] public Camera cam;
            [HideInInspector] public float targetFov;
            [HideInInspector] public float currentBobIntensity;
            [HideInInspector] public float currentBobSpeed;
            [HideInInspector] public float targetTilt;
        private float bobTimer;
        private float fovVelocity;
        private float originalCamY;

        [SerializeField] private Image crosshair;
#endregion
    
#region CrouchSettings
        [Header("Height Settings")]
        public float standingCameraHeight = 1.75f;
        public float crouchingCameraHeight = 1f;
        public float crouchingCharacterControllerHeight = 1f;
        private float landingMomentum;

            [HideInInspector] public float standingCharacterControllerHeight = 1.8f;
            [HideInInspector] public Vector3 standingCharacterControllerCenter = new Vector3(0, 0.9f, 0);
            [HideInInspector] public float targetCameraY;
#endregion

#region Swimming Settings
        [Header("Swimming Settings")]
        public float swimSpeed = 4f;
        public float swimSprintSpeed = 6f;
        public float waterDrag = 2f;
        public LayerMask waterMask;
        [HideInInspector] public bool isInWater;
#endregion

#region Preference Bools
        [Header("Visual Preferences")]
        public bool useFovKick = true;
        public bool useHeadBob = true;
        public bool useCameraTilt = true;
        public bool useClimbTilt = true;
#endregion

#region Debugs
        [Header("Debug")]
        public bool currentStateDebugLog = true;

            void OnGUI()
        {
            // Display the current state
            if (currentState != null && Application.isEditor && currentStateDebugLog)
                GUILayout.Label("Current State: " + currentState.GetType().Name);
        }
#endregion

#region Unity Methods
        private void Awake()
        {
            cam = playerCamera.GetComponent<Camera>();
                targetFov = normalFOV;
                targetCameraY = standingCameraHeight;
                originalCamY = standingCameraHeight;

            characterController = GetComponent<CharacterController>();
            
            standingCharacterControllerHeight = characterController.height;
            standingCharacterControllerCenter = characterController.center;

            input = GetComponent<IInputManager>();

            stateFactory = new PlayerStateFactory(this);
        }

        private void Start()
        {
            currentState = stateFactory.Grounded();
            currentState.EnterState();

        }

        private void Update()
        {
            if (GameStateController.Instance.CurrentPhase != GamePhase.Playing)
                return;
            
            isGrounded = Physics.CheckSphere(groundCheck.position, 0.2f, groundLayer, QueryTriggerInteraction.Ignore);

            currentState.UpdateState();
            HandleRotation();
            HandleFov();
        }
#endregion

#region Handlers
        public void HandleRotation()
        {
            float mouseX = input.lookInput.x * mouseSensitivity;
            float mouseY = input.lookInput.y * mouseSensitivity;

            transform.Rotate(Vector3.up * mouseX);

            xRotation -= mouseY;
            xRotation = Mathf.Clamp(xRotation, -90f, 90f);

            float strafeTilt = useCameraTilt ? (-input.moveInput.x * strafeTiltAmount) : 0f;
            float combinedTargetTilt = (useCameraTilt ? targetTilt : 0f) + strafeTilt;

            currentTilt = Mathf.SmoothDamp(currentTilt, combinedTargetTilt, ref tiltVelocity, 0.1f);
            playerCamera.localRotation = Quaternion.Euler(xRotation, 0f, currentTilt);
        }

        public void HandleFov()
        {
            if (!useFovKick)
            {
                targetFov = normalFOV;
            }
            cam.fieldOfView = Mathf.SmoothDamp(cam.fieldOfView, targetFov, ref fovVelocity, 1f / fovChangeSpeed);

            landingMomentum = Mathf.Lerp(landingMomentum, 0, Time.deltaTime * 10f);
            float newY = Mathf.Lerp(cameraHolder.localPosition.y, targetCameraY, Time.deltaTime * 8f);

            if (useHeadBob && characterController.velocity.magnitude > 0.1f && isGrounded)
            {
                bobTimer += Time.deltaTime * currentBobSpeed;
                float bobOffset = Mathf.Sin(bobTimer) * currentBobIntensity;
                cameraHolder.localPosition = new Vector3(cameraHolder.localPosition.x, newY + bobOffset, cameraHolder.localPosition.z);
            }
            else
            {
                bobTimer = 0;
                cameraHolder.localPosition = new Vector3(cameraHolder.localPosition.x, newY, cameraHolder.localPosition.z);
            }
        }

        public bool HasCeiling()
        {
            float radius = characterController.radius * 0.9f;
            Vector3 origin = transform.position + Vector3.up * (characterController.height - radius);
            float checkDistance = standingCharacterControllerHeight - characterController.height + 0.1f;

            return Physics.SphereCast(origin, radius, Vector3.up, out _, checkDistance, groundLayer, QueryTriggerInteraction.Ignore);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (((1 << other.gameObject.layer) & waterMask) != 0)
            {
                isInWater = true;
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (((1 << other.gameObject.layer) & waterMask) != 0)
            {
                isInWater = false;
            }
        }
#endregion
}
}
