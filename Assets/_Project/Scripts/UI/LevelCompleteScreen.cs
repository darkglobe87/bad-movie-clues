using System;
using BadMovieClues.Services;
using PrimeTween;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BadMovieClues.UI
{
    /// <summary>
    /// Win/lose overlay hooked to GameController's Won/Lost events via
    /// GameHud. Built procedurally, same pattern as every other screen in
    /// this project. Win: dim + card scale/rotate reveal with overshoot,
    /// confetti burst, star rating, coins-earned, Next button. Lose:
    /// gentler card reveal, reveals the answer, no confetti, Retry/Menu.
    ///
    /// The win/lose "sting" reuses the existing UI click sound
    /// (click.ogg) rather than a dedicated asset - no distinct win/lose
    /// SFX has been sourced yet, and a mismatched clip would be worse than
    /// none; this is a clearly-labeled placeholder, not a real sting.
    /// </summary>
    public class LevelCompleteScreen : MonoBehaviour
    {
        private UITheme _theme;
        private AudioClip _stingSound;
        private IAudioService _audio;

        private RectTransform _dimBackground;
        private RectTransform _card;
        private TextMeshProUGUI _titleText;
        private TextMeshProUGUI _starsText;
        private TextMeshProUGUI _coinsText;
        private RectTransform _buttonRow;

        public void Init(UITheme theme, AudioClip stingSound, IAudioService audio)
        {
            _theme = theme;
            _stingSound = stingSound;
            _audio = audio;
            Build();
        }

        public void ShowWon(string movieTitle, int stars, int coinsEarned, Action onNext)
        {
            gameObject.SetActive(true);
            ClearButtons();

            _titleText.text = movieTitle;
            _starsText.text = new string('★', Mathf.Clamp(stars, 0, 3));
            _coinsText.text = $"+{coinsEarned} coins";

            _card.localScale = Vector3.zero;
            _card.localRotation = Quaternion.Euler(0f, 0f, -8f);
            Tween.Scale(_card, endValue: 1f, duration: 0.5f, ease: Ease.OutBack);
            Tween.Rotation(_card, endValue: Vector3.zero, duration: 0.5f, ease: Ease.OutBack);

            ConfettiBurst.Play(_dimBackground, 60);
            _audio?.PlayOneShot(_stingSound);

            BuildButton("Next", onNext);
        }

        public void ShowLost(string movieTitle, Action onRetry, Action onMenu)
        {
            gameObject.SetActive(true);
            ClearButtons();

            _titleText.text = $"So close!\nIt was:\n{movieTitle}";
            _starsText.text = "";
            _coinsText.text = "";

            _card.localScale = Vector3.zero;
            _card.localRotation = Quaternion.identity;
            Tween.Scale(_card, endValue: 1f, duration: 0.4f, ease: Ease.OutBack);

            _audio?.PlayOneShot(_stingSound);

            BuildButton("Retry", onRetry);
            BuildButton("Menu", onMenu);
        }

        private void Build()
        {
            var dimGo = new GameObject("Dim", typeof(RectTransform), typeof(Image));
            dimGo.transform.SetParent(transform, false);
            _dimBackground = (RectTransform)dimGo.transform;
            MainMenuScreen.StretchFull(_dimBackground);
            dimGo.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.7f);

            var cardGo = new GameObject("Card", typeof(RectTransform), typeof(Image));
            cardGo.transform.SetParent(transform, false);
            _card = (RectTransform)cardGo.transform;
            _card.anchorMin = new Vector2(0.1f, 0.3f);
            _card.anchorMax = new Vector2(0.9f, 0.7f);
            _card.offsetMin = _card.offsetMax = Vector2.zero;
            cardGo.GetComponent<Image>().color = _theme != null ? _theme.BackgroundBottom : new Color32(0x3D, 0x24, 0x59, 0xFF);

            _titleText = MainMenuScreen.UIText(_card, "", 30, FontStyles.Bold);
            SetRect(_titleText.rectTransform, 0.05f, 0.6f, 0.95f, 0.9f);
            if (_theme != null) _titleText.color = _theme.NeutralLight;

            _starsText = MainMenuScreen.UIText(_card, "", 40, FontStyles.Bold);
            SetRect(_starsText.rectTransform, 0.05f, 0.42f, 0.95f, 0.58f);
            if (_theme != null) _starsText.color = _theme.AccentGold;

            _coinsText = MainMenuScreen.UIText(_card, "", 24, FontStyles.Normal);
            SetRect(_coinsText.rectTransform, 0.05f, 0.28f, 0.95f, 0.42f);
            if (_theme != null) _coinsText.color = _theme.NeutralLight;

            var buttonRowGo = new GameObject("Buttons", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            buttonRowGo.transform.SetParent(_card, false);
            _buttonRow = (RectTransform)buttonRowGo.transform;
            SetRect(_buttonRow, 0.1f, 0.06f, 0.9f, 0.24f);
            var rowLayout = buttonRowGo.GetComponent<HorizontalLayoutGroup>();
            rowLayout.spacing = 12;
            rowLayout.childControlWidth = true;
            rowLayout.childControlHeight = true;
            rowLayout.childForceExpandWidth = true;
            rowLayout.childForceExpandHeight = true;
        }

        private static void SetRect(RectTransform rt, float xMin, float yMin, float xMax, float yMax)
        {
            rt.anchorMin = new Vector2(xMin, yMin);
            rt.anchorMax = new Vector2(xMax, yMax);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        private void ClearButtons()
        {
            foreach (Transform child in _buttonRow) Destroy(child.gameObject);
        }

        private void BuildButton(string label, Action onClick)
        {
            var buttonGo = new GameObject($"Button_{label}", typeof(RectTransform), typeof(Image), typeof(Button));
            buttonGo.transform.SetParent(_buttonRow, false);
            var button = buttonGo.GetComponent<Button>();
            if (_theme != null) _theme.ApplyButton(button, buttonGo.GetComponent<Image>());

            var text = MainMenuScreen.UIText(buttonGo.transform, label, 24, FontStyles.Normal);
            MainMenuScreen.StretchFull(text.rectTransform);

            button.onClick.AddListener(() =>
            {
                _audio?.PlayOneShot(_stingSound);
                onClick();
            });
        }
    }
}
