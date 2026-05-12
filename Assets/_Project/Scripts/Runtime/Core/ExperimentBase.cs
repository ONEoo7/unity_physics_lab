using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace PhysicsLab.Core
{
    public abstract class ExperimentBase : MonoBehaviour
    {
        [SerializeField] private ExperimentDefinition definition;

        public ExperimentDefinition Definition => definition;

        private readonly List<AudioListener> suppressedListeners = new();
        private readonly List<EventSystem> suppressedEventSystems = new();

        protected virtual void OnEnable()
        {
            if (LabManager.Instance != null)
                LabManager.Instance.ExperimentExited += HandleExited;

            SuppressOtherScenes();
        }

        protected virtual void OnDisable()
        {
            if (LabManager.Instance != null)
                LabManager.Instance.ExperimentExited -= HandleExited;

            RestoreOtherScenes();
        }

        public void RequestExit()
        {
            if (LabManager.Instance != null)
                LabManager.Instance.ExitExperiment();
        }

        protected virtual void HandleExited() { }

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
