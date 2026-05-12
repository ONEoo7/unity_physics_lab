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
        [SerializeField] private InputActionReference moveAction;
        [SerializeField] private InputActionReference lookAction;
        [SerializeField] private InputActionReference jumpAction;
        [SerializeField] private InputActionReference sprintAction;

        private CharacterController controller;
        private float pitch;
        private float yaw;
        private float verticalVelocity;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            yaw = transform.eulerAngles.y;
            if (cameraPivot != null) pitch = cameraPivot.localEulerAngles.x;
        }

        private void OnEnable()
        {
            EnableAction(moveAction);
            EnableAction(lookAction);
            EnableAction(jumpAction);
            EnableAction(sprintAction);
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void OnDisable()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private static void EnableAction(InputActionReference reference)
        {
            if (reference != null && reference.action != null) reference.action.Enable();
        }

        private void Update()
        {
            HandleLook();
            HandleMove();
        }

        private void HandleLook()
        {
            if (cameraPivot == null || lookAction == null || lookAction.action == null) return;
            var delta = lookAction.action.ReadValue<Vector2>();
            yaw += delta.x * lookSensitivity;
            pitch = Mathf.Clamp(pitch - delta.y * lookSensitivity, pitchMin, pitchMax);
            transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            cameraPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        }

        private void HandleMove()
        {
            var input = moveAction != null && moveAction.action != null
                ? moveAction.action.ReadValue<Vector2>()
                : Vector2.zero;

            var sprinting = sprintAction != null
                            && sprintAction.action != null
                            && sprintAction.action.IsPressed();
            var speed = sprinting ? sprintSpeed : walkSpeed;
            var move = (transform.right * input.x + transform.forward * input.y) * speed;

            if (controller.isGrounded)
            {
                if (verticalVelocity < 0f) verticalVelocity = -2f;
                var jumpPressed = jumpAction != null
                                  && jumpAction.action != null
                                  && jumpAction.action.WasPressedThisFrame();
                if (jumpPressed) verticalVelocity = Mathf.Sqrt(-2f * gravity * jumpHeight);
            }

            verticalVelocity += gravity * Time.deltaTime;
            move.y = verticalVelocity;
            controller.Move(move * Time.deltaTime);
        }
    }
}
