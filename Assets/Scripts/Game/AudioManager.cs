// =============================================================================
// AudioManager.cs — Audio Manager for Veil of Uncertainty
// Simple singleton that manages game sound effects using procedurally
// generated tones when no audio clip assets are available.
// =============================================================================

using UnityEngine;

namespace VeilOfUncertainty
{
    /// <summary>
    /// AudioManager manages game sound effects. Uses procedurally generated
    /// sine wave tones since no audio clip assets are available.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        private static AudioManager instance;
        public static AudioManager Instance => instance;

        private AudioSource audioSource;

        // Procedural audio clips
        private AudioClip footstepClip;
        private AudioClip scoutPingClip;
        private AudioClip attackClip;
        private AudioClip damageClip;
        private AudioClip pickupClip;
        private AudioClip victoryClip;
        private AudioClip defeatClip;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;

            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;

            GenerateAudioClips();
        }

        private void GenerateAudioClips()
        {
            footstepClip = CreateTone(220f, 0.08f, 0.3f);   // Short low tap
            scoutPingClip = CreateTone(880f, 0.15f, 0.25f);  // High-pitched blip
            attackClip = CreateTone(330f, 0.12f, 0.35f);      // Quick slash
            damageClip = CreateTone(110f, 0.2f, 0.4f);        // Low thud
            pickupClip = CreateRisingTone(440f, 880f, 0.2f, 0.3f); // Rising chime
            victoryClip = CreateRisingTone(330f, 660f, 0.5f, 0.35f); // Triumphant
            defeatClip = CreateFallingTone(330f, 110f, 0.5f, 0.3f);  // Descending
        }

        public void PlayFootstep() => Play(footstepClip);
        public void PlayScoutPing() => Play(scoutPingClip);
        public void PlayAttack() => Play(attackClip);
        public void PlayDamage() => Play(damageClip);
        public void PlayPickup() => Play(pickupClip);
        public void PlayVictory() => Play(victoryClip);
        public void PlayDefeat() => Play(defeatClip);

        private void Play(AudioClip clip)
        {
            if (clip != null && audioSource != null)
            {
                audioSource.PlayOneShot(clip);
            }
        }

        /// <summary>
        /// Creates a simple sine wave tone.
        /// </summary>
        private static AudioClip CreateTone(float frequency, float duration, float volume)
        {
            int sampleRate = 44100;
            int sampleCount = Mathf.RoundToInt(sampleRate * duration);
            float[] samples = new float[sampleCount];

            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleRate;
                float envelope = 1f - (t / duration); // Linear decay
                samples[i] = Mathf.Sin(2f * Mathf.PI * frequency * t) * volume * envelope;
            }

            AudioClip clip = AudioClip.Create("tone", sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        /// <summary>
        /// Creates a rising pitch tone (for pickup/victory).
        /// </summary>
        private static AudioClip CreateRisingTone(float startFreq, float endFreq,
                                                    float duration, float volume)
        {
            int sampleRate = 44100;
            int sampleCount = Mathf.RoundToInt(sampleRate * duration);
            float[] samples = new float[sampleCount];

            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleRate;
                float progress = t / duration;
                float freq = Mathf.Lerp(startFreq, endFreq, progress);
                float envelope = 1f - (progress * 0.5f); // Gentle decay
                samples[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * volume * envelope;
            }

            AudioClip clip = AudioClip.Create("rising", sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        /// <summary>
        /// Creates a falling pitch tone (for defeat).
        /// </summary>
        private static AudioClip CreateFallingTone(float startFreq, float endFreq,
                                                     float duration, float volume)
        {
            int sampleRate = 44100;
            int sampleCount = Mathf.RoundToInt(sampleRate * duration);
            float[] samples = new float[sampleCount];

            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleRate;
                float progress = t / duration;
                float freq = Mathf.Lerp(startFreq, endFreq, progress);
                float envelope = 1f - (progress * 0.7f);
                samples[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * volume * envelope;
            }

            AudioClip clip = AudioClip.Create("falling", sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
