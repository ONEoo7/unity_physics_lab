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

        private void OnEnable()
        {
            if (LabManager.Instance != null)
            {
                LabManager.Instance.ExperimentEntered += OnExperimentEntered;
                LabManager.Instance.ExperimentExited += OnExperimentExited;
                Debug.Log($"[LabPlayer] OnEnable subscribed to LabManager (instance={LabManager.Instance.name})");
            }
            else
            {
                Debug.LogWarning("[LabPlayer] OnEnable: LabManager.Instance was null, NOT subscribing. Lab will not hide on experiment enter.");
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

        private void OnExperimentEntered(ExperimentDefinition def)
        {
            Debug.Log($"[LabPlayer] OnExperimentEntered fired for '{def?.Title}', disabling hub.");
            SetHubControlsActive(false);
        }

        private void OnExperimentExited()
        {
            Debug.Log("[LabPlayer] OnExperimentExited fired, re-enabling hub.");
            SetHubControlsActive(true);
        }

        private void SetHubControlsActive(bool active)
        {
            Debug.Log($"[LabPlayer] SetHubControlsActive({active}) | controller={(controller != null)} interactor={(interactor != null)} playerView={(playerView != null)} hubHud={(hubHud != null)} labEnvironment={(labEnvironment != null ? labEnvironment.name : "NULL")}");

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
            int hidden = 0;
            foreach (var root in scene.GetRootGameObjects())
            {
                if (root == gameObject) continue;            // keep us alive for events
                if (!root.activeSelf) continue;              // already hidden
                root.SetActive(false);
                hiddenLabRoots.Add(root);
                hidden++;
            }
            Debug.Log($"[LabPlayer] HideOtherLabRoots: hid {hidden} root(s) in scene '{scene.name}'.");
        }

        private void RestoreOtherLabRoots()
        {
            foreach (var root in hiddenLabRoots)
                if (root != null) root.SetActive(true);
            hiddenLabRoots.Clear();
        }
    }
}
