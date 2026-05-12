using UnityEngine;

namespace PhysicsLab.Core
{
    [CreateAssetMenu(menuName = "Physics Lab/Experiment Definition", fileName = "ExperimentDefinition")]
    public sealed class ExperimentDefinition : ScriptableObject
    {
        [SerializeField] private string id;
        [SerializeField] private string title;
        [TextArea(2, 6)]
        [SerializeField] private string description;
        [SerializeField] private string sceneName;
        [SerializeField] private Sprite previewImage;

        public string Id => string.IsNullOrEmpty(id) ? name : id;
        public string Title => string.IsNullOrEmpty(title) ? name : title;
        public string Description => description;
        public string SceneName => sceneName;
        public Sprite PreviewImage => previewImage;
    }
}
