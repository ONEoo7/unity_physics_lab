using UnityEngine;

namespace PhysicsLab.Core
{
    public abstract class ExperimentBase : MonoBehaviour
    {
        [SerializeField] private ExperimentDefinition definition;

        public ExperimentDefinition Definition => definition;

        protected virtual void OnEnable()
        {
            if (LabManager.Instance != null)
                LabManager.Instance.ExperimentExited += HandleExited;
        }

        protected virtual void OnDisable()
        {
            if (LabManager.Instance != null)
                LabManager.Instance.ExperimentExited -= HandleExited;
        }

        public void RequestExit()
        {
            if (LabManager.Instance != null)
                LabManager.Instance.ExitExperiment();
        }

        protected virtual void HandleExited() { }
    }
}
