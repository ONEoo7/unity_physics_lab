using System;
using PhysicsLab.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PhysicsLab.Experiments.Chladni
{
    public sealed class ChladniExperiment : ExperimentBase
    {
        [Serializable]
        public struct ModeEntry
        {
            public float Frequency;
            public int N;
            public int M;
        }

        [Header("Wiring")]
        [SerializeField] private ChladniSaltSimulator simulator;
        [SerializeField] private ChladniSineTone sineTone;
        [SerializeField] private Transform plateVisual;

        [Header("UI")]
        [SerializeField] private Slider frequencySlider;
        [SerializeField] private Toggle audioToggle;
        [SerializeField] private Button resetButton;
        [SerializeField] private TMP_Text frequencyLabel;
        [SerializeField] private TMP_Text modeLabel;

        [Header("Modes")]
        [SerializeField] private float minFrequency = 100f;
        [SerializeField] private float maxFrequency = 1850f;
        [SerializeField] private ModeEntry[] modes = DefaultModes();

        private float currentFrequency;

        private static ModeEntry[] DefaultModes() => new[]
        {
            new ModeEntry { Frequency = 100f,  N = 1, M = 2 },
            new ModeEntry { Frequency = 180f,  N = 1, M = 3 },
            new ModeEntry { Frequency = 260f,  N = 2, M = 3 },
            new ModeEntry { Frequency = 340f,  N = 1, M = 4 },
            new ModeEntry { Frequency = 430f,  N = 2, M = 4 },
            new ModeEntry { Frequency = 520f,  N = 3, M = 4 },
            new ModeEntry { Frequency = 620f,  N = 1, M = 5 },
            new ModeEntry { Frequency = 730f,  N = 2, M = 5 },
            new ModeEntry { Frequency = 850f,  N = 3, M = 5 },
            new ModeEntry { Frequency = 980f,  N = 4, M = 5 },
            new ModeEntry { Frequency = 1100f, N = 2, M = 6 },
            new ModeEntry { Frequency = 1250f, N = 3, M = 6 },
            new ModeEntry { Frequency = 1400f, N = 4, M = 6 },
            new ModeEntry { Frequency = 1550f, N = 5, M = 6 },
            new ModeEntry { Frequency = 1700f, N = 3, M = 7 },
            new ModeEntry { Frequency = 1850f, N = 4, M = 7 },
        };

        protected override void OnEnable()
        {
            base.OnEnable();

            // This experiment is mouse-driven (slider/buttons), so claim the cursor.
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            if (frequencySlider != null)
            {
                frequencySlider.minValue = minFrequency;
                frequencySlider.maxValue = maxFrequency;
                frequencySlider.value = Mathf.Clamp(220f, minFrequency, maxFrequency);
                frequencySlider.onValueChanged.AddListener(SetFrequency);
            }
            if (audioToggle != null)
            {
                audioToggle.isOn = false;
                audioToggle.onValueChanged.AddListener(SetAudio);
            }
            if (resetButton != null) resetButton.onClick.AddListener(ResetGrains);

            SetFrequency(frequencySlider != null ? frequencySlider.value : 220f);
            SetAudio(audioToggle != null && audioToggle.isOn);
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            if (frequencySlider != null) frequencySlider.onValueChanged.RemoveListener(SetFrequency);
            if (audioToggle != null) audioToggle.onValueChanged.RemoveListener(SetAudio);
            if (resetButton != null) resetButton.onClick.RemoveListener(ResetGrains);
        }

        private void SetFrequency(float frequency)
        {
            currentFrequency = frequency;
            if (sineTone != null) sineTone.Frequency = frequency;

            ResolveBlend(frequency, out int iA, out int iB, out float blend);
            var a = modes[iA];
            var b = modes[iB];
            if (simulator != null)
            {
                simulator.NA = a.N;
                simulator.MA = a.M;
                simulator.NB = b.N;
                simulator.MB = b.M;
                simulator.Blend = blend;
                simulator.VibrationEnvelope = 1f;
            }
            if (frequencyLabel != null) frequencyLabel.text = $"{frequency:F0} Hz";
            if (modeLabel != null)
            {
                modeLabel.text = Mathf.Approximately(blend, 0f)
                    ? $"({a.N}, {a.M})"
                    : Mathf.Approximately(blend, 1f)
                        ? $"({b.N}, {b.M})"
                        : $"({a.N}, {a.M}) → ({b.N}, {b.M})  t={blend:F2}";
            }
        }

        private void SetAudio(bool on)
        {
            if (sineTone != null) sineTone.Muted = !on;
        }

        private void ResetGrains()
        {
            if (simulator != null) simulator.ResetGrains();
        }

        private void ResolveBlend(float frequency, out int iA, out int iB, out float blend)
        {
            if (modes == null || modes.Length == 0)
            {
                iA = iB = 0;
                blend = 0f;
                return;
            }
            if (frequency <= modes[0].Frequency)
            {
                iA = iB = 0;
                blend = 0f;
                return;
            }
            if (frequency >= modes[modes.Length - 1].Frequency)
            {
                iA = iB = modes.Length - 1;
                blend = 0f;
                return;
            }
            for (int i = 0; i < modes.Length - 1; i++)
            {
                if (frequency >= modes[i].Frequency && frequency <= modes[i + 1].Frequency)
                {
                    iA = i;
                    iB = i + 1;
                    float span = modes[iB].Frequency - modes[iA].Frequency;
                    blend = span > 0f ? (frequency - modes[iA].Frequency) / span : 0f;
                    return;
                }
            }
            iA = iB = 0;
            blend = 0f;
        }
    }
}
