using UnityEngine;

namespace BadMovieClues.Services
{
    /// <summary>
    /// Minimal "light touch" audio: fire-and-forget one-shot clips (button
    /// clicks, stings). No AudioManager/mixer - just enough for basic SFX
    /// plus a mute switch, per the plan's audio scope for this pass.
    /// </summary>
    public class SimpleAudioService : IAudioService
    {
        public bool Enabled { get; set; } = true;

        public void PlayOneShot(AudioClip clip)
        {
            if (!Enabled || clip == null) return;
            AudioSource.PlayClipAtPoint(clip, Vector3.zero);
        }
    }
}
