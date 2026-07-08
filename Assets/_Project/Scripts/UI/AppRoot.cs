using BadMovieClues.Data;
using BadMovieClues.Economy;
using BadMovieClues.Progression;
using BadMovieClues.Services;
using UnityEngine;

namespace BadMovieClues.UI
{
    /// <summary>
    /// Persistent (DontDestroyOnLoad) composition root for app-lifetime
    /// services - constructed once via RuntimeInitializeOnLoadMethod so it
    /// exists before any scene's own Awake/Start runs, regardless of which
    /// scene happens to be first (robust against a Splash scene being
    /// inserted ahead of MainMenu later, without needing to move anything).
    /// GameBootstrap reads services from here on each gameplay scene load
    /// instead of constructing its own.
    /// </summary>
    public class AppRoot : MonoBehaviour
    {
        public static AppRoot Instance { get; private set; }

        public ISaveService SaveService { get; private set; }
        public ICurrencyService Currency { get; private set; }
        public GameConfig Config { get; private set; }
        public IContentProvider ContentProvider { get; private set; }
        public IAudioService AudioService { get; private set; }
        public ParticleSystem DustParticles { get; private set; }
        public IUserSettings Settings { get; private set; }
        public IProgressService Progress { get; private set; }

        /// <summary>Set by LevelSelectScreen before navigating to Gameplay;
        /// read by GameBootstrap instead of always loading index 0.</summary>
        public int SelectedLevelIndex { get; set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            if (Instance != null) return;

            var go = new GameObject("AppRoot");
            DontDestroyOnLoad(go);
            var appRoot = go.AddComponent<AppRoot>();
            appRoot.Initialize();

            var navigatorGo = new GameObject("ScreenNavigator");
            navigatorGo.transform.SetParent(go.transform, false);
            navigatorGo.AddComponent<ScreenNavigator>();
        }

        private void Initialize()
        {
            Instance = this;

            Config = Resources.Load<GameConfig>("GameConfig");
            SaveService = new LocalJsonSaveService();
            Currency = new CurrencyService(SaveService, Config.StartingBalance);
            ContentProvider = new BundledContentProvider();
            AudioService = new SimpleAudioService();
            DustParticles = AmbientDustBackground.Build(transform);
            Progress = new ProgressService(SaveService);

            // Applied last since it depends on AudioService/DustParticles
            // already existing to push the loaded values onto them.
            Settings = new SettingsService(SaveService);
            ApplySettings();
            Settings.Changed += ApplySettings;
        }

        private void ApplySettings()
        {
            AudioService.Enabled = Settings.AudioEnabled;
            AmbientDustBackground.SetReducedEffects(DustParticles, Settings.ReducedEffects);
        }
    }
}
