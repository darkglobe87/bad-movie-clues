using System;
using BadMovieClues.Data;
using BadMovieClues.Economy;
using BadMovieClues.Progression;
using BadMovieClues.Services;
using PrimeTween;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BadMovieClues.UI
{
    /// <summary>
    /// Composition root + view for MainMenu.unity: title, Play/Store/Settings
    /// buttons, and the LevelSelectScreen/SettingsScreen panels toggled in
    /// place of the button row. Built entirely in code at runtime with rich
    /// animations, staggered button slide-ins, glowing titles, and coin display.
    /// </summary>
    public class MainMenuScreen : MonoBehaviour
    {
        [SerializeField] private UITheme theme;
        [SerializeField] private AudioClip clickSound;
        [SerializeField] private RectTransform canvasRoot;

        private IAudioService _audio;
        private RectTransform _title;
        private RectTransform _buttonPanel;
        private TextMeshProUGUI _coinText;
        private SettingsScreen _settingsScreen;
        private LevelSelectScreen _levelSelectScreen;
        private StoreScreen _storeScreen;
        private LevelCatalog _catalog;
        private DailyRewardModal _dailyRewardModal;
        private Button _dailyChallengeButton;
        private TextMeshProUGUI _streakText;

        private async void Start()
        {
            Debug.Log("[MainMenuScreen] Start() beginning execution...");
            try
            {
                var app = AppRoot.Instance;
                if (app == null)
                {
                    Debug.LogError("[MainMenuScreen] AppRoot.Instance is null!");
                    return;
                }
                _audio = app.AudioService;

                Debug.Log("[MainMenuScreen] Building UI elements...");
                BuildTitle();
                Debug.Log("[MainMenuScreen] Title built.");
                BuildButtonPanel();
                Debug.Log("[MainMenuScreen] Button panel built.");
                BuildCoinDisplay();
                Debug.Log("[MainMenuScreen] Coin display built.");
                BuildStreakBadge();
                Debug.Log("[MainMenuScreen] Streak badge built.");
                BuildVersionText();
                Debug.Log("[MainMenuScreen] Version text built.");

                if (app.Currency != null)
                {
                    app.Currency.OnBalanceChanged += OnBalanceChanged;
                }
                else
                {
                    Debug.LogWarning("[MainMenuScreen] app.Currency is null!");
                }

                Debug.Log("[MainMenuScreen] Loading level catalog asynchronously...");
                var catalog = await app.ContentProvider.LoadCatalogAsync();
                Debug.Log($"[MainMenuScreen] Catalog loaded successfully. Level count: {catalog?.Levels?.Count}");

                Debug.Log("[MainMenuScreen] Initializing SettingsScreen...");
                var settingsGo = new GameObject("SettingsScreen", typeof(RectTransform));
                settingsGo.transform.SetParent(canvasRoot, false);
                StretchFull((RectTransform)settingsGo.transform);
                _settingsScreen = settingsGo.AddComponent<SettingsScreen>();
                _settingsScreen.Init(theme, clickSound, app.Settings, app.Currency, app.Progress, app.Config, OnPanelClosed);
                _settingsScreen.gameObject.SetActive(false);

                Debug.Log("[MainMenuScreen] Initializing LevelSelectScreen...");
                var levelSelectGo = new GameObject("LevelSelectScreen", typeof(RectTransform));
                levelSelectGo.transform.SetParent(canvasRoot, false);
                StretchFull((RectTransform)levelSelectGo.transform);
                _levelSelectScreen = levelSelectGo.AddComponent<LevelSelectScreen>();
                _levelSelectScreen.Init(theme, clickSound, app.Progress, catalog, OnPanelClosed);
                _levelSelectScreen.gameObject.SetActive(false);

                Debug.Log("[MainMenuScreen] Initializing StoreScreen...");
                var storeGo = new GameObject("StoreScreen", typeof(RectTransform));
                storeGo.transform.SetParent(canvasRoot, false);
                StretchFull((RectTransform)storeGo.transform);
                _storeScreen = storeGo.AddComponent<StoreScreen>();
                _storeScreen.Init(theme, clickSound, app.Purchases, app.Currency, OnPanelClosed);
                _storeScreen.gameObject.SetActive(false);

                _catalog = catalog;

                // Show daily reward modal if unclaimed
                if (app.Retention != null && !app.Retention.HasClaimedToday)
                {
                    Debug.Log("[MainMenuScreen] Showing daily reward modal...");
                    ShowDailyRewardModal(app);
                }

                RefreshDailyChallengeButton();
                Debug.Log("[MainMenuScreen] Start() completed successfully!");
            }
            catch (Exception e)
            {
                Debug.LogError($"[MainMenuScreen] Exception in Start(): {e}");
                ShowErrorPanel("Failed to load level catalog.\nPlease restart the app.");
            }
        }

        private void ShowErrorPanel(string message)
        {
            var errorGo = new GameObject("ErrorPanel", typeof(RectTransform), typeof(Image));
            errorGo.transform.SetParent(canvasRoot, false);
            StretchFull((RectTransform)errorGo.transform);
            
            var img = errorGo.GetComponent<Image>();
            img.color = new Color(0f, 0f, 0f, 0.85f);
            
            var text = UIText(errorGo.transform, message, 20, FontStyles.Normal);
            var textRt = text.rectTransform;
            textRt.anchorMin = new Vector2(0.1f, 0.4f);
            textRt.anchorMax = new Vector2(0.9f, 0.6f);
            textRt.offsetMin = textRt.offsetMax = Vector2.zero;
            text.color = theme != null ? theme.NeutralLight : Color.white;
            
            if (_buttonPanel != null) _buttonPanel.gameObject.SetActive(false);
            if (_title != null) _title.gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            var app = AppRoot.Instance;
            if (app != null && app.Currency != null)
            {
                app.Currency.OnBalanceChanged -= OnBalanceChanged;
            }
        }

        private void OnBalanceChanged(int balance)
        {
            UpdateCoinDisplay();
        }

        private void BuildTitle()
        {
            var title = UIText(canvasRoot, "Bad Movie Clues", 54, TMPro.FontStyles.Bold);
            _title = title.rectTransform;
            _title.anchorMin = new Vector2(0.1f, 0.72f);
            _title.anchorMax = new Vector2(0.9f, 0.85f);
            _title.offsetMin = _title.offsetMax = Vector2.zero;
            if (theme != null)
            {
                title.color = theme.NeutralLight;
                if (theme.HeadingFont != null) title.font = theme.HeadingFont;
            }

            // Glow title text behind
            var glowGo = new GameObject("GlowText", typeof(RectTransform));
            glowGo.transform.SetParent(_title, false);
            var glowText = glowGo.AddComponent<TextMeshProUGUI>();
            glowText.text = title.text;
            glowText.fontSize = title.fontSize;
            glowText.fontStyle = title.fontStyle;
            glowText.alignment = title.alignment;
            if (theme != null)
            {
                glowText.color = theme.GlowGold;
                if (theme.HeadingFont != null) glowText.font = theme.HeadingFont;
            }
            else
            {
                glowText.color = new Color32(0xFF, 0xD7, 0x70, 0x80);
            }
            var glowRt = (RectTransform)glowGo.transform;
            StretchFull(glowRt);
            glowRt.localPosition = new Vector3(2f, -2f, 0f);

            var glowGroup = glowGo.AddComponent<CanvasGroup>();
            Tween.Alpha(glowGroup, startValue: 0.2f, endValue: 0.8f, duration: 1.5f, cycles: -1, cycleMode: CycleMode.Yoyo);
        }

        private void BuildButtonPanel()
        {
            var panelGo = new GameObject("ButtonPanel", typeof(RectTransform), typeof(VerticalLayoutGroup));
            panelGo.transform.SetParent(canvasRoot, false);
            _buttonPanel = (RectTransform)panelGo.transform;
            _buttonPanel.anchorMin = new Vector2(0.2f, 0.28f);
            _buttonPanel.anchorMax = new Vector2(0.8f, 0.68f);
            _buttonPanel.offsetMin = _buttonPanel.offsetMax = Vector2.zero;

            var layout = panelGo.GetComponent<VerticalLayoutGroup>();
            layout.spacing = 14;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;

            // Slide-in staggered animations
            BuildMenuButton(_buttonPanel, "PLAY", OnPlayClicked, 0.1f);
            _dailyChallengeButton = BuildMenuButton(_buttonPanel, "DAILY CHALLENGE", OnDailyChallengeClicked, 0.15f);
            Button adBtn = null;
            adBtn = BuildMenuButton(_buttonPanel, "FREE COINS (AD)", () => OnFreeCoinsAdClicked(adBtn), 0.2f);
            BuildMenuButton(_buttonPanel, "STORE", OnStoreClicked, 0.25f);
            BuildMenuButton(_buttonPanel, "SETTINGS", OnSettingsClicked, 0.3f);

            // Apply gold tint to daily challenge button to make it visually distinct
            if (theme != null && _dailyChallengeButton != null)
            {
                var colors = _dailyChallengeButton.colors;
                colors.normalColor = theme.AccentGold;
                colors.highlightedColor = new Color(
                    Mathf.Min(theme.AccentGold.r * 1.15f, 1f),
                    Mathf.Min(theme.AccentGold.g * 1.15f, 1f),
                    Mathf.Min(theme.AccentGold.b * 1.15f, 1f),
                    theme.AccentGold.a);
                colors.pressedColor = theme.NeutralLight;
                colors.disabledColor = new Color(theme.AccentGold.r, theme.AccentGold.g, theme.AccentGold.b, 0.5f);
                _dailyChallengeButton.colors = colors;

                // Dark text for readability on gold background
                var label = _dailyChallengeButton.GetComponentInChildren<TextMeshProUGUI>();
                if (label != null) label.color = theme.BackgroundTop;
            }
        }

        private Button BuildMenuButton(Transform parent, string label, Action onClick, float delay, bool interactable = true)
        {
            var holderGo = new GameObject($"Holder_{label}", typeof(RectTransform));
            holderGo.transform.SetParent(parent, false);

            var buttonGo = new GameObject($"Button_{label}", typeof(RectTransform), typeof(Image), typeof(Button));
            buttonGo.transform.SetParent(holderGo.transform, false);
            var buttonRt = (RectTransform)buttonGo.transform;
            StretchFull(buttonRt);

            var button = buttonGo.GetComponent<Button>();
            button.interactable = interactable;
            var text = UIText(buttonGo.transform, label, 28, TMPro.FontStyles.Normal);
            if (theme != null) theme.ApplyButton(button, buttonGo.GetComponent<Image>());
            if (theme != null && theme.BodyFont != null) text.font = theme.BodyFont;
            var rt = text.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;

            // Initial transition state (offscreen and invisible)
            buttonRt.anchoredPosition = new Vector2(300f, 0f);
            var canvasGroup = buttonGo.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 0f;

            // Slide in + Fade in
            Tween.UIAnchoredPosition(buttonRt, endValue: Vector2.zero, duration: 0.5f, ease: Ease.OutBack, startDelay: delay);
            Tween.Alpha(canvasGroup, endValue: 1f, duration: 0.4f, startDelay: delay);

            if (onClick != null)
            {
                button.onClick.AddListener(() =>
                {
                    AppRoot.Instance.Haptics?.VibrateClick();
                    _audio?.PlayOneShot(clickSound);
                    onClick();
                });
            }
            return button;
        }

        private void BuildCoinDisplay()
        {
            var coinGo = new GameObject("MainMenuCoinDisplay", typeof(RectTransform));
            coinGo.transform.SetParent(canvasRoot, false);
            var coinRt = (RectTransform)coinGo.transform;
            coinRt.anchorMin = new Vector2(0.6f, 0.88f);
            coinRt.anchorMax = new Vector2(0.95f, 0.96f);
            coinRt.offsetMin = coinRt.offsetMax = Vector2.zero;

            _coinText = coinGo.AddComponent<TextMeshProUGUI>();
            _coinText.alignment = TextAlignmentOptions.Right;
            _coinText.fontSize = 24;
            _coinText.fontStyle = FontStyles.Bold;
            if (theme != null)
            {
                _coinText.color = theme.NeutralLight;
                if (theme.BodyFont != null) _coinText.font = theme.BodyFont;
            }
            else
            {
                _coinText.color = new Color32(0xF5, 0xEC, 0xD9, 0xFF);
            }

            UpdateCoinDisplay();
        }

        private void UpdateCoinDisplay()
        {
            if (_coinText != null)
            {
                var app = AppRoot.Instance;
                var balance = app.Currency.Balance;
                _coinText.text = $"● {balance}";
                if (theme != null) _coinText.color = theme.CoinTextColor;
            }
        }

        private void BuildVersionText()
        {
            var versionGo = new GameObject("MainMenuVersion", typeof(RectTransform));
            versionGo.transform.SetParent(canvasRoot, false);
            var versionRt = (RectTransform)versionGo.transform;
            versionRt.anchorMin = new Vector2(0.1f, 0.02f);
            versionRt.anchorMax = new Vector2(0.9f, 0.08f);
            versionRt.offsetMin = versionRt.offsetMax = Vector2.zero;

            var versionText = versionGo.AddComponent<TextMeshProUGUI>();
            versionText.alignment = TextAlignmentOptions.Center;
            versionText.fontSize = 16;
            versionText.text = $"v{Application.version}";
            if (theme != null)
            {
                versionText.color = theme.NeutralLight;
                if (theme.BodyFont != null) versionText.font = theme.BodyFont;
            }
            else
            {
                versionText.color = new Color32(0xF5, 0xEC, 0xD9, 0x80);
            }
        }

        private void BuildStreakBadge()
        {
            var app = AppRoot.Instance;
            var streakDay = app.Retention.CurrentStreakDay;

            var streakGo = new GameObject("StreakBadge", typeof(RectTransform));
            streakGo.transform.SetParent(canvasRoot, false);
            var streakRt = (RectTransform)streakGo.transform;
            streakRt.anchorMin = new Vector2(0.05f, 0.88f);
            streakRt.anchorMax = new Vector2(0.4f, 0.96f);
            streakRt.offsetMin = streakRt.offsetMax = Vector2.zero;

            _streakText = streakGo.AddComponent<TextMeshProUGUI>();
            _streakText.alignment = TextAlignmentOptions.Left;
            _streakText.fontSize = 22;
            _streakText.fontStyle = FontStyles.Bold;
            if (theme != null)
            {
                _streakText.color = theme.AccentGold;
                if (theme.BodyFont != null) _streakText.font = theme.BodyFont;
            }

            // Only show streak badge when streak > 1 (showing "Day 1" isn't meaningful)
            if (streakDay > 1)
            {
                _streakText.text = $"\U0001F525 Day {streakDay}";
            }
            else
            {
                streakGo.SetActive(false);
            }
        }

        private void OnPlayClicked()
        {
            _title.gameObject.SetActive(false);
            _buttonPanel.gameObject.SetActive(false);
            if (_coinText != null) _coinText.gameObject.SetActive(false);
            if (_streakText != null) _streakText.transform.parent.gameObject.SetActive(false);
            _levelSelectScreen.gameObject.SetActive(true);
            _levelSelectScreen.Refresh();
        }

        private void OnSettingsClicked()
        {
            _title.gameObject.SetActive(false);
            _buttonPanel.gameObject.SetActive(false);
            if (_coinText != null) _coinText.gameObject.SetActive(false);
            if (_streakText != null) _streakText.transform.parent.gameObject.SetActive(false);
            _settingsScreen.gameObject.SetActive(true);
            _settingsScreen.Refresh();
        }

        private void OnStoreClicked()
        {
            _title.gameObject.SetActive(false);
            _buttonPanel.gameObject.SetActive(false);
            if (_coinText != null) _coinText.gameObject.SetActive(false);
            if (_streakText != null) _streakText.transform.parent.gameObject.SetActive(false);
            _storeScreen.gameObject.SetActive(true);
            _storeScreen.Refresh();
        }

        private void OnPanelClosed()
        {
            _settingsScreen.gameObject.SetActive(false);
            _levelSelectScreen.gameObject.SetActive(false);
            _storeScreen.gameObject.SetActive(false);
            _title.gameObject.SetActive(true);
            _buttonPanel.gameObject.SetActive(true);
            if (_coinText != null) _coinText.gameObject.SetActive(true);
            if (_streakText != null) _streakText.transform.parent.gameObject.SetActive(true);
            UpdateCoinDisplay();
            RefreshDailyChallengeButton();
        }

        private void OnDailyChallengeClicked()
        {
            if (_catalog == null) return;
            var app = AppRoot.Instance;
            app.IsDailyChallenge = true;
            app.SelectedLevelIndex = app.DailyChallenge.GetTodaysCatalogIndex(_catalog.Levels.Count);
            _ = ScreenNavigator.Instance.LoadScene("Gameplay", TransitionType.SlideLeft);
        }

        private void RefreshDailyChallengeButton()
        {
            if (_dailyChallengeButton == null) return;
            var completed = AppRoot.Instance.DailyChallenge.IsCompletedToday;
            _dailyChallengeButton.interactable = !completed;
            var label = _dailyChallengeButton.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null)
            {
                label.text = completed ? "\u2713 COMPLETED" : "DAILY CHALLENGE";
            }
        }

        private void OnFreeCoinsAdClicked(Button btn)
        {
            if (btn == null) return;
            var app = AppRoot.Instance;
            btn.interactable = false;
            var label = btn.GetComponentInChildren<TextMeshProUGUI>();
            var originalText = label != null ? label.text : "FREE COINS (AD)";
            if (label != null) label.text = "WATCHING AD...";

            app.AdService.ShowRewardedAd(success =>
            {
                if (btn != null) btn.interactable = true;
                if (label != null) label.text = originalText;
                if (success)
                {
                    app.Currency.Add(app.Config.RewardedAdCoinReward);
                }
            });
        }

        private void ShowDailyRewardModal(AppRoot app)
        {
            var modalGo = new GameObject("DailyRewardModal", typeof(RectTransform));
            modalGo.transform.SetParent(canvasRoot, false);
            StretchFull((RectTransform)modalGo.transform);
            _dailyRewardModal = modalGo.AddComponent<DailyRewardModal>();
            _dailyRewardModal.Init(theme, clickSound, app.Retention, app.Currency, app.Config, () =>
            {
                UpdateCoinDisplay();
                // Refresh streak badge after claiming
                if (_streakText != null)
                {
                    var streakDay = app.Retention.CurrentStreakDay;
                    if (streakDay > 1)
                    {
                        _streakText.text = $"\U0001F525 Day {streakDay}";
                        _streakText.transform.parent.gameObject.SetActive(true);
                    }
                }
            });
        }

        internal static TextMeshProUGUI UIText(Transform parent, string text, float fontSize, TMPro.FontStyles style)
        {
            var go = new GameObject("Text", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.fontStyle = style;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.black;
            return tmp;
        }

        internal static void StretchFull(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }
    }
}
