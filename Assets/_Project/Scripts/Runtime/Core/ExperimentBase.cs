using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace PhysicsLab.Core
{
    public abstract class ExperimentBase : MonoBehaviour
    {
        [SerializeField] private ExperimentDefinition definition;

        public ExperimentDefinition Definition => definition;

        // Override per experiment if it wants a locked cursor (e.g., a free-look
        // viewer). Default is "visible mouse for UI interaction".
        protected virtual CursorLockMode DesiredCursorLockMode => CursorLockMode.None;
        protected virtual bool DesiredCursorVisible => true;

        private readonly List<AudioListener> suppressedListeners = new();
        private readonly List<EventSystem> suppressedEventSystems = new();

        protected virtual void OnEnable()
        {
            if (LabManager.Instance != null)
                LabManager.Instance.ExperimentExited += HandleExited;

            SuppressOtherScenes();
            ApplyCursor();
        }

        protected virtual void OnDisable()
        {
            if (LabManager.Instance != null)
                LabManager.Instance.ExperimentExited -= HandleExited;

            RestoreOtherScenes();
        }

        // Unity unlocks the cursor when the Game window loses focus and re-locks
        // to whatever state was active before focus loss when it returns. After
        // entering an experiment from the hub, that means the cursor re-locks
        // (hub state) on focus return, hiding it. Re-assert our preference here.
        private void OnApplicationFocus(bool focused)
        {
            if (!isActiveAndEnabled || !focused) return;
            ApplyCursor();
        }

        public void RequestExit()
        {
            if (LabManager.Instance != null)
                LabManager.Instance.ExitExperiment();
        }

        protected virtual void HandleExited() { }

        private void ApplyCursor()
        {
            Cursor.lockState = DesiredCursorLockMode;
            Cursor.visible = DesiredCursorVisible;
        }

        private void SuppressOtherScenes()
        {
            var mine = gameObject.scene;
            foreach (var listener in FindObjectsByType<AudioListener>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (listener.gameObject.scene != mine && listener.enabled)
                {
                    listener.enabled = false;
                    suppressedListeners.Add(listener);
                }
            }
            foreach (var es in FindObjectsByType<EventSystem>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (es.gameObject.scene != mine && es.enabled)
                {
                    es.enabled = false;
                    suppressedEventSystems.Add(es);
                }
            }
        }

        private void RestoreOtherScenes()
        {
            foreach (var l in suppressedListeners) if (l != null) l.enabled = true;
            foreach (var e in suppressedEventSystems) if (e != null) e.enabled = true;
            suppressedListeners.Clear();
            suppressedEventSystems.Clear();
        }
    }
}
