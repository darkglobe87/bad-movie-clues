using System.Collections.Generic;
using BadMovieClues.Core;
using BadMovieClues.Data;
using BadMovieClues.Puzzle;
using UnityEngine;
using UnityEngine.UI;

namespace BadMovieClues.UI
{
    /// <summary>
    /// Minimal view: description text, blanks row, picture, an A-Z keyboard,
    /// a coin balance, and three hint buttons that gate on affordability.
    /// No animation/juice yet - that's M5.
    /// </summary>
    public class GameHud : MonoBehaviour
    {
        [SerializeField] private Text descriptionText;
        [SerializeField] private Text blanksText;
        [SerializeField] private Image pictureImage;
        [SerializeField] private RectTransform keyboardRoot;
        [SerializeField] private Text coinBalanceText;
        [SerializeField] private Text characterClueText;
        [SerializeField] private Button pictureHintButton;
        [SerializeField] private Text pictureHintButtonLabel;
        [SerializeField] private Button characterHintButton;
        [SerializeField] private Text characterHintButtonLabel;
        [SerializeField] private Button letterHintButton;
        [SerializeField] private Text letterHintButtonLabel;

        private GameController _controller;
        private readonly Dictionary<char, Button> _letterButtons = new Dictionary<char, Button>();
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
            RefreshBlanks();
            RefreshHintButtons();
        }

        private void RefreshBlanks()
        {
            blanksText.text = _controller.CurrentPuzzle.MaskedDisplay();
        }

        private void SetStatus(string status)
        {
            blanksText.text = $"{_controller.CurrentPuzzle.MaskedDisplay()}  [{status}]";
            foreach (var button in _letterButtons.Values) button.interactable = false;
            RefreshHintButtons();
        }

        private void OnPictureHintClicked()
        {
            if (_pictureRevealed || !_controller.TryRevealPictureHint()) return;

            _pictureRevealed = true;
            pictureImage.sprite = _pendingPictureSprite;
            pictureImage.enabled = _pendingPictureSprite != null;
            RefreshHintButtons();
        }

        private void OnCharacterHintClicked()
        {
            if (_characterRevealed || !_controller.TryRevealCharacterHint()) return;

            _characterRevealed = true;
            characterClueText.text = _controller.CurrentLevel.CharacterClue;
            RefreshHintButtons();
        }

        private void OnLetterHintClicked()
        {
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
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            foreach (var letter in "ABCDEFGHIJKLMNOPQRSTUVWXYZ")
            {
                var buttonGo = new GameObject($"Key_{letter}", typeof(RectTransform), typeof(Image), typeof(Button));
                buttonGo.transform.SetParent(keyboardRoot, false);

                var labelGo = new GameObject("Label", typeof(RectTransform), typeof(Text));
                labelGo.transform.SetParent(buttonGo.transform, false);
                var label = labelGo.GetComponent<Text>();
                label.text = letter.ToString();
                label.font = font;
                label.alignment = TextAnchor.MiddleCenter;
                label.color = Color.black;
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
            var outcome = _controller.GuessLetter(letter);
            if (outcome != GuessOutcome.Correct && outcome != GuessOutcome.Incorrect) return;

            button.interactable = false;
            if (_controller.CurrentPuzzle.IsOver) return; // SetStatus already refreshed everything

            RefreshBlanks();
            RefreshHintButtons();
        }
    }
}
