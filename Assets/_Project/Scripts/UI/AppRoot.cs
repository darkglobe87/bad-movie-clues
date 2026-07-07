using BadMovieClues.Data;
using BadMovieClues.Economy;
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
        }
    }
}
