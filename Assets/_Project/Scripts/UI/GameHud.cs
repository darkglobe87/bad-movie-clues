using System.Collections.Generic;
using BadMovieClues.Core;
using BadMovieClues.Data;
using BadMovieClues.Puzzle;
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
    /// letter/picture/character "pop" reveal - purely visual, no game logic
    /// lives here beyond what M3/M4 already had.
    /// </summary>
    public class GameHud : MonoBehaviour
    {
        private const char BlankChar = '_';
        private const float SquishScale = 0.88f;
        private const float SquishDuration = 0.08f;
        private const float PopDuration = 0.35f;

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
        private readonly Dictionary<char, Button> _letterButtons = new Dictionary<char, Button>();
        private TextMeshProUGUI[] _tileLabels;
        private string _previousMaskedDisplay = "";
        private Sprite _pendingPictureSprite;
        private bool _pictureRevealed;
        private bool _characterRevealed;

        public void Bind(GameController controller)
        {
            _controller = controller;
            controller.LevelLoaded += OnLevelLoaded;
            controller.Won += () => SetStatus("YOU WIN!");
            controller.Lost += () => SetStatus("YOU LOSE!");
            controller.Currency.OnBalanceChanged += _ => RefreshHintButtons();

            pictureHintButtonLabel.text = $"Picture ({controller.Config.PictureHintCost})";
            characterHintButtonLabel.text = $"Character ({controller.Config.CharacterHintCost})";
            letterHintButtonLabel.text = $"Letter ({controller.Config.LetterHintCost})";

            pictureHintButton.onClick.AddListener(OnPictureHintClicked);
            characterHintButton.onClick.AddListener(OnCharacterHintClicked);
            letterHintButton.onClick.AddListener(OnLetterHintClicked);

            RefreshHintButtons();
            BuildKeyboard();
        }

        private void OnLevelLoaded(LevelData level, Sprite sprite)
        {
            descriptionText.text = level.BadDescription;

            _pendingPictureSprite = sprite;
            _pictureRevealed = false;
            _characterRevealed = false;
            pictureImage.enabled = false;
            characterClueText.text = "";

            foreach (var button in _letterButtons.Values) button.interactable = true;

            BuildBlanksRow(level.MovieTitle);
            RefreshHintButtons();
        }

        private void BuildBlanksRow(string title)
        {
            foreach (Transform child in blanksRoot) Destroy(child.gameObject);

            _tileLabels = new TextMeshProUGUI[title.Length];
            for (var i = 0; i < title.Length; i++)
            {
                var tileGo = new GameObject($"Tile_{i}", typeof(RectTransform));
                tileGo.transform.SetParent(blanksRoot, false);

                var tmp = tileGo.AddComponent<TextMeshProUGUI>();
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.fontSize = 44;
                tmp.color = Color.black;
                tmp.text = char.IsLetter(title[i]) ? BlankChar.ToString() : title[i].ToString();

                _tileLabels[i] = tmp;
            }

            // Placeholder that can't match BlankChar or any real character, so
            // the first RefreshBlanks() never spuriously pops anything that
            // was already revealed from the start (spaces/punctuation).
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
                    PlayPop(_tileLabels[i].rectTransform);
                }
            }
            _previousMaskedDisplay = newDisplay;
        }

        private void SetStatus(string status)
        {
            descriptionText.text = $"{_controller.CurrentLevel.BadDescription}\n\n{status}";
            foreach (var button in _letterButtons.Values) button.interactable = false;
            RefreshHintButtons();
        }

        private void OnPictureHintClicked()
        {
            PlaySquish(pictureHintButton.transform);
            if (_pictureRevealed || !_controller.TryRevealPictureHint()) return;

            _pictureRevealed = true;
            pictureImage.sprite = _pendingPictureSprite;
            pictureImage.enabled = _pendingPictureSprite != null;
            if (pictureImage.enabled) PlayPop(pictureImage.rectTransform);
            RefreshHintButtons();
        }

        private void OnCharacterHintClicked()
        {
            PlaySquish(characterHintButton.transform);
            if (_characterRevealed || !_controller.TryRevealCharacterHint()) return;

            _characterRevealed = true;
            characterClueText.text = _controller.CurrentLevel.CharacterClue;
            PlayPop(characterClueText.rectTransform);
            RefreshHintButtons();
        }

        private void OnLetterHintClicked()
        {
            PlaySquish(letterHintButton.transform);
            if (!_controller.TryRevealLetterHint()) return;
            if (_controller.CurrentPuzzle.IsOver) return; // SetStatus already refreshed everything

            RefreshBlanks();
            RefreshHintButtons();
        }

        private void RefreshHintButtons()
        {
            var balance = _controller.Currency.Balance;
            var config = _controller.Config;
            var over = _controller.CurrentPuzzle?.IsOver ?? true;

            coinBalanceText.text = $"Coins: {balance}";
            pictureHintButton.interactable = !over && !_pictureRevealed && balance >= config.PictureHintCost;
            characterHintButton.interactable = !over && !_characterRevealed && balance >= config.CharacterHintCost;
            letterHintButton.interactable = !over && balance >= config.LetterHintCost;
        }

        private void BuildKeyboard()
        {
            foreach (var letter in "ABCDEFGHIJKLMNOPQRSTUVWXYZ")
            {
                var buttonGo = new GameObject($"Key_{letter}", typeof(RectTransform), typeof(Image), typeof(Button));
                buttonGo.transform.SetParent(keyboardRoot, false);

                var labelGo = new GameObject("Label", typeof(RectTransform));
                labelGo.transform.SetParent(buttonGo.transform, false);
                var label = labelGo.AddComponent<TextMeshProUGUI>();
                label.text = letter.ToString();
                label.alignment = TextAlignmentOptions.Center;
                label.color = Color.black;
                label.fontSize = 28;
                var labelRt = (RectTransform)labelGo.transform;
                labelRt.anchorMin = Vector2.zero;
                labelRt.anchorMax = Vector2.one;
                labelRt.offsetMin = Vector2.zero;
                labelRt.offsetMax = Vector2.zero;

                var button = buttonGo.GetComponent<Button>();
                var capturedLetter = letter;
                button.onClick.AddListener(() => OnLetterPressed(capturedLetter, button));

                _letterButtons[letter] = button;
            }
        }

        private void OnLetterPressed(char letter, Button button)
        {
            PlaySquish(button.transform);
            var outcome = _controller.GuessLetter(letter);
            if (outcome != GuessOutcome.Correct && outcome != GuessOutcome.Incorrect) return;

            button.interactable = false;
            if (_controller.CurrentPuzzle.IsOver) return; // SetStatus already refreshed everything

            RefreshBlanks();
            RefreshHintButtons();
        }

        private static void PlaySquish(Transform target)
        {
            Tween.Scale(target, endValue: SquishScale, duration: SquishDuration, cycles: 2, cycleMode: CycleMode.Yoyo);
        }

        private static void PlayPop(Transform target)
        {
            target.localScale = Vector3.zero;
            Tween.Scale(target, endValue: 1f, duration: PopDuration, ease: Ease.OutElastic);
        }
    }
}
