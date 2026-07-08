using System;
using BadMovieClues.Services;
using PrimeTween;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BadMovieClues.UI
{
    /// <summary>
    /// Win/lose overlay hooked to GameController's Won/Lost events via GameHud.
    /// Enhanced with card visual styling, staggered star reveals, coin count-up tweens,
    /// and a pulsing "NEW BEST!" progress badge.
    /// </summary>
    public class LevelCompleteScreen : MonoBehaviour
    {
        private UITheme _theme;
        private AudioClip _stingSound;
        private IAudioService _audio;

        private RectTransform _dimBackground;
        private RectTransform _card;
        private TextMeshProUGUI _titleText;
        private RectTransform _starsContainer;
        private TextMeshProUGUI[] _starLabels = new TextMeshProUGUI[3];
        private TextMeshProUGUI _coinsText;
        private TextMeshProUGUI _badgeText;
        private RectTransform _buttonRow;

        public void Init(UITheme theme, AudioClip stingSound, IAudioService audio)
        {
            _theme = theme;
            _stingSound = stingSound;
            _audio = audio;
            Build();
        }

        public void ShowWon(string movieTitle, int stars, int coinsEarned, bool isNewBest, Action onNext)
        {
            gameObject.SetActive(true);
            ClearButtons();

            _titleText.text = movieTitle;
            
            // Pop stars in with a stagger
            for (int i = 0; i < 3; i++)
            {
                var starLabel = _starLabels[i];
                starLabel.text = i < stars ? "★" : "☆";
                if (_theme != null)
                {
                    starLabel.color = i < stars ? _theme.AccentGold : _theme.StarEmpty;
                    if (_theme.HeadingFont != null) starLabel.font = _theme.HeadingFont;
                }
                else
                {
                    starLabel.color = i < stars ? Color.yellow : Color.gray;
                }

                starLabel.transform.localScale = Vector3.zero;
                Tween.Scale(starLabel.transform, endValue: 1f, duration: 0.4f, ease: Ease.OutBack, startDelay: 0.3f + i * 0.15f);
            }

            // Coin count-up animation
            _coinsText.text = "+0 coins";
            Tween.Custom(startValue: 0f, endValue: coinsEarned, duration: 0.8f, onValueChange: val =>
            {
                _coinsText.text = $"+{Mathf.RoundToInt(val)} coins";
            }, startDelay: 0.4f);

            // New best badge
            if (isNewBest)
            {
                _badgeText.text = "★ NEW BEST ★";
                _badgeText.gameObject.SetActive(true);
                _badgeText.transform.localScale = Vector3.zero;
                Tween.Scale(_badgeText.transform, endValue: 1f, duration: 0.4f, ease: Ease.OutBack, startDelay: 0.8f);
                Tween.Scale(_badgeText.transform, startValue: 1f, endValue: 1.1f, duration: 0.6f, cycles: -1, cycleMode: CycleMode.Yoyo, startDelay: 1.2f);
            }
            else
            {
                _badgeText.gameObject.SetActive(false);
            }

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
            _coinsText.text = "";
            _badgeText.gameObject.SetActive(false);

            // Hide stars
            for (int i = 0; i < 3; i++)
            {
                _starLabels[i].transform.localScale = Vector3.zero;
            }

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
            
            var cardImage = cardGo.GetComponent<Image>();
            if (_theme != null)
            {
                _theme.ApplyPanel(cardImage);
            }
            else
            {
                cardImage.color = new Color32(0x3D, 0x24, 0x59, 0xFF);
            }

            _titleText = MainMenuScreen.UIText(_card, "", 30, FontStyles.Bold);
            SetRect(_titleText.rectTransform, 0.05f, 0.64f, 0.95f, 0.92f);
            if (_theme != null)
            {
                _titleText.color = _theme.NeutralLight;
                if (_theme.HeadingFont != null) _titleText.font = _theme.HeadingFont;
            }

            // Badge text (NEW BEST)
            _badgeText = MainMenuScreen.UIText(_card, "", 18, FontStyles.Bold);
            SetRect(_badgeText.rectTransform, 0.05f, 0.54f, 0.95f, 0.62f);
            _badgeText.color = _theme != null ? _theme.AccentLime : Color.green;
            if (_theme != null && _theme.HeadingFont != null) _badgeText.font = _theme.HeadingFont;
            _badgeText.gameObject.SetActive(false);

            // Stars Layout container
            var starsGo = new GameObject("StarsContainer", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            starsGo.transform.SetParent(_card, false);
            _starsContainer = (RectTransform)starsGo.transform;
            SetRect(_starsContainer, 0.2f, 0.4f, 0.8f, 0.52f);
            
            var starsLayout = starsGo.GetComponent<HorizontalLayoutGroup>();
            starsLayout.spacing = 10;
            starsLayout.childAlignment = TextAnchor.MiddleCenter;
            starsLayout.childControlWidth = true;
            starsLayout.childControlHeight = true;
            starsLayout.childForceExpandWidth = false;
            starsLayout.childForceExpandHeight = true;

            for (int i = 0; i < 3; i++)
            {
                var starLabel = MainMenuScreen.UIText(_starsContainer, "★", 40, FontStyles.Bold);
                _starLabels[i] = starLabel;
            }

            _coinsText = MainMenuScreen.UIText(_card, "", 24, FontStyles.Normal);
            SetRect(_coinsText.rectTransform, 0.05f, 0.26f, 0.95f, 0.38f);
            if (_theme != null)
            {
                _coinsText.color = _theme.CoinTextColor;
                if (_theme.BodyFont != null) _coinsText.font = _theme.BodyFont;
            }

            var buttonRowGo = new GameObject("Buttons", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            buttonRowGo.transform.SetParent(_card, false);
            _buttonRow = (RectTransform)buttonRowGo.transform;
            SetRect(_buttonRow, 0.1f, 0.06f, 0.9f, 0.22f);
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
            if (_theme != null && _theme.BodyFont != null) text.font = _theme.BodyFont;
            MainMenuScreen.StretchFull(text.rectTransform);

            button.onClick.AddListener(() =>
            {
                _audio?.PlayOneShot(_stingSound);
                onClick();
            });
        }
    }
}
