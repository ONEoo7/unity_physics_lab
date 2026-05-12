using PhysicsLab.Core;
using UnityEngine;

namespace PhysicsLab.Framework
{
    public sealed class LabPlayer : MonoBehaviour
    {
        [SerializeField] private FirstPersonController controller;
        [SerializeField] private Interactor interactor;
        [SerializeField] private GameObject playerView;
        [SerializeField] private GameObject hubHud;
        [SerializeField] private GameObject labEnvironment;

        private void Reset()
        {
            controller = GetComponentInChildren<FirstPersonController>();
            interactor = GetComponentInChildren<Interactor>();
        }

        private void OnEnable()
        {
            if (LabManager.Instance != null)
            {
                LabManager.Instance.ExperimentEntered += OnExperimentEntered;
                LabManager.Instance.ExperimentExited += OnExperimentExited;
            }
            SetHubControlsActive(true);
        }

        private void OnDisable()
        {
            if (LabManager.Instance != null)
            {
                LabManager.Instance.ExperimentEntered -= OnExperimentEntered;
                LabManager.Instance.ExperimentExited -= OnExperimentExited;
            }
        }

        private void OnExperimentEntered(ExperimentDefinition _) => SetHubControlsActive(false);
        private void OnExperimentExited() => SetHubControlsActive(true);

        private void SetHubControlsActive(bool active)
        {
            if (controller != null) controller.enabled = active;
            if (interactor != null) interactor.enabled = active;
            if (playerView != null) playerView.SetActive(active);
            if (hubHud != null) hubHud.SetActive(active);
            // Hide the lab room, lights, and station pedestals so the experiment
            // camera doesn't render them peeking into its frame.
            if (labEnvironment != null) labEnvironment.SetActive(active);

            Cursor.lockState = active ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !active;
        }
    }
}
