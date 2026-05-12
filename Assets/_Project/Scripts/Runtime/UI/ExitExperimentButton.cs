using PhysicsLab.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace PhysicsLab.UI
{
    public sealed class ExitExperimentButton : MonoBehaviour
    {
        [SerializeField] private Button button;

        [Header("Input")]
        [SerializeField] private InputActionAsset inputActions;
        [SerializeField] private string cancelActionName = "Cancel";

        private InputAction cancelAction;

        private void Reset()
        {
            button = GetComponent<Button>();
        }

        private void Awake()
        {
            if (inputActions != null)
                cancelAction = inputActions.FindAction(cancelActionName, throwIfNotFound: false);
        }

        private void OnEnable()
        {
            if (button != null) button.onClick.AddListener(Exit);
            if (cancelAction != null)
            {
                cancelAction.performed += OnCancel;
                cancelAction.Enable();
            }
        }

        private void OnDisable()
        {
            if (button != null) button.onClick.RemoveListener(Exit);
            if (cancelAction != null) cancelAction.performed -= OnCancel;
        }

        private void OnCancel(InputAction.CallbackContext _) => Exit();

        private void Exit()
        {
            if (LabManager.Instance != null) LabManager.Instance.ExitExperiment();
        }
    }
}
