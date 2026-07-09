using System;

namespace BadMovieClues.Services
{
    public class SettingsService : IUserSettings
    {
        private const string SaveKey = "user_settings";
        private readonly ISaveService _saveService;
        private bool _reducedEffects;
        private bool _audioEnabled;
        private bool _hapticsEnabled;

        public event Action Changed;

        public SettingsService(ISaveService saveService)
        {
            _saveService = saveService;
            if (_saveService.TryLoad(SaveKey, out SettingsData loaded))
            {
                _reducedEffects = loaded.ReducedEffects;
                _audioEnabled = loaded.AudioEnabled;
                _hapticsEnabled = loaded.HapticsEnabled;
            }
            else
            {
                _audioEnabled = true;
                _hapticsEnabled = true;
            }
        }

        public bool ReducedEffects
        {
            get => _reducedEffects;
            set
            {
                if (_reducedEffects == value) return;
                _reducedEffects = value;
                Persist();
                Changed?.Invoke();
            }
        }

        public bool AudioEnabled
        {
            get => _audioEnabled;
            set
            {
                if (_audioEnabled == value) return;
                _audioEnabled = value;
                Persist();
                Changed?.Invoke();
            }
        }

        public bool HapticsEnabled
        {
            get => _hapticsEnabled;
            set
            {
                if (_hapticsEnabled == value) return;
                _hapticsEnabled = value;
                Persist();
                Changed?.Invoke();
            }
        }

        private void Persist() => _saveService.Save(SaveKey, new SettingsData
        {
            ReducedEffects = _reducedEffects,
            AudioEnabled = _audioEnabled,
            HapticsEnabled = _hapticsEnabled,
        });

    [System.Serializable]
    public class SettingsData
    {
        public bool ReducedEffects;
        public bool AudioEnabled;
        public bool HapticsEnabled;
    }
    }
}
