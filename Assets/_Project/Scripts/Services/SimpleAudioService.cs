using UnityEngine;

namespace BadMovieClues.Services
{
    /// <summary>
    /// Light-touch audio service providing fire-and-forget one-shots
    /// and looping BGM support through a dynamically generated AudioSource.
    /// </summary>
    public class SimpleAudioService : IAudioService
    {
        private AudioSource _musicSource;
        private bool _enabled = true;

        public bool Enabled
        {
            get => _enabled;
            set
            {
                if (_enabled == value) return;
                _enabled = value;
                if (_musicSource != null)
                {
                    _musicSource.mute = !_enabled;
                }
            }
        }

        public void PlayOneShot(AudioClip clip)
        {
            if (!_enabled || clip == null) return;
            AudioSource.PlayClipAtPoint(clip, Vector3.zero);
        }

        public void PlayMusic(AudioClip clip, float volume = 0.5f)
        {
            if (clip == null) return;

            if (_musicSource == null)
            {
                var musicGo = new GameObject("MainMenuBGM", typeof(AudioSource));
                Object.DontDestroyOnLoad(musicGo);
                _musicSource = musicGo.GetComponent<AudioSource>();
                _musicSource.loop = true;
            }

            if (_musicSource.clip == clip && _musicSource.isPlaying) return;

            _musicSource.clip = clip;
            _musicSource.volume = volume;
            _musicSource.mute = !_enabled;
            _musicSource.Play();
        }

        public void StopMusic()
        {
            if (_musicSource != null)
            {
                _musicSource.Stop();
            }
        }
    }
}
