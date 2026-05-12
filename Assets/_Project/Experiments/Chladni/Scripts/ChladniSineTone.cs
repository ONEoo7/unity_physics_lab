using UnityEngine;

namespace PhysicsLab.Experiments.Chladni
{
    [RequireComponent(typeof(AudioSource))]
    public sealed class ChladniSineTone : MonoBehaviour
    {
        [SerializeField, Range(0f, 0.2f)] private float maxAmplitude = 0.05f;
        [SerializeField] private float smoothingSeconds = 0.05f;

        public float Frequency = 220f;
        public bool Muted = true;

        private double phase;
        private float currentAmplitude;
        private float currentFrequency;
        private int sampleRate;

        private void Awake()
        {
            // OnAudioFilterRead only fires while the AudioSource is playing.
            // Drive it with a looping silent clip so the filter has a steady tick.
            var src = GetComponent<AudioSource>();
            sampleRate = AudioSettings.outputSampleRate;
            if (src.clip == null)
                src.clip = AudioClip.Create("ChladniSilence", sampleRate, 1, sampleRate, false);
            src.loop = true;
            src.playOnAwake = false;
            src.Play();
        }

        private void OnEnable()
        {
            currentFrequency = Frequency;
            currentAmplitude = 0f;
        }

        private void OnAudioFilterRead(float[] data, int channels)
        {
            // Block-rate smoothing toward target amplitude and frequency.
            float targetAmplitude = Muted ? 0f : maxAmplitude;
            int frames = data.Length / channels;
            float blockSeconds = (float)frames / sampleRate;
            float k = smoothingSeconds <= 0f ? 1f : Mathf.Clamp01(blockSeconds / smoothingSeconds);

            currentAmplitude = Mathf.Lerp(currentAmplitude, targetAmplitude, k);
            currentFrequency = Mathf.Lerp(currentFrequency, Frequency, k);

            double increment = 2.0 * System.Math.PI * currentFrequency / sampleRate;
            for (int i = 0; i < frames; i++)
            {
                float sample = (float)System.Math.Sin(phase) * currentAmplitude;
                phase += increment;
                if (phase > 2.0 * System.Math.PI) phase -= 2.0 * System.Math.PI;
                for (int c = 0; c < channels; c++)
                    data[i * channels + c] = sample;
            }
        }
    }
}
