using UnityEngine;

namespace BadMovieClues.Services
{
    public interface IAudioService
    {
        bool Enabled { get; set; }
        void PlayOneShot(AudioClip clip);
        void PlayMusic(AudioClip clip, float volume = 0.5f);
        void StopMusic();
    }
}
