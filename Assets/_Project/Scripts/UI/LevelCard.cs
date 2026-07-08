using System;
using BadMovieClues.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BadMovieClues.UI
{
    /// <summary>
    /// One cell in the LevelSelectScreen grid. Locked cards deliberately
    /// don't show the movie title (would spoil the puzzle before it's
    /// reachable) - just the level number and a "Locked" label.
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

        public void Bind(LevelData level, int index, bool unlocked, bool solved, Action onClick)
        {
            _label.text = !unlocked
                ? $"{index + 1}\nLocked"
                : solved
                    ? $"{index + 1}\n{level.MovieTitle}\n✓"
                    : $"{index + 1}\n{level.MovieTitle}";
            _button.interactable = unlocked;
            _onClick = onClick;
        }
    }
}
