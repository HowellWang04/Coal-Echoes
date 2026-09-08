using UnityEngine;
using UnityEngine.InputSystem;

namespace Controller.FinalCharacterController
{
    [DefaultExecutionOrder(-1)]
    public class PlayerMovementManager : MonoBehaviour
    {
        [Header("Game Object Refs")]
        [SerializeField] private CharacterController characterController;
        [SerializeField] private Camera playerCamera;

        [Header("Movement")]
        public float maxSpeed = 4f;
        public float acceleration = 20f;
        public float deceleration = 25f;
        public float gravity = -9.81f;

        [Header("Look")]
        public float lookSensitivity = 0.15f;
        public float lookLimit = 85f;

        [Header("State")]
        [SerializeField] private bool allowControl = true;


        private Vector3 playerFinalVelocity;            
        private float yaw;                   
        private float pitch;                 

        private PlayerInputProvider inputProvider;
        private InputAction movementAction;
        private InputAction lookAction;
        private Vector2 movementInput;
        private Vector2 lookInput;

        public bool AllowControl
        {
            get => allowControl;
            set => allowControl = value;
        }

        public bool IsGrounded => characterController.isGrounded;
        public bool IsMoving => new Vector3(playerFinalVelocity.x, 0f, playerFinalVelocity.z).sqrMagnitude > 0.01f;

        private void Awake()
        {
            inputProvider = GetComponent<PlayerInputProvider>();

            yaw = transform.eulerAngles.y;
        }

        private void OnEnable()
        {
            movementAction = inputProvider.Controls.Gameplay.Movement;
            lookAction = inputProvider.Controls.Gameplay.Look;

            movementAction.performed += OnMovement;
            movementAction.canceled += OnMovement;
            lookAction.performed += OnLook;
            lookAction.canceled += OnLook;
        }

        private void OnDisable()
        {
            movementAction.performed -= OnMovement;
            movementAction.canceled -= OnMovement;
            lookAction.performed -= OnLook;
            lookAction.canceled -= OnLook;
        }

        private void OnMovement(InputAction.CallbackContext context)
        {
            movementInput = context.ReadValue<Vector2>();
        }

        private void OnLook(InputAction.CallbackContext context)
        {
            lookInput = context.ReadValue<Vector2>();
        }

        private void Update()
        {
            if (!allowControl) return;

            HandleMovement();
        }

        private void LateUpdate()
        {
            if (!allowControl) return;

            HandleLook();
        }

        private void HandleMovement()
        {
            // Project camera forward/right to ground plane
            Vector3 forward = Vector3.ProjectOnPlane(playerCamera.transform.forward, Vector3.up).normalized;
            Vector3 right   = Vector3.ProjectOnPlane(playerCamera.transform.right,   Vector3.up).normalized;

            // Get direction by input and camera orientation
            Vector3 wishDir = right * movementInput.x + forward * movementInput.y;
            wishDir = wishDir.sqrMagnitude > 1f ? wishDir.normalized : wishDir;

            Vector3 targetVelocity = wishDir * maxSpeed;

            float accel = (wishDir.sqrMagnitude > 0.01f) ? acceleration : deceleration;

            
            Vector3 horizontalVelocity = new Vector3(playerFinalVelocity.x, 0f, playerFinalVelocity.z);
            // Smoothly move from current velocity towards target velocity
            horizontalVelocity = Vector3.MoveTowards(
                horizontalVelocity,
                targetVelocity,
                accel * Time.deltaTime
            );

            // Gravity
            if (characterController.isGrounded && playerFinalVelocity.y < 0f)
                playerFinalVelocity.y = -2f; // Stick to ground

            playerFinalVelocity.y += gravity * Time.deltaTime;

            // Combine horizontal and vertical velocity
            playerFinalVelocity.x = horizontalVelocity.x;
            playerFinalVelocity.z = horizontalVelocity.z;

            // Use playerFinalVelocity to move character
            characterController.Move(playerFinalVelocity * Time.deltaTime);
        }

        private void HandleLook()
        {
            yaw   += lookInput.x * lookSensitivity;
            pitch -= lookInput.y * lookSensitivity;
            pitch = Mathf.Clamp(pitch, -lookLimit, lookLimit);

            // Apply rotations to player, whole body turns Y (left/right)
            transform.rotation = Quaternion.Euler(0f, yaw, 0f);

            // Apply rotations to camera, only X (up/down)
            playerCamera.transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        }

        public void SetPlayerMovementControl(bool value)
        {
            allowControl = value;
        }
    }
}
