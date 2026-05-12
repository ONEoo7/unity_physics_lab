using PhysicsLab.Core;
using UnityEngine;

namespace PhysicsLab.Framework
{
    [RequireComponent(typeof(Collider))]
    public sealed class ExperimentStation : MonoBehaviour, IInteractable
    {
        [SerializeField] private ExperimentDefinition definition;

        public ExperimentDefinition Definition => definition;

        public string Prompt =>
            definition != null ? $"Enter: {definition.Title}" : "Enter experiment";

        public bool CanInteract =>
            definition != null
            && LabManager.Instance != null
            && !LabManager.Instance.IsTransitioning
            && LabManager.Instance.CurrentExperiment == null;

        public void Interact()
        {
            if (!CanInteract) return;
            LabManager.Instance.EnterExperiment(definition);
        }
    }
}
