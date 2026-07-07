using System.Collections.Generic;
using BadMovieClues.Core;
using BadMovieClues.Data;
using BadMovieClues.Puzzle;
using UnityEngine;
using UnityEngine.UI;

namespace BadMovieClues.UI
{
    /// <summary>
    /// Minimal M3 view: description text, blanks row, picture, and a plain
    /// A-Z keyboard built at runtime. No animation/juice yet - that's M5.
    /// </summary>
    public class GameHud : MonoBehaviour
    {
        [SerializeField] private Text descriptionText;
        [SerializeField] private Text blanksText;
        [SerializeField] private Image pictureImage;
        [SerializeField] private RectTransform keyboardRoot;

        private GameController _controller;
        private readonly Dictionary<char, Button> _letterButtons = new Dictionary<char, Button>();

        public void Bind(GameController controller)
        {
            _controller = controller;
            controller.LevelLoaded += OnLevelLoaded;
            controller.Won += () => SetStatus("YOU WIN!");
            controller.Lost += () => SetStatus("YOU LOSE!");
            BuildKeyboard();
        }

        private void OnLevelLoaded(LevelData level, Sprite sprite)
        {
            descriptionText.text = level.BadDescription;
            pictureImage.sprite = sprite;
            pictureImage.enabled = sprite != null;

            foreach (var button in _letterButtons.Values) button.interactable = true;
            RefreshBlanks();
        }

        private void RefreshBlanks()
        {
            blanksText.text = _controller.CurrentPuzzle.MaskedDisplay();
        }

        private void SetStatus(string status)
        {
            blanksText.text = $"{_controller.CurrentPuzzle.MaskedDisplay()}  [{status}]";
            foreach (var button in _letterButtons.Values) button.interactable = false;
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
            RefreshBlanks();
        }
    }
}
