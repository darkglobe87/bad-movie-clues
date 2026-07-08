using System;
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
    /// Settings panel: Sound Effects, Vibration Haptics, and Reduced Effects toggles,
    /// Reset Progress, Credits, version number. Built procedurally with themed card panels.
    /// </summary>
    public class SettingsScreen : MonoBehaviour
    {
        private UITheme _theme;
        private AudioClip _clickSound;
        private IUserSettings _settings;
        private ICurrencyService _currency;
        private IProgressService _progress;
        private GameConfig _config;
        private Action _onClose;

        private Toggle _reducedEffectsToggle;
        private Toggle _muteToggle;
        private Toggle _hapticsToggle;
        private RectTransform _creditsPanel;
        private RectTransform _resetConfirmPanel;

        public void Init(UITheme theme, AudioClip clickSound, IUserSettings settings,
            ICurrencyService currency, IProgressService progress, GameConfig config, Action onClose)
        {
            _theme = theme;
            _clickSound = clickSound;
            _settings = settings;
            _currency = currency;
            _progress = progress;
            _config = config;
            _onClose = onClose;
            Build();
        }

        public void Refresh()
        {
            _reducedEffectsToggle.SetIsOnWithoutNotify(_settings.ReducedEffects);
            _muteToggle.SetIsOnWithoutNotify(_settings.AudioEnabled);
            _hapticsToggle.SetIsOnWithoutNotify(_settings.HapticsEnabled);
        }

        private void Build()
        {
            var panelGo = new GameObject("Panel", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup));
            panelGo.transform.SetParent(transform, false);
            var panelRt = (RectTransform)panelGo.transform;
            panelRt.anchorMin = new Vector2(0.1f, 0.15f);
            panelRt.anchorMax = new Vector2(0.9f, 0.85f);
            panelRt.offsetMin = panelRt.offsetMax = Vector2.zero;

            var panelImage = panelGo.GetComponent<Image>();
            if (_theme != null)
            {
                _theme.ApplyPanel(panelImage);
            }
            else
            {
                panelImage.color = new Color32(0x35, 0x20, 0x4E, 0xFF);
            }

            var layout = panelGo.GetComponent<VerticalLayoutGroup>();
            layout.spacing = 10;
            layout.padding = new RectOffset(20, 20, 20, 20);
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            BuildSectionHeader(panelRt, "AUDIO & EFFECTS");

            _muteToggle = BuildToggleRow(panelRt, "Sound Effects", _settings.AudioEnabled,
                v => _settings.AudioEnabled = v);
            _hapticsToggle = BuildToggleRow(panelRt, "Vibration Haptics", _settings.HapticsEnabled,
                v => _settings.HapticsEnabled = v);
            _reducedEffectsToggle = BuildToggleRow(panelRt, "Reduced Effects", _settings.ReducedEffects,
                v => _settings.ReducedEffects = v);

            BuildSectionHeader(panelRt, "SYSTEM DATA");

            BuildButton(panelRt, "Reset Progress", OnResetProgressClicked);
            BuildButton(panelRt, "Restore Purchases (coming soon)", null, interactable: false);
            BuildButton(panelRt, "Credits", OnCreditsClicked);

            var version = MainMenuScreen.UIText(panelRt, $"v{Application.version}", 16, FontStyles.Normal);
            AddFixedHeight(version.gameObject, 24);
            if (_theme != null)
            {
                version.color = _theme.NeutralLight;
                if (_theme.BodyFont != null) version.font = _theme.BodyFont;
            }

            if (_theme != null)
            {
                _theme.CreateSeparator(panelRt, 2f);
            }

            BuildButton(panelRt, "< Back", OnBackClicked);

            BuildCreditsPanel();
            BuildResetConfirmPanel();
        }

        private static LayoutElement AddFixedHeight(GameObject go, float height)
        {
            var le = go.AddComponent<LayoutElement>();
            le.preferredHeight = height;
            le.minHeight = height;
            return le;
        }

        private void BuildSectionHeader(Transform parent, string title)
        {
            var go = new GameObject($"Header_{title}", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            AddFixedHeight(go, 28);

            var text = MainMenuScreen.UIText(go.transform, title, 14, FontStyles.Bold);
            if (_theme != null && _theme.HeadingFont != null) text.font = _theme.HeadingFont;
            MainMenuScreen.StretchFull(text.rectTransform);
            text.alignment = TextAlignmentOptions.MidlineLeft;
            
            if (_theme != null) text.color = _theme.AccentGold;
            else text.color = Color.yellow;
        }

        private Toggle BuildToggleRow(Transform parent, string label, bool initialValue, Action<bool> onChanged)
        {
            var rowGo = new GameObject($"Toggle_{label}", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            rowGo.transform.SetParent(parent, false);
            var rowLayout = rowGo.GetComponent<HorizontalLayoutGroup>();
            rowLayout.spacing = 12;
            rowLayout.childAlignment = TextAnchor.MiddleLeft;
            rowLayout.childControlWidth = true;
            rowLayout.childControlHeight = true;
            rowLayout.childForceExpandWidth = false;
            rowLayout.childForceExpandHeight = true;
            AddFixedHeight(rowGo, 48);

            var toggleGo = new GameObject("Toggle", typeof(RectTransform), typeof(Image), typeof(Toggle));
            toggleGo.transform.SetParent(rowGo.transform, false);
            var toggleLe = toggleGo.AddComponent<LayoutElement>();
            toggleLe.preferredWidth = 44;
            toggleLe.preferredHeight = 44;
            toggleLe.minWidth = 44;
            toggleLe.minHeight = 44;
            
            var bgImage = toggleGo.GetComponent<Image>();
            if (_theme != null && _theme.ToggleBackgroundSprite != null)
            {
                bgImage.sprite = _theme.ToggleBackgroundSprite;
                bgImage.type = Image.Type.Sliced;
            }
            bgImage.color = Color.white;

            var checkGo = new GameObject("Checkmark", typeof(RectTransform), typeof(Image));
            checkGo.transform.SetParent(toggleGo.transform, false);
            MainMenuScreen.StretchFull((RectTransform)checkGo.transform);
            var checkImage = checkGo.GetComponent<Image>();
            if (_theme != null && _theme.ToggleCheckmarkSprite != null)
            {
                checkImage.sprite = _theme.ToggleCheckmarkSprite;
            }
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

            var labelGo = new GameObject("Label", typeof(RectTransform));
            labelGo.transform.SetParent(rowGo.transform, false);
            labelGo.AddComponent<LayoutElement>().flexibleWidth = 1;
            var labelText = MainMenuScreen.UIText(labelGo.transform, label, 20, FontStyles.Normal);
            if (_theme != null && _theme.BodyFont != null) labelText.font = _theme.BodyFont;
            MainMenuScreen.StretchFull(labelText.rectTransform);
            labelText.alignment = TextAlignmentOptions.MidlineLeft;
            if (_theme != null) labelText.color = _theme.NeutralLight;

            return toggle;
        }

        private void BuildButton(Transform parent, string label, Action onClick, bool interactable = true)
        {
            var buttonGo = new GameObject($"Button_{label}", typeof(RectTransform), typeof(Image), typeof(Button));
            buttonGo.transform.SetParent(parent, false);
            AddFixedHeight(buttonGo, 48);
            var button = buttonGo.GetComponent<Button>();
            button.interactable = interactable;
            if (_theme != null) _theme.ApplyButton(button, buttonGo.GetComponent<Image>());

            var text = MainMenuScreen.UIText(buttonGo.transform, label, 20, FontStyles.Normal);
            if (_theme != null && _theme.BodyFont != null) text.font = _theme.BodyFont;
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
            
            var img = go.GetComponent<Image>();
            if (_theme != null) _theme.ApplyPanel(img);
            else img.color = new Color32(0x23, 0x14, 0x34, 0xFF);

            var text = MainMenuScreen.UIText(_creditsPanel, "Bad Movie Clues\n\nBad descriptions & bad art by the owner.\nBuilt with Unity, PrimeTween, and the Kenney UI Pack.",
                18, FontStyles.Normal);
            if (_theme != null && _theme.BodyFont != null) text.font = _theme.BodyFont;
            var textRt = text.rectTransform;
            textRt.anchorMin = new Vector2(0.05f, 0.25f);
            textRt.anchorMax = new Vector2(0.95f, 0.95f);
            textRt.offsetMin = textRt.offsetMax = Vector2.zero;
            if (_theme != null) text.color = _theme.NeutralLight;

            var closeGo = new GameObject("Close", typeof(RectTransform), typeof(Image), typeof(Button));
            closeGo.transform.SetParent(_creditsPanel, false);
            var closeRt = (RectTransform)closeGo.transform;
            closeRt.anchorMin = new Vector2(0.35f, 0.05f);
            closeRt.anchorMax = new Vector2(0.65f, 0.2f);
            closeRt.offsetMin = closeRt.offsetMax = Vector2.zero;
            var closeButton = closeGo.GetComponent<Button>();
            if (_theme != null) _theme.ApplyButton(closeButton, closeGo.GetComponent<Image>());
            var closeText = MainMenuScreen.UIText(closeGo.transform, "Close", 18, FontStyles.Normal);
            if (_theme != null && _theme.BodyFont != null) closeText.font = _theme.BodyFont;
            MainMenuScreen.StretchFull(closeText.rectTransform);
            closeButton.onClick.AddListener(() =>
            {
                PlayClick();
                _creditsPanel.gameObject.SetActive(false);
            });

            _creditsPanel.gameObject.SetActive(false);
        }

        private void BuildResetConfirmPanel()
        {
            var go = new GameObject("ResetConfirmPanel", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(transform, false);
            _resetConfirmPanel = (RectTransform)go.transform;
            _resetConfirmPanel.anchorMin = new Vector2(0.15f, 0.35f);
            _resetConfirmPanel.anchorMax = new Vector2(0.85f, 0.65f);
            _resetConfirmPanel.offsetMin = _resetConfirmPanel.offsetMax = Vector2.zero;
            
            var img = go.GetComponent<Image>();
            if (_theme != null) _theme.ApplyPanel(img);
            else img.color = new Color32(0x23, 0x14, 0x34, 0xFF);

            var text = MainMenuScreen.UIText(_resetConfirmPanel,
                "Reset all progress?\nThis clears your coin balance\nand every solved/unlocked level.",
                18, FontStyles.Normal);
            if (_theme != null && _theme.BodyFont != null) text.font = _theme.BodyFont;
            var textRt = text.rectTransform;
            textRt.anchorMin = new Vector2(0.05f, 0.45f);
            textRt.anchorMax = new Vector2(0.95f, 0.95f);
            textRt.offsetMin = textRt.offsetMax = Vector2.zero;
            if (_theme != null) text.color = _theme.NeutralLight;

            var buttonRowGo = new GameObject("Buttons", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            buttonRowGo.transform.SetParent(_resetConfirmPanel, false);
            var buttonRowRt = (RectTransform)buttonRowGo.transform;
            buttonRowRt.anchorMin = new Vector2(0.15f, 0.08f);
            buttonRowRt.anchorMax = new Vector2(0.85f, 0.32f);
            buttonRowRt.offsetMin = buttonRowRt.offsetMax = Vector2.zero;
            var buttonRowLayout = buttonRowGo.GetComponent<HorizontalLayoutGroup>();
            buttonRowLayout.spacing = 12;
            buttonRowLayout.childControlWidth = true;
            buttonRowLayout.childControlHeight = true;
            buttonRowLayout.childForceExpandWidth = true;
            buttonRowLayout.childForceExpandHeight = true;

            BuildButton(buttonRowRt, "Cancel", () => _resetConfirmPanel.gameObject.SetActive(false));
            BuildButton(buttonRowRt, "Reset", OnResetConfirmed);

            _resetConfirmPanel.gameObject.SetActive(false);
        }

        private void OnResetProgressClicked() => _resetConfirmPanel.gameObject.SetActive(true);

        private void OnResetConfirmed()
        {
            _resetConfirmPanel.gameObject.SetActive(false);
            _currency.Reset(_config.StartingBalance);
            _progress.Reset();
        }

        private void OnCreditsClicked() => _creditsPanel.gameObject.SetActive(true);

        private void OnBackClicked() => _onClose();

        private void PlayClick()
        {
            AppRoot.Instance.Haptics?.VibrateClick();
            AppRoot.Instance.AudioService.PlayOneShot(_clickSound);
        }
    }
}
