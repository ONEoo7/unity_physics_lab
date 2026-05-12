using PhysicsLab.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace PhysicsLab.UI
{
    public sealed class ExitExperimentButton : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private InputActionReference cancelAction;

        private void Reset()
        {
            button = GetComponent<Button>();
        }

        private void OnEnable()
        {
            if (button != null) button.onClick.AddListener(Exit);
            if (cancelAction != null && cancelAction.action != null)
            {
                cancelAction.action.performed += OnCancel;
                cancelAction.action.Enable();
            }
        }

        private void OnDisable()
        {
            if (button != null) button.onClick.RemoveListener(Exit);
            if (cancelAction != null && cancelAction.action != null)
                cancelAction.action.performed -= OnCancel;
        }

        private void OnCancel(InputAction.CallbackContext _) => Exit();

        private void Exit()
        {
            if (LabManager.Instance != null) LabManager.Instance.ExitExperiment();
        }
    }
}
