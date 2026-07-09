using System.Collections.Generic;
using BadMovieClues.Core;
using BadMovieClues.Data;
using BadMovieClues.Puzzle;
using BadMovieClues.Services;
using PrimeTween;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BadMovieClues.UI
{
    /// <summary>
    /// Description text, per-letter blanks row, picture, an A-Z keyboard, a
    /// coin balance, and three hint buttons that gate on affordability.
    /// TextMeshPro throughout; PrimeTween drives the button "squish" and the
    /// letter/picture/character "pop" reveal; UITheme skins buttons/tiles.
    /// Enhanced with dynamic panels, QWERTY correct/incorrect feedback, coin particle bursts.
    /// </summary>
    public class GameHud : MonoBehaviour
    {
        private const char BlankChar = '_';
        private const float SquishScale = 0.88f;
        private const float SquishDuration = 0.08f;
        private const float PopDuration = 0.35f;

        [SerializeField] private UITheme theme;
        [SerializeField] private AudioClip clickSound;
        [SerializeField] private RectTransform canvasRoot;

        [SerializeField] private TextMeshProUGUI descriptionText;
        [SerializeField] private RectTransform blanksRoot;
        [SerializeField] private Image pictureImage;
        [SerializeField] private RectTransform keyboardRoot;
        [SerializeField] private TextMeshProUGUI coinBalanceText;
        [SerializeField] private TextMeshProUGUI characterClueText;
        [SerializeField] private Button pictureHintButton;
        [SerializeField] private TextMeshProUGUI pictureHintButtonLabel;
        [SerializeField] private Button characterHintButton;
        [SerializeField] private TextMeshProUGUI characterHintButtonLabel;
        [SerializeField] private Button letterHintButton;
        [SerializeField] private TextMeshProUGUI letterHintButtonLabel;

        private GameController _controller;
        private IAudioService _audioService;
        private LevelCompleteScreen _levelCompleteScreen;
        private readonly Dictionary<char, Button> _letterButtons = new Dictionary<char, Button>();
        private TextMeshProUGUI[] _tileLabels;
        private RectTransform[] _tileRoots;
        private string _previousMaskedDisplay = "";
        private Sprite _pendingPictureSprite;
        private bool _pictureRevealed;
        private bool _characterRevealed;
        private int _previousStars;
        private UIObjectPool<Image> _tilePool;
        private bool _isDailySession;

        public void Bind(GameController controller, IAudioService audioService)
        {
            _controller = controller;
            _audioService = audioService;
            _tilePool = new UIObjectPool<Image>(blanksRoot);

            controller.LevelLoaded += OnLevelLoaded;
            controller.Won += OnWon;
            controller.Lost += OnLost;
            controller.Currency.OnBalanceChanged += _ => RefreshHintButtons();

            var completeGo = new GameObject("LevelCompleteScreen", typeof(RectTransform));
            completeGo.transform.SetParent(canvasRoot, false);
            MainMenuScreen.StretchFull((RectTransform)completeGo.transform);
            _levelCompleteScreen = completeGo.AddComponent<LevelCompleteScreen>();
            _levelCompleteScreen.Init(theme, clickSound, _audioService);
            _levelCompleteScreen.gameObject.SetActive(false);

            // Show "DAILY CHALLENGE" banner if this session is a daily
            if (AppRoot.Instance.IsDailyChallenge || _isDailySession)
            {
                _isDailySession = true;
                BuildDailyBanner();
            }

            pictureHintButton.onClick.AddListener(OnPictureHintClicked);
            characterHintButton.onClick.AddListener(OnCharacterHintClicked);
            letterHintButton.onClick.AddListener(OnLetterHintClicked);

            if (pictureImage != null)
            {
                var tv = pictureImage.gameObject.GetComponent<RetroTVFrame>();
                if (tv == null) tv = pictureImage.gameObject.AddComponent<RetroTVFrame>();
            }

            if (theme != null)
            {
                theme.ApplyButton(pictureHintButton, pictureHintButton.GetComponent<Image>());
                theme.ApplyButton(characterHintButton, characterHintButton.GetComponent<Image>());
                theme.ApplyButton(letterHintButton, letterHintButton.GetComponent<Image>());

                // Style coin, character, and description texts
                descriptionText.color = theme.NeutralLight;
                if (theme.BodyFont != null) descriptionText.font = theme.BodyFont;
                
                coinBalanceText.color = theme.CoinTextColor;
                if (theme.BodyFont != null) coinBalanceText.font = theme.BodyFont;
                
                characterClueText.color = theme.NeutralLight;
                if (theme.BodyFont != null) characterClueText.font = theme.BodyFont;

                // Add dynamic panel behind description text for visual separation
                if (descriptionText != null)
                {
                    var panelGo = new GameObject("DescriptionPanel", typeof(RectTransform), typeof(Image));
                    panelGo.transform.SetParent(descriptionText.transform.parent, false);
                    var panelRt = (RectTransform)panelGo.transform;

                    var descRt = descriptionText.rectTransform;
                    panelRt.anchorMin = descRt.anchorMin;
                    panelRt.anchorMax = descRt.anchorMax;
                    panelRt.anchoredPosition = descRt.anchoredPosition;
                    panelRt.sizeDelta = descRt.sizeDelta;
                    panelRt.offsetMin = descRt.offsetMin;
                    panelRt.offsetMax = descRt.offsetMax;

                    panelGo.transform.SetSiblingIndex(descriptionText.transform.GetSiblingIndex());

                    var panelImage = panelGo.GetComponent<Image>();
                    theme.ApplyPanel(panelImage);

                    descriptionText.transform.SetParent(panelRt, false);
                    descRt.anchorMin = Vector2.zero;
                    descRt.anchorMax = Vector2.one;
                    descRt.offsetMin = new Vector2(16f, 12f);
                    descRt.offsetMax = new Vector2(-16f, -12f);
                }
            }

            RefreshHintButtons();
            BuildKeyboard();

            if (theme != null)
            {
                var allButtons = canvasRoot.GetComponentsInChildren<Button>(true);
                foreach (var b in allButtons)
                {
                    if (b.name.Contains("Menu") || b.name.Contains("Back"))
                    {
                        theme.ApplyButton(b, b.GetComponent<Image>());
                    }
                }
            }
        }

        private void OnLevelLoaded(LevelData level, Sprite sprite)
        {
            descriptionText.text = level.BadDescription;

            _pendingPictureSprite = sprite;
            _pictureRevealed = false;
            _characterRevealed = false;
            pictureImage.enabled = false;
            characterClueText.text = "";
            _previousStars = AppRoot.Instance.Progress.GetStars(level.Id);

            foreach (var button in _letterButtons.Values)
            {
                button.interactable = true;
                // Reset key colors to default theme button state
                if (theme != null)
                {
                    var img = button.GetComponent<Image>();
                    img.color = Color.white;
                }
            }

            BuildBlanksRow(level.MovieTitle);
            RefreshHintButtons();
        }

        private void BuildBlanksRow(string title)
        {
            _tilePool.Clear();

            _tileLabels = new TextMeshProUGUI[title.Length];
            _tileRoots = new RectTransform[title.Length];

            for (var i = 0; i < title.Length; i++)
            {
                var tileImage = _tilePool.Get(() =>
                {
                    var tileGo = new GameObject("Tile", typeof(RectTransform), typeof(Image));
                    var image = tileGo.GetComponent<Image>();
                    
                    var labelGo = new GameObject("Label", typeof(RectTransform));
                    labelGo.transform.SetParent(tileGo.transform, false);
                    var tmp = labelGo.AddComponent<TextMeshProUGUI>();
                    tmp.alignment = TextAlignmentOptions.Center;
                    tmp.fontSize = 46;
                    tmp.fontStyle = FontStyles.Bold;
                    
                    var labelRt = (RectTransform)labelGo.transform;
                    labelRt.anchorMin = Vector2.zero;
                    labelRt.anchorMax = Vector2.one;
                    labelRt.offsetMin = Vector2.zero;
                    labelRt.offsetMax = Vector2.zero;
                    
                    return image;
                });

                tileImage.gameObject.name = $"Tile_{i}";
                if (theme != null) theme.ApplyTile(tileImage, isKeyboardKey: false);
                else tileImage.enabled = false;

                Tween.StopAll(tileImage);
                Tween.StopAll(tileImage.transform);
                tileImage.transform.localScale = Vector3.one;

                if (char.IsLetter(title[i]))
                {
                    Tween.Alpha(tileImage, startValue: 0.6f, endValue: 1.0f, duration: 1.5f, cycles: -1, cycleMode: CycleMode.Yoyo);
                }

                var label = tileImage.transform.GetChild(0).GetComponent<TextMeshProUGUI>();
                if (theme != null)
                {
                    label.color = theme.AccentMagenta;
                    if (theme.HeadingFont != null) label.font = theme.HeadingFont;
                }
                else
                {
                    label.color = Color.black;
                }
                label.outlineWidth = 0.2f;
                label.outlineColor = theme != null ? (Color32)theme.BackgroundTop : new Color32(0x2A, 0x1A, 0x3E, 0xFF);
                label.text = char.IsLetter(title[i]) ? BlankChar.ToString() : title[i].ToString();

                _tileLabels[i] = label;
                _tileRoots[i] = (RectTransform)tileImage.transform;
            }

            _previousMaskedDisplay = new string('\0', title.Length);
            RefreshBlanks();
        }

        private void RefreshBlanks()
        {
            var newDisplay = _controller.CurrentPuzzle.MaskedDisplay();
            for (var i = 0; i < _tileLabels.Length; i++)
            {
                var newChar = newDisplay[i];
                var oldChar = i < _previousMaskedDisplay.Length ? _previousMaskedDisplay[i] : BlankChar;

                _tileLabels[i].text = newChar.ToString();

                if (oldChar == BlankChar && newChar != BlankChar)
                {
                    var tileImage = _tileRoots[i].GetComponent<Image>();
                    Tween.StopAll(tileImage);
                    if (theme != null) tileImage.color = theme.NeutralLight;
                    else tileImage.color = Color.white;
                    
                    PlayPop(_tileRoots[i]);

                    // Glow flash on the newly revealed letter text
                    var text = _tileLabels[i];
                    Tween.Color(text, endValue: theme != null ? theme.AccentGold : Color.yellow, duration: 0.15f, cycles: 2, cycleMode: CycleMode.Yoyo);
                }
            }
            _previousMaskedDisplay = newDisplay;
        }

        private void SetStatus(string status)
        {
            RefreshBlanks();
            descriptionText.text = $"{_controller.CurrentLevel.BadDescription}\n\n{status}";
            foreach (var button in _letterButtons.Values) button.interactable = false;
            RefreshHintButtons();
        }

        private void OnWon()
        {
            AppRoot.Instance.Haptics?.VibrateWin();
            SetStatus("YOU WIN!");
            for (var i = 0; i < _tileRoots.Length; i++) PlayPop(_tileRoots[i], delay: i * 0.05f);

            bool isNewBest = _controller.StarsEarned > _previousStars;
            _levelCompleteScreen.ShowWon(_controller.CurrentLevel.MovieTitle, _controller.StarsEarned,
                _isDailySession
                    ? _controller.Config.LevelCompleteReward * _controller.Config.DailyChallengeRewardMultiplier
                    : _controller.Config.LevelCompleteReward,
                isNewBest, OnNextClicked, _isDailySession);
        }

        private void OnLost()
        {
            AppRoot.Instance.Haptics?.VibrateFailure();
            SetStatus("YOU LOSE!");
            _levelCompleteScreen.ShowLost(_controller.CurrentLevel.MovieTitle, OnRetryClicked, OnMenuClicked);
        }

        private void OnNextClicked()
        {
            AppRoot.Instance.SelectedLevelIndex = _controller.CurrentIndex + 1;
            _ = ScreenNavigator.Instance.LoadScene("Gameplay", TransitionType.SlideLeft);
        }

        private void OnRetryClicked()
        {
            AppRoot.Instance.SelectedLevelIndex = _controller.CurrentIndex;
            _ = ScreenNavigator.Instance.LoadScene("Gameplay", TransitionType.Fade);
        }

        private void OnMenuClicked() => _ = ScreenNavigator.Instance.LoadScene("MainMenu", TransitionType.SlideRight);

        private void OnPictureHintClicked()
        {
            PlaySquish(pictureHintButton.transform);
            if (_pictureRevealed) return;
            
            var fromPos = GetCanvasLocalPos((RectTransform)pictureHintButton.transform);
            if (!_controller.TryRevealPictureHint()) return;

            // Trigger flying coin burst
            TriggerCoinBurst(fromPos);

            _pictureRevealed = true;
            pictureImage.sprite = _pendingPictureSprite;
            pictureImage.enabled = _pendingPictureSprite != null;
            if (pictureImage.enabled) PlayPop(pictureImage.rectTransform);
            RefreshHintButtons();
        }

        private void OnCharacterHintClicked()
        {
            PlaySquish(characterHintButton.transform);
            if (_characterRevealed) return;

            var fromPos = GetCanvasLocalPos((RectTransform)characterHintButton.transform);
            if (!_controller.TryRevealCharacterHint()) return;

            // Trigger flying coin burst
            TriggerCoinBurst(fromPos);

            _characterRevealed = true;
            characterClueText.text = _controller.CurrentLevel.CharacterClue;
            PlayPop(characterClueText.rectTransform);
            RefreshHintButtons();
        }

        private void OnLetterHintClicked()
        {
            PlaySquish(letterHintButton.transform);
            
            var fromPos = GetCanvasLocalPos((RectTransform)letterHintButton.transform);
            if (!_controller.TryRevealLetterHint()) return;

            // Trigger flying coin burst
            TriggerCoinBurst(fromPos);

            if (_controller.CurrentPuzzle.IsOver) return;

            RefreshBlanks();
            RefreshHintButtons();
        }

        private void TriggerCoinBurst(Vector2 targetPos)
        {
            if (canvasRoot != null && coinBalanceText != null)
            {
                var fromPos = GetCanvasLocalPos((RectTransform)coinBalanceText.transform);
                ConfettiBurst.PlayCoinBurst(canvasRoot, fromPos, targetPos, 12);
            }
        }

        private Vector2 GetCanvasLocalPos(RectTransform element)
        {
            if (canvasRoot == null || element == null) return Vector2.zero;
            return canvasRoot.InverseTransformPoint(element.position);
        }

        private void RefreshHintButtons()
        {
            var balance = _controller.Currency.Balance;
            var config = _controller.Config;
            var over = _controller.CurrentPuzzle?.IsOver ?? true;

            coinBalanceText.text = $"Coins: {balance}";
            var hasImage = _pendingPictureSprite != null;
            pictureHintButton.interactable = !over && !_pictureRevealed && hasImage && balance >= config.PictureHintCost;
            characterHintButton.interactable = !over && !_characterRevealed && balance >= config.CharacterHintCost;
            letterHintButton.interactable = !over && balance >= config.LetterHintCost;

            pictureHintButtonLabel.text = !hasImage
                ? "Pic (no image)"
                : HintLabel("Pic", config.PictureHintCost, balance, _pictureRevealed);
            characterHintButtonLabel.text = HintLabel("Who", config.CharacterHintCost, balance, _characterRevealed);
            letterHintButtonLabel.text = HintLabel("ABC", config.LetterHintCost, balance, revealed: false);
        }

        private static string HintLabel(string name, int cost, int balance, bool revealed)
        {
            if (revealed) return $"{name} ({cost})";
            return balance >= cost ? $"{name} ({cost})" : $"{name} ({cost}) - need {cost - balance} more";
        }

        private static readonly string[] QwertyRows = { "QWERTYUIOP", "ASDFGHJKL", "ZXCVBNM" };

        private void BuildKeyboard()
        {
            foreach (var row in QwertyRows)
            {
                var rowGo = new GameObject($"Row_{row}", typeof(RectTransform), typeof(HorizontalLayoutGroup));
                rowGo.transform.SetParent(keyboardRoot, false);

                var layout = rowGo.GetComponent<HorizontalLayoutGroup>();
                layout.spacing = 6;
                layout.childAlignment = TextAnchor.MiddleCenter;
                layout.childControlWidth = true;
                layout.childControlHeight = true;
                layout.childForceExpandWidth = true;
                layout.childForceExpandHeight = true;

                foreach (var letter in row)
                {
                    var buttonGo = new GameObject($"Key_{letter}", typeof(RectTransform), typeof(Image), typeof(Button));
                    buttonGo.transform.SetParent(rowGo.transform, false);
                    var labelGo = new GameObject("Label", typeof(RectTransform));
                    labelGo.transform.SetParent(buttonGo.transform, false);
                    var label = labelGo.AddComponent<TextMeshProUGUI>();
                    label.text = letter.ToString();
                    label.alignment = TextAlignmentOptions.Center;
                    label.fontSize = 28;
                    
                    var labelRt = (RectTransform)labelGo.transform;
                    labelRt.anchorMin = Vector2.zero;
                    labelRt.anchorMax = Vector2.one;
                    labelRt.offsetMin = Vector2.zero;
                    labelRt.offsetMax = Vector2.zero;

                    if (theme != null)
                    {
                        theme.ApplyButton(buttonGo.GetComponent<Button>(), buttonGo.GetComponent<Image>());
                    }
                    else
                    {
                        label.color = Color.black;
                    }

                    var button = buttonGo.GetComponent<Button>();
                    var capturedLetter = letter;
                    button.onClick.AddListener(() => OnLetterPressed(capturedLetter, button));

                    _letterButtons[letter] = button;
                }
            }
        }

        private void OnLetterPressed(char letter, Button button)
        {
            PlaySquish(button.transform);
            var outcome = _controller.GuessLetter(letter);
            if (outcome != GuessOutcome.Correct && outcome != GuessOutcome.Incorrect) return;

            button.interactable = false;
            
            // Visual QWERTY keyboard feedback based on correctness
            var keyImage = button.GetComponent<Image>();
            if (outcome == GuessOutcome.Correct)
            {
                if (theme != null)
                {
                    Tween.Color(keyImage, endValue: theme.AccentGold, duration: 0.15f, cycles: 2, cycleMode: CycleMode.Yoyo);
                }
            }
            else
            {
                if (theme != null)
                {
                    Tween.Color(keyImage, endValue: theme.DangerRed, duration: 0.15f, cycles: 2, cycleMode: CycleMode.Yoyo);
                }
            }

            if (_controller.CurrentPuzzle.IsOver) return;

            RefreshBlanks();
            RefreshHintButtons();
        }

        private void PlaySquish(Transform target)
        {
            AppRoot.Instance.Haptics?.VibrateClick();
            _audioService?.PlayOneShot(clickSound);
        }

        private static void PlayPop(Transform target, float delay = 0f)
        {
            target.localScale = Vector3.zero;
            Tween.Scale(target, endValue: 1f, duration: PopDuration, ease: Ease.OutElastic, startDelay: delay);
        }

        private void BuildDailyBanner()
        {
            var bannerGo = new GameObject("DailyBanner", typeof(RectTransform), typeof(Image));
            bannerGo.transform.SetParent(canvasRoot, false);
            var bannerRt = (RectTransform)bannerGo.transform;
            bannerRt.anchorMin = new Vector2(0.2f, 0.94f);
            bannerRt.anchorMax = new Vector2(0.8f, 0.99f);
            bannerRt.offsetMin = bannerRt.offsetMax = Vector2.zero;

            var bannerImage = bannerGo.GetComponent<Image>();
            bannerImage.sprite = ProceduralIcons.RoundedRect;
            bannerImage.type = Image.Type.Sliced;
            bannerImage.color = theme != null ? theme.AccentGold : Color.yellow;

            var bannerLabel = MainMenuScreen.UIText(bannerGo.transform, "DAILY CHALLENGE", 16, FontStyles.Bold);
            MainMenuScreen.StretchFull(bannerLabel.rectTransform);
            if (theme != null)
            {
                bannerLabel.color = theme.BackgroundTop;
                if (theme.BodyFont != null) bannerLabel.font = theme.BodyFont;
            }

            // Slide in from top
            var startPos = bannerRt.anchoredPosition + new Vector2(0, 60f);
            bannerRt.anchoredPosition = startPos;
            Tween.UIAnchoredPosition(bannerRt, endValue: startPos - new Vector2(0, 60f), duration: 0.4f, ease: Ease.OutBack);
        }
    }
}
