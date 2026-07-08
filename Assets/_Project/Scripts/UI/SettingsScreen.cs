using System;
using BadMovieClues.Economy;
using BadMovieClues.Services;
using PrimeTween;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BadMovieClues.UI
{
    /// <summary>
    /// Settings panel: Reduced Effects toggle, mute toggle, Reset Progress,
    /// Restore Purchases (stub - IPurchaseService doesn't exist until M12),
    /// Credits, version number. Built procedurally by MainMenuScreen, same
    /// pattern as the rest of the UI. "Reset Progress" currently resets the
    /// coin balance only - the only persisted state that exists pre-M11;
    /// once ProgressService (M11) exists this should also clear solved/
    /// unlocked level state.
    /// </summary>
    public class SettingsScreen : MonoBehaviour
    {
        private UITheme _theme;
        private AudioClip _clickSound;
        private IUserSettings _settings;
        private ICurrencyService _currency;
        private GameConfig _config;
        private Action _onClose;

        private Toggle _reducedEffectsToggle;
        private Toggle _muteToggle;
        private RectTransform _creditsPanel;

        public void Init(UITheme theme, AudioClip clickSound, IUserSettings settings,
            ICurrencyService currency, GameConfig config, Action onClose)
        {
            _theme = theme;
            _clickSound = clickSound;
            _settings = settings;
            _currency = currency;
            _config = config;
            _onClose = onClose;
            Build();
        }

        public void Refresh()
        {
            _reducedEffectsToggle.SetIsOnWithoutNotify(_settings.ReducedEffects);
            _muteToggle.SetIsOnWithoutNotify(_settings.AudioEnabled);
        }

        private void Build()
        {
            var panelGo = new GameObject("Panel", typeof(RectTransform), typeof(VerticalLayoutGroup));
            panelGo.transform.SetParent(transform, false);
            var panelRt = (RectTransform)panelGo.transform;
            panelRt.anchorMin = new Vector2(0.15f, 0.2f);
            panelRt.anchorMax = new Vector2(0.85f, 0.8f);
            panelRt.offsetMin = panelRt.offsetMax = Vector2.zero;

            var layout = panelGo.GetComponent<VerticalLayoutGroup>();
            layout.spacing = 12;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            _reducedEffectsToggle = BuildToggleRow(panelRt, "Reduced Effects", _settings.ReducedEffects,
                v => _settings.ReducedEffects = v);
            _muteToggle = BuildToggleRow(panelRt, "Mute Audio", !_settings.AudioEnabled,
                v => _settings.AudioEnabled = !v);

            BuildButton(panelRt, "Reset Progress", OnResetProgress);
            BuildButton(panelRt, "Restore Purchases (coming soon)", null, interactable: false);
            BuildButton(panelRt, "Credits", OnCreditsClicked);

            var version = MainMenuScreen.UIText(panelRt, $"v{Application.version}", 18, FontStyles.Normal);
            version.rectTransform.sizeDelta = new Vector2(0, 40);
            if (_theme != null) version.color = _theme.NeutralLight;

            BuildButton(panelRt, "< Back", OnBackClicked);

            BuildCreditsPanel();
        }

        private Toggle BuildToggleRow(Transform parent, string label, bool initialValue, Action<bool> onChanged)
        {
            var rowGo = new GameObject($"Toggle_{label}", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            rowGo.transform.SetParent(parent, false);
            var rowLayout = rowGo.GetComponent<HorizontalLayoutGroup>();
            rowLayout.spacing = 12;
            rowLayout.childAlignment = TextAnchor.MiddleLeft;
            rowLayout.childControlHeight = true;
            ((RectTransform)rowGo.transform).sizeDelta = new Vector2(0, 60);

            var toggleGo = new GameObject("Toggle", typeof(RectTransform), typeof(Image), typeof(Toggle));
            toggleGo.transform.SetParent(rowGo.transform, false);
            ((RectTransform)toggleGo.transform).sizeDelta = new Vector2(50, 50);
            var bgImage = toggleGo.GetComponent<Image>();
            bgImage.color = Color.white;

            var checkGo = new GameObject("Checkmark", typeof(RectTransform), typeof(Image));
            checkGo.transform.SetParent(toggleGo.transform, false);
            MainMenuScreen.StretchFull((RectTransform)checkGo.transform);
            var checkImage = checkGo.GetComponent<Image>();
            checkImage.color = _theme != null ? _theme.AccentGold : Color.green;

            var toggle = toggleGo.GetComponent<Toggle>();
            toggle.targetGraphic = bgImage;
            toggle.graphic = checkImage;
            toggle.isOn = initialValue;
            toggle.onValueChanged.AddListener(v =>
            {
                PlayClick();
                onChanged(v);
            });

            var labelText = MainMenuScreen.UIText(rowGo.transform, label, 24, FontStyles.Normal);
            labelText.alignment = TextAlignmentOptions.MidlineLeft;
            if (_theme != null) labelText.color = _theme.NeutralLight;

            return toggle;
        }

        private void BuildButton(Transform parent, string label, Action onClick, bool interactable = true)
        {
            var buttonGo = new GameObject($"Button_{label}", typeof(RectTransform), typeof(Image), typeof(Button));
            buttonGo.transform.SetParent(parent, false);
            ((RectTransform)buttonGo.transform).sizeDelta = new Vector2(0, 56);
            var button = buttonGo.GetComponent<Button>();
            button.interactable = interactable;
            if (_theme != null) _theme.ApplyButton(button, buttonGo.GetComponent<Image>());

            var text = MainMenuScreen.UIText(buttonGo.transform, label, 22, FontStyles.Normal);
            MainMenuScreen.StretchFull(text.rectTransform);

            if (onClick != null)
            {
                button.onClick.AddListener(() =>
                {
                    PlayClick();
                    Tween.Scale(buttonGo.transform, endValue: 0.92f, duration: 0.08f, cycles: 2, cycleMode: CycleMode.Yoyo);
                    onClick();
                });
            }
        }

        private void BuildCreditsPanel()
        {
            var go = new GameObject("CreditsPanel", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(transform, false);
            _creditsPanel = (RectTransform)go.transform;
            _creditsPanel.anchorMin = new Vector2(0.15f, 0.3f);
            _creditsPanel.anchorMax = new Vector2(0.85f, 0.7f);
            _creditsPanel.offsetMin = _creditsPanel.offsetMax = Vector2.zero;
            if (_theme != null) go.GetComponent<Image>().color = _theme.BackgroundBottom;

            var text = MainMenuScreen.UIText(_creditsPanel, "Bad Movie Clues\n\nBad descriptions & bad art by the owner.\nBuilt with Unity, PrimeTween, and the Kenney UI Pack.",
                20, FontStyles.Normal);
            var textRt = text.rectTransform;
            textRt.anchorMin = new Vector2(0.05f, 0.2f);
            textRt.anchorMax = new Vector2(0.95f, 0.95f);
            textRt.offsetMin = textRt.offsetMax = Vector2.zero;
            if (_theme != null) text.color = _theme.NeutralLight;

            var closeGo = new GameObject("Close", typeof(RectTransform), typeof(Image), typeof(Button));
            closeGo.transform.SetParent(_creditsPanel, false);
            var closeRt = (RectTransform)closeGo.transform;
            closeRt.anchorMin = new Vector2(0.3f, 0.03f);
            closeRt.anchorMax = new Vector2(0.7f, 0.15f);
            closeRt.offsetMin = closeRt.offsetMax = Vector2.zero;
            var closeButton = closeGo.GetComponent<Button>();
            if (_theme != null) _theme.ApplyButton(closeButton, closeGo.GetComponent<Image>());
            var closeText = MainMenuScreen.UIText(closeGo.transform, "Close", 20, FontStyles.Normal);
            MainMenuScreen.StretchFull(closeText.rectTransform);
            closeButton.onClick.AddListener(() =>
            {
                PlayClick();
                _creditsPanel.gameObject.SetActive(false);
            });

            _creditsPanel.gameObject.SetActive(false);
        }

        private void OnResetProgress() => _currency.Reset(_config.StartingBalance);

        private void OnCreditsClicked() => _creditsPanel.gameObject.SetActive(true);

        private void OnBackClicked() => _onClose();

        private void PlayClick() => AppRoot.Instance.AudioService.PlayOneShot(_clickSound);
    }
}
