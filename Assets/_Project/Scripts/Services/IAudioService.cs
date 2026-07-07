using UnityEngine;

namespace BadMovieClues.Services
{
    public interface IAudioService
    {
        bool Enabled { get; set; }
        void PlayOneShot(AudioClip clip);
    }
}
