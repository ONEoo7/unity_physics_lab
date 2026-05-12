using UnityEngine;
using UnityEngine.InputSystem;

namespace PhysicsLab.Framework
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class FirstPersonController : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float walkSpeed = 4.0f;
        [SerializeField] private float sprintSpeed = 6.5f;
        [SerializeField] private float jumpHeight = 1.2f;
        [SerializeField] private float gravity = -19.62f;

        [Header("Look")]
        [SerializeField] private Transform cameraPivot;
        [SerializeField] private float lookSensitivity = 0.1f;
        [SerializeField] private float pitchMin = -85f;
        [SerializeField] private float pitchMax = 85f;

        [Header("Input")]
        [SerializeField] private InputActionAsset inputActions;
        [SerializeField] private string moveActionName = "Move";
        [SerializeField] private string lookActionName = "Look";
        [SerializeField] private string jumpActionName = "Jump";
        [SerializeField] private string sprintActionName = "Sprint";

        private CharacterController controller;
        private InputAction moveAction;
        private InputAction lookAction;
        private InputAction jumpAction;
        private InputAction sprintAction;
        private float pitch;
        private float yaw;
        private float verticalVelocity;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            yaw = transform.eulerAngles.y;
            if (cameraPivot != null) pitch = cameraPivot.localEulerAngles.x;

            if (inputActions != null)
            {
                moveAction = inputActions.FindAction(moveActionName, throwIfNotFound: false);
                lookAction = inputActions.FindAction(lookActionName, throwIfNotFound: false);
                jumpAction = inputActions.FindAction(jumpActionName, throwIfNotFound: false);
                sprintAction = inputActions.FindAction(sprintActionName, throwIfNotFound: false);
            }
        }

        private void OnEnable()
        {
            moveAction?.Enable();
            lookAction?.Enable();
            jumpAction?.Enable();
            sprintAction?.Enable();
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void OnDisable()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void Update()
        {
            HandleLook();
            HandleMove();
        }

        private void HandleLook()
        {
            if (cameraPivot == null || lookAction == null) return;
            var delta = lookAction.ReadValue<Vector2>();
            yaw += delta.x * lookSensitivity;
            pitch = Mathf.Clamp(pitch - delta.y * lookSensitivity, pitchMin, pitchMax);
            transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            cameraPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        }

        private void HandleMove()
        {
            var input = moveAction != null ? moveAction.ReadValue<Vector2>() : Vector2.zero;
            var sprinting = sprintAction != null && sprintAction.IsPressed();
            var speed = sprinting ? sprintSpeed : walkSpeed;
            var move = (transform.right * input.x + transform.forward * input.y) * speed;

            if (controller.isGrounded)
            {
                if (verticalVelocity < 0f) verticalVelocity = -2f;
                if (jumpAction != null && jumpAction.WasPressedThisFrame())
                    verticalVelocity = Mathf.Sqrt(-2f * gravity * jumpHeight);
            }

            verticalVelocity += gravity * Time.deltaTime;
            move.y = verticalVelocity;
            controller.Move(move * Time.deltaTime);
        }
    }
}
