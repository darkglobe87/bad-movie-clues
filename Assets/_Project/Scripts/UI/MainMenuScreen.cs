using System;
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

        private async void Start()
        {
            try
            {
                var app = AppRoot.Instance;
                _audio = app.AudioService;

                BuildTitle();
                BuildButtonPanel();
                BuildCoinDisplay();
                BuildVersionText();

                app.Currency.OnBalanceChanged += OnBalanceChanged;

                var catalog = await app.ContentProvider.LoadCatalogAsync();

                var settingsGo = new GameObject("SettingsScreen", typeof(RectTransform));
                settingsGo.transform.SetParent(canvasRoot, false);
                StretchFull((RectTransform)settingsGo.transform);
                _settingsScreen = settingsGo.AddComponent<SettingsScreen>();
                _settingsScreen.Init(theme, clickSound, app.Settings, app.Currency, app.Progress, app.Config, OnPanelClosed);
                _settingsScreen.gameObject.SetActive(false);

                var levelSelectGo = new GameObject("LevelSelectScreen", typeof(RectTransform));
                levelSelectGo.transform.SetParent(canvasRoot, false);
                StretchFull((RectTransform)levelSelectGo.transform);
                _levelSelectScreen = levelSelectGo.AddComponent<LevelSelectScreen>();
                _levelSelectScreen.Init(theme, clickSound, app.Progress, catalog, OnPanelClosed);
                _levelSelectScreen.gameObject.SetActive(false);

                var storeGo = new GameObject("StoreScreen", typeof(RectTransform));
                storeGo.transform.SetParent(canvasRoot, false);
                StretchFull((RectTransform)storeGo.transform);
                _storeScreen = storeGo.AddComponent<StoreScreen>();
                _storeScreen.Init(theme, clickSound, app.Purchases, app.Currency, OnPanelClosed);
                _storeScreen.gameObject.SetActive(false);
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
            _buttonPanel.anchorMin = new Vector2(0.25f, 0.35f);
            _buttonPanel.anchorMax = new Vector2(0.75f, 0.65f);
            _buttonPanel.offsetMin = _buttonPanel.offsetMax = Vector2.zero;

            var layout = panelGo.GetComponent<VerticalLayoutGroup>();
            layout.spacing = 16;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;

            // Slide-in staggered animations
            BuildMenuButton(_buttonPanel, "PLAY", OnPlayClicked, 0.1f);
            BuildMenuButton(_buttonPanel, "STORE", OnStoreClicked, 0.2f);
            BuildMenuButton(_buttonPanel, "SETTINGS", OnSettingsClicked, 0.3f);
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
            if (theme != null) theme.ApplyButton(button, buttonGo.GetComponent<Image>());

            var text = UIText(buttonGo.transform, label, 28, TMPro.FontStyles.Normal);
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
                    Tween.Scale(buttonGo.transform, endValue: 0.92f, duration: 0.08f, cycles: 2, cycleMode: CycleMode.Yoyo);
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

        private void OnPlayClicked()
        {
            _title.gameObject.SetActive(false);
            _buttonPanel.gameObject.SetActive(false);
            if (_coinText != null) _coinText.gameObject.SetActive(false);
            _levelSelectScreen.gameObject.SetActive(true);
            _levelSelectScreen.Refresh();
        }

        private void OnSettingsClicked()
        {
            _title.gameObject.SetActive(false);
            _buttonPanel.gameObject.SetActive(false);
            if (_coinText != null) _coinText.gameObject.SetActive(false);
            _settingsScreen.gameObject.SetActive(true);
            _settingsScreen.Refresh();
        }

        private void OnStoreClicked()
        {
            _title.gameObject.SetActive(false);
            _buttonPanel.gameObject.SetActive(false);
            if (_coinText != null) _coinText.gameObject.SetActive(false);
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
            UpdateCoinDisplay();
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
