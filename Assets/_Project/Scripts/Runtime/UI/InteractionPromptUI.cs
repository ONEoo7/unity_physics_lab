using PhysicsLab.Framework;
using TMPro;
using UnityEngine;

namespace PhysicsLab.UI
{
    public sealed class InteractionPromptUI : MonoBehaviour
    {
        [SerializeField] private Interactor interactor;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private TMP_Text label;
        [SerializeField] private string actionHint = "[E]";

        private void Reset()
        {
            canvasGroup = GetComponent<CanvasGroup>();
        }

        private void OnEnable()
        {
            if (interactor != null) interactor.CurrentChanged += OnCurrentChanged;
            SetVisible(false);
        }

        private void OnDisable()
        {
            if (interactor != null) interactor.CurrentChanged -= OnCurrentChanged;
        }

        private void OnCurrentChanged(IInteractable interactable)
        {
            if (interactable == null)
            {
                SetVisible(false);
                return;
            }
            if (label != null) label.text = $"{actionHint}  {interactable.Prompt}";
            SetVisible(true);
        }

        private void SetVisible(bool visible)
        {
            if (canvasGroup == null) return;
            canvasGroup.alpha = visible ? 1f : 0f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
        }
    }
}
