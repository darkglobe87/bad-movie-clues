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
        public AmbientDustBackground.DustSystems DustParticles { get; private set; }
        public IUserSettings Settings { get; private set; }
        public IProgressService Progress { get; private set; }
        public IPurchaseService Purchases { get; private set; }
        public IHapticsService Haptics { get; private set; }
        public DailyPuzzleService DailyChallenge { get; private set; }
        public RetentionService Retention { get; private set; }

        /// <summary>Set by LevelSelectScreen before navigating to Gameplay;
        /// read by GameBootstrap instead of always loading index 0.</summary>
        public int SelectedLevelIndex { get; set; }

        /// <summary>True when the current Gameplay scene was launched as a
        /// daily challenge, not a normal level-select pick.</summary>
        public bool IsDailyChallenge { get; set; }

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

            // Mobile CPU/GPU Optimizations
            Application.targetFrameRate = 60;
            QualitySettings.vSyncCount = 0;

            Config = Resources.Load<GameConfig>("GameConfig");
            SaveService = new LocalJsonSaveService();
            Currency = new CurrencyService(SaveService, Config.StartingBalance);
            ContentProvider = new BundledContentProvider();
            AudioService = new SimpleAudioService();
            DustParticles = AmbientDustBackground.Build(transform);
            Progress = new ProgressService(SaveService);
            Purchases = new StubPurchaseService(Currency);
            Haptics = new AndroidHapticsService();
            DailyChallenge = new DailyPuzzleService(SaveService);
            Retention = new RetentionService(SaveService);

            // Applied last since it depends on AudioService/DustParticles
            // already existing to push the loaded values onto them.
            Settings = new SettingsService(SaveService);
            ApplySettings();
            Settings.Changed += ApplySettings;

            // Start BGM
            var bgmClip = Resources.Load<AudioClip>("Audio/BGM");
            AudioService.PlayMusic(bgmClip, 0.4f);
        }

        private void ApplySettings()
        {
            AudioService.Enabled = Settings.AudioEnabled;
            AmbientDustBackground.SetReducedEffects(DustParticles, Settings.ReducedEffects);
            if (Haptics != null) Haptics.Enabled = Settings.HapticsEnabled;
        }
    }
}
