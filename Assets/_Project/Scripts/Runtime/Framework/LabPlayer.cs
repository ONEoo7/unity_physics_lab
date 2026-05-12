using System.Collections.Generic;
using PhysicsLab.Core;
using UnityEngine;

namespace PhysicsLab.Framework
{
    public sealed class LabPlayer : MonoBehaviour
    {
        [SerializeField] private FirstPersonController controller;
        [SerializeField] private Interactor interactor;
        [SerializeField] private GameObject playerView;

        // Optional: if set, also toggled explicitly. Kept for backward compatibility
        // with scenes that pre-date the "hide every other root" approach below.
        [SerializeField] private GameObject hubHud;
        [SerializeField] private GameObject labEnvironment;

        private readonly List<GameObject> hiddenLabRoots = new();

        private void Reset()
        {
            controller = GetComponentInChildren<FirstPersonController>();
            interactor = GetComponentInChildren<Interactor>();
        }

        private bool subscribed;

        private void OnEnable()
        {
            TrySubscribe();
            SetHubControlsActive(true);
        }

        private void Start()
        {
            // Safety net: if OnEnable raced ahead of LabManager.Awake (despite the
            // execution-order attribute), Start runs strictly after all Awakes.
            TrySubscribe();
        }

        private void OnDisable()
        {
            if (subscribed && LabManager.Instance != null)
            {
                LabManager.Instance.ExperimentEntered -= OnExperimentEntered;
                LabManager.Instance.ExperimentExited -= OnExperimentExited;
            }
            subscribed = false;
        }

        private void TrySubscribe()
        {
            if (subscribed || LabManager.Instance == null) return;
            LabManager.Instance.ExperimentEntered += OnExperimentEntered;
            LabManager.Instance.ExperimentExited += OnExperimentExited;
            subscribed = true;
        }

        private void OnExperimentEntered(ExperimentDefinition _) => SetHubControlsActive(false);
        private void OnExperimentExited() => SetHubControlsActive(true);

        private void SetHubControlsActive(bool active)
        {
            if (controller != null) controller.enabled = active;
            if (interactor != null) interactor.enabled = active;
            if (playerView != null) playerView.SetActive(active);
            if (hubHud != null) hubHud.SetActive(active);
            if (labEnvironment != null) labEnvironment.SetActive(active);

            // Belt-and-suspenders: hide every other root in the lab scene so the
            // experiment camera sees a clean stage. Catches stations, walls, or
            // anything that isn't parented under labEnvironment.
            if (active) RestoreOtherLabRoots();
            else HideOtherLabRoots();

            Cursor.lockState = active ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !active;
        }

        private void HideOtherLabRoots()
        {
            var scene = gameObject.scene;
            if (!scene.IsValid()) return;
            foreach (var root in scene.GetRootGameObjects())
            {
                if (root == gameObject) continue;            // keep us alive for events
                if (!root.activeSelf) continue;              // already hidden
                root.SetActive(false);
                hiddenLabRoots.Add(root);
            }
        }

        private void RestoreOtherLabRoots()
        {
            foreach (var root in hiddenLabRoots)
                if (root != null) root.SetActive(true);
            hiddenLabRoots.Clear();
        }
    }
}
