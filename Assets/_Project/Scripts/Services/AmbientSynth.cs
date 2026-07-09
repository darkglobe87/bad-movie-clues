using UnityEngine;

namespace BadMovieClues.Services
{
    /// <summary>
    /// Procedurally synthesizes a smooth, floating ambient pad using mathematical waves.
    /// Requires 0 bytes of disk space and loops infinitely without compression artifacts.
    /// </summary>
    public class AmbientSynth : MonoBehaviour
    {
        private double[] _phases = new double[4];
        
        // Ethereal minor-9th chord frequencies:
        // A2 (110Hz), E3 (165Hz), C4 (261.63Hz), G4 (392Hz)
        private readonly double[] _frequencies = { 110.0, 165.0, 261.63, 392.0 };
        
        private float[] _lfoPhases = new float[4];
        private readonly float[] _lfoSpeeds = { 0.05f, 0.03f, 0.07f, 0.04f }; // Very slow swells

        private void Start()
        {
            var audioSource = GetComponent<AudioSource>();
            if (audioSource != null)
            {
                audioSource.playOnAwake = false;
                audioSource.Stop(); // Ensure it doesn't try to play a missing clip
            }
        }

        private void OnAudioFilterRead(float[] data, int channels)
        {
            double sampleRate = 44100.0;
            
            for (int i = 0; i < data.Length; i += channels)
            {
                float mix = 0f;
                
                for (int tone = 0; tone < _frequencies.Length; tone++)
                {
                    // Update wave phase
                    _phases[tone] += 2.0 * System.Math.PI * _frequencies[tone] / sampleRate;
                    if (_phases[tone] > 2.0 * System.Math.PI) 
                        _phases[tone] -= 2.0 * System.Math.PI;

                    // Update LFO phase for swelling volume
                    _lfoPhases[tone] += (float)(2.0 * System.Math.PI * _lfoSpeeds[tone] / sampleRate);
                    if (_lfoPhases[tone] > 2.0 * System.Math.PI) 
                        _lfoPhases[tone] -= (float)(2.0 * System.Math.PI);
                    
                    // Mix sine wave scaled by the slow LFO
                    float volume = 0.5f + 0.5f * Mathf.Sin(_lfoPhases[tone]);
                    mix += (float)System.Math.Sin(_phases[tone]) * volume * 0.15f;
                }

                // Smooth master output with a low volume to prevent clipping
                float signal = mix / _frequencies.Length * 0.18f;

                for (int c = 0; c < channels; c++)
                {
                    data[i + c] = signal;
                }
            }
        }
    }
}
