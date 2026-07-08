using System;
using BadMovieClues.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BadMovieClues.UI
{
    /// <summary>
    /// One cell in the LevelSelectScreen grid. Only *solved* cards show the
    /// movie title - showing it as soon as a level is merely unlocked (the
    /// original M11 behavior) spoils the puzzle before the player has even
    /// attempted it, which is a much bigger miss than the locked-card case
    /// this class originally guarded against. Unlocked-but-unsolved cards
    /// show a plain "Play" prompt instead.
    /// </summary>
    public class LevelCard : MonoBehaviour
    {
        private Button _button;
        private TextMeshProUGUI _label;
        private Action _onClick;

        public void Setup(Button button, TextMeshProUGUI label)
        {
            _button = button;
            _label = label;
            _button.onClick.AddListener(() => _onClick?.Invoke());
        }

        public void Bind(LevelData level, int index, bool unlocked, bool solved, int stars, Action onClick)
        {
            _label.text = !unlocked
                ? $"{index + 1}\nLocked"
                : solved
                    ? $"{index + 1}\n{level.MovieTitle}\n{new string('★', stars)}"
                    : $"{index + 1}\nPlay";
            _button.interactable = unlocked;
            _onClick = onClick;
        }
    }
}
