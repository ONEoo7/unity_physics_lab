using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PhysicsLab.Core
{
    public sealed class LabManager : MonoBehaviour
    {
        public static LabManager Instance { get; private set; }

        [SerializeField] private string hubSceneName = "Lab";

        public event Action<ExperimentDefinition> ExperimentEntered;
        public event Action ExperimentExited;

        public ExperimentDefinition CurrentExperiment { get; private set; }
        public bool IsTransitioning { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void EnterExperiment(ExperimentDefinition experiment)
        {
            if (IsTransitioning || experiment == null) return;
            if (string.IsNullOrEmpty(experiment.SceneName)) return;
            StartCoroutine(EnterExperimentRoutine(experiment));
        }

        public void ExitExperiment()
        {
            if (IsTransitioning || CurrentExperiment == null) return;
            StartCoroutine(ExitExperimentRoutine());
        }

        private IEnumerator EnterExperimentRoutine(ExperimentDefinition experiment)
        {
            IsTransitioning = true;
            var load = SceneManager.LoadSceneAsync(experiment.SceneName, LoadSceneMode.Additive);
            while (!load.isDone) yield return null;

            var loaded = SceneManager.GetSceneByName(experiment.SceneName);
            if (loaded.IsValid())
                SceneManager.SetActiveScene(loaded);

            CurrentExperiment = experiment;
            IsTransitioning = false;
            ExperimentEntered?.Invoke(experiment);
        }

        private IEnumerator ExitExperimentRoutine()
        {
            IsTransitioning = true;
            var unload = SceneManager.UnloadSceneAsync(CurrentExperiment.SceneName);
            while (unload != null && !unload.isDone) yield return null;

            var hub = SceneManager.GetSceneByName(hubSceneName);
            if (hub.IsValid())
                SceneManager.SetActiveScene(hub);

            CurrentExperiment = null;
            IsTransitioning = false;
            ExperimentExited?.Invoke();
        }
    }
}
