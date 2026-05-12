using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PhysicsLab.Framework
{
    public sealed class Interactor : MonoBehaviour
    {
        [SerializeField] private Camera viewCamera;
        [SerializeField] private float maxDistance = 3.0f;
        [SerializeField] private LayerMask interactableLayers = ~0;
        [SerializeField] private InputActionReference interactAction;

        public event Action<IInteractable> CurrentChanged;

        private IInteractable current;

        public IInteractable Current
        {
            get => current;
            private set
            {
                if (ReferenceEquals(current, value)) return;
                current = value;
                CurrentChanged?.Invoke(current);
            }
        }

        private void Awake()
        {
            if (viewCamera == null) viewCamera = Camera.main;
        }

        private void OnEnable()
        {
            if (interactAction != null && interactAction.action != null)
            {
                interactAction.action.performed += OnInteract;
                interactAction.action.Enable();
            }
        }

        private void OnDisable()
        {
            if (interactAction != null && interactAction.action != null)
            {
                interactAction.action.performed -= OnInteract;
            }
        }

        private void Update()
        {
            if (viewCamera == null)
            {
                Current = null;
                return;
            }

            var ray = new Ray(viewCamera.transform.position, viewCamera.transform.forward);
            if (Physics.Raycast(ray, out var hit, maxDistance, interactableLayers, QueryTriggerInteraction.Collide)
                && hit.collider.TryGetComponent<IInteractable>(out var hitInteractable)
                && hitInteractable.CanInteract)
            {
                Current = hitInteractable;
            }
            else
            {
                Current = null;
            }
        }

        private void OnInteract(InputAction.CallbackContext ctx)
        {
            if (Current != null && Current.CanInteract) Current.Interact();
        }
    }
}
