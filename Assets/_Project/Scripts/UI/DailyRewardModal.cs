using System;
using BadMovieClues.Economy;
using BadMovieClues.Progression;
using PrimeTween;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BadMovieClues.UI
{
    /// <summary>
    /// Full-screen modal that appears once per day on the main menu.
    /// Shows a 7-day reward track, claims today's streak reward, and
    /// grants coins via ICurrencyService. Built procedurally, same
    /// pattern as every other screen in this project.
    /// </summary>
    public class DailyRewardModal : MonoBehaviour
    {
        private UITheme _theme;
        private AudioClip _clickSound;
        private RetentionService _retention;
        private ICurrencyService _currency;
        private GameConfig _config;
        private Action _onDismissed;

        public void Init(UITheme theme, AudioClip clickSound, RetentionService retention,
            ICurrencyService currency, GameConfig config, Action onDismissed)
        {
            _theme = theme;
            _clickSound = clickSound;
            _retention = retention;
            _currency = currency;
            _config = config;
            _onDismissed = onDismissed;
            Build();
        }

        private void Build()
        {
            // Dim background overlay
            var dimGo = new GameObject("Dim", typeof(RectTransform), typeof(Image));
            dimGo.transform.SetParent(transform, false);
            MainMenuScreen.StretchFull((RectTransform)dimGo.transform);
            var dimImage = dimGo.GetComponent<Image>();
            dimImage.color = new Color(0f, 0f, 0f, 0.75f);

            // Modal card
            var cardGo = new GameObject("Card", typeof(RectTransform), typeof(Image));
            cardGo.transform.SetParent(transform, false);
            var cardRt = (RectTransform)cardGo.transform;
            cardRt.anchorMin = new Vector2(0.08f, 0.2f);
            cardRt.anchorMax = new Vector2(0.92f, 0.8f);
            cardRt.offsetMin = cardRt.offsetMax = Vector2.zero;
            var cardImage = cardGo.GetComponent<Image>();
            if (_theme != null) _theme.ApplyPanel(cardImage);
            else cardImage.color = new Color32(0x35, 0x20, 0x4E, 0xFF);

            // Scale-in animation
            cardRt.localScale = Vector3.zero;
            Tween.Scale(cardRt, endValue: 1f, duration: 0.4f, ease: Ease.OutBack);

            // Claim the reward
            var reward = _retention.RecordLogin(_config.StreakRewards);
            if (reward > 0)
            {
                _currency.Add(reward);
            }
            var currentDay = _retention.CurrentStreakDay;
            var wasStreakBroken = _retention.WasStreakBroken;

            // Title
            var titleText = MainMenuScreen.UIText(cardRt, "Daily Reward", 32, FontStyles.Bold);
            var titleRt = titleText.rectTransform;
            titleRt.anchorMin = new Vector2(0.05f, 0.82f);
            titleRt.anchorMax = new Vector2(0.95f, 0.95f);
            titleRt.offsetMin = titleRt.offsetMax = Vector2.zero;
            if (_theme != null)
            {
                titleText.color = _theme.AccentGold;
                if (_theme.HeadingFont != null) titleText.font = _theme.HeadingFont;
            }

            // Streak status
            var statusMsg = wasStreakBroken
                ? "Streak reset! Start a new one!"
                : $"Day {currentDay} streak!";
            var statusText = MainMenuScreen.UIText(cardRt, statusMsg, 20, FontStyles.Normal);
            var statusRt = statusText.rectTransform;
            statusRt.anchorMin = new Vector2(0.05f, 0.72f);
            statusRt.anchorMax = new Vector2(0.95f, 0.82f);
            statusRt.offsetMin = statusRt.offsetMax = Vector2.zero;
            if (_theme != null)
            {
                statusText.color = wasStreakBroken ? _theme.DangerRed : _theme.NeutralLight;
                if (_theme.BodyFont != null) statusText.font = _theme.BodyFont;
            }

            // 7-day track
            var trackGo = new GameObject("Track", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            trackGo.transform.SetParent(cardRt, false);
            var trackRt = (RectTransform)trackGo.transform;
            trackRt.anchorMin = new Vector2(0.05f, 0.38f);
            trackRt.anchorMax = new Vector2(0.95f, 0.7f);
            trackRt.offsetMin = trackRt.offsetMax = Vector2.zero;
            var trackLayout = trackGo.GetComponent<HorizontalLayoutGroup>();
            trackLayout.spacing = 6;
            trackLayout.childAlignment = TextAnchor.MiddleCenter;
            trackLayout.childControlWidth = true;
            trackLayout.childControlHeight = true;
            trackLayout.childForceExpandWidth = true;
            trackLayout.childForceExpandHeight = true;

            var rewardTrack = _config.StreakRewards;
            for (var i = 0; i < rewardTrack.Length; i++)
            {
                var dayNum = i + 1;
                BuildDayBox(trackGo.transform, dayNum, rewardTrack[i], currentDay, i);
            }

            // Reward text
            var rewardMsg = reward > 0 ? $"+{reward} Coins!" : "Already claimed!";
            var rewardText = MainMenuScreen.UIText(cardRt, rewardMsg, 28, FontStyles.Bold);
            var rewardRt = rewardText.rectTransform;
            rewardRt.anchorMin = new Vector2(0.05f, 0.22f);
            rewardRt.anchorMax = new Vector2(0.95f, 0.38f);
            rewardRt.offsetMin = rewardRt.offsetMax = Vector2.zero;
            if (_theme != null)
            {
                rewardText.color = _theme.CoinTextColor;
                if (_theme.HeadingFont != null) rewardText.font = _theme.HeadingFont;
            }

            // Pulsing reward text
            if (reward > 0)
            {
                Tween.Scale(rewardRt, startValue: 0.5f, endValue: 1f, duration: 0.5f, ease: Ease.OutElastic, startDelay: 0.3f);
            }

            // Collect button
            var collectGo = new GameObject("CollectButton", typeof(RectTransform), typeof(Image), typeof(Button));
            collectGo.transform.SetParent(cardRt, false);
            var collectRt = (RectTransform)collectGo.transform;
            collectRt.anchorMin = new Vector2(0.25f, 0.06f);
            collectRt.anchorMax = new Vector2(0.75f, 0.2f);
            collectRt.offsetMin = collectRt.offsetMax = Vector2.zero;
            var collectButton = collectGo.GetComponent<Button>();
            var collectLabel = MainMenuScreen.UIText(collectGo.transform, "Collect", 24, FontStyles.Bold);
            MainMenuScreen.StretchFull(collectLabel.rectTransform);
            if (_theme != null) _theme.ApplyButton(collectButton, collectGo.GetComponent<Image>());

            collectButton.onClick.AddListener(() =>
            {
                AppRoot.Instance.Haptics?.VibrateClick();
                AppRoot.Instance.AudioService.PlayOneShot(_clickSound);

                // Coin burst from reward text toward coin display
                if (reward > 0)
                {
                    var canvas = GetComponentInParent<Canvas>();
                    if (canvas != null)
                    {
                        var canvasRt = (RectTransform)canvas.transform;
                        var fromPos = canvasRt.InverseTransformPoint(rewardRt.position);
                        ConfettiBurst.PlayCoinBurst(canvasRt, fromPos, fromPos + new Vector3(0, 200, 0), 8);
                    }
                }

                _onDismissed?.Invoke();
                Destroy(gameObject);
            });
        }

        private void BuildDayBox(Transform parent, int dayNum, int dayReward, int currentDay, int index)
        {
            var boxGo = new GameObject($"Day_{dayNum}", typeof(RectTransform), typeof(Image));
            boxGo.transform.SetParent(parent, false);
            var boxImage = boxGo.GetComponent<Image>();
            boxImage.sprite = ProceduralIcons.RoundedRect;
            boxImage.type = Image.Type.Sliced;

            var isPast = dayNum < currentDay;
            var isToday = dayNum == currentDay;
            var isFuture = dayNum > currentDay;

            if (_theme != null)
            {
                if (isPast)
                    boxImage.color = new Color(_theme.AccentGold.r, _theme.AccentGold.g, _theme.AccentGold.b, 0.4f);
                else if (isToday)
                    boxImage.color = _theme.AccentGold;
                else
                    boxImage.color = _theme.SeparatorColor;
            }
            else
            {
                boxImage.color = isToday ? Color.yellow : Color.grey;
            }

            // Day number + reward
            var labelText = isPast ? $"D{dayNum}\n\u2713" : $"D{dayNum}\n{dayReward}";
            var label = MainMenuScreen.UIText(boxGo.transform, labelText, 14, FontStyles.Bold);
            MainMenuScreen.StretchFull(label.rectTransform);
            if (_theme != null)
            {
                label.color = (isPast || isToday) ? _theme.BackgroundTop : _theme.NeutralLight;
                if (_theme.BodyFont != null) label.font = _theme.BodyFont;
            }

            // Pulse animation for today's box
            if (isToday)
            {
                var boxRt = (RectTransform)boxGo.transform;
                Tween.Scale(boxRt, startValue: 0.9f, endValue: 1.05f, duration: 0.8f,
                    cycles: -1, cycleMode: CycleMode.Yoyo, startDelay: index * 0.05f);
            }
        }
    }
}
