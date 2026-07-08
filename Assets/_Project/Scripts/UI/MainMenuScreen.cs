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
    /// buttons, and the SettingsScreen panel toggled in place of the button
    /// row. Built entirely in code at runtime (RectTransform anchors, no
    /// scene-authored children) - same procedural-UI pattern GameHud already
    /// uses for the keyboard/blanks row, chosen for the same reason: no way
    /// to drive the Editor GUI from this environment to hand-place UI, and
    /// it keeps the scene file itself trivial to reason about.
    /// </summary>
    public class MainMenuScreen : MonoBehaviour
    {
        [SerializeField] private UITheme theme;
        [SerializeField] private AudioClip clickSound;
        [SerializeField] private RectTransform canvasRoot;

        private IAudioService _audio;
        private RectTransform _buttonPanel;
        private SettingsScreen _settingsScreen;

        private void Start()
        {
            try
            {
                var app = AppRoot.Instance;
                _audio = app.AudioService;

                BuildTitle();
                BuildButtonPanel();

                var settingsGo = new GameObject("SettingsScreen", typeof(RectTransform));
                settingsGo.transform.SetParent(canvasRoot, false);
                StretchFull((RectTransform)settingsGo.transform);
                _settingsScreen = settingsGo.AddComponent<SettingsScreen>();
                _settingsScreen.Init(theme, clickSound, app.Settings, app.Currency, app.Config, OnSettingsClosed);
                _settingsScreen.gameObject.SetActive(false);
            }
            catch (Exception e)
            {
                Debug.LogError($"[MainMenuScreen] Exception in Start(): {e}");
            }
        }

        private void BuildTitle()
        {
            var title = UIText(canvasRoot, "Bad Movie Clues", 54, TMPro.FontStyles.Bold);
            var rt = title.rectTransform;
            rt.anchorMin = new Vector2(0.1f, 0.72f);
            rt.anchorMax = new Vector2(0.9f, 0.85f);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            if (theme != null) title.color = theme.NeutralLight;
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

            BuildMenuButton(_buttonPanel, "PLAY", OnPlayClicked);
            BuildMenuButton(_buttonPanel, "STORE (coming soon)", null, interactable: false);
            BuildMenuButton(_buttonPanel, "SETTINGS", OnSettingsClicked);
        }

        private Button BuildMenuButton(Transform parent, string label, Action onClick, bool interactable = true)
        {
            var buttonGo = new GameObject($"Button_{label}", typeof(RectTransform), typeof(Image), typeof(Button));
            buttonGo.transform.SetParent(parent, false);
            var button = buttonGo.GetComponent<Button>();
            button.interactable = interactable;
            if (theme != null) theme.ApplyButton(button, buttonGo.GetComponent<Image>());

            var text = UIText(buttonGo.transform, label, 28, TMPro.FontStyles.Normal);
            var rt = text.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;

            if (onClick != null)
            {
                button.onClick.AddListener(() =>
                {
                    _audio?.PlayOneShot(clickSound);
                    Tween.Scale(buttonGo.transform, endValue: 0.92f, duration: 0.08f, cycles: 2, cycleMode: CycleMode.Yoyo);
                    onClick();
                });
            }
            return button;
        }

        private void OnPlayClicked() => _ = ScreenNavigator.Instance.LoadScene("Gameplay");

        private void OnSettingsClicked()
        {
            _buttonPanel.gameObject.SetActive(false);
            _settingsScreen.gameObject.SetActive(true);
            _settingsScreen.Refresh();
        }

        private void OnSettingsClosed()
        {
            _settingsScreen.gameObject.SetActive(false);
            _buttonPanel.gameObject.SetActive(true);
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
