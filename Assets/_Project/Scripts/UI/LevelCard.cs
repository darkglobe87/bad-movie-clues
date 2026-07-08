using System;
using BadMovieClues.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BadMovieClues.UI
{
    /// <summary>
    /// One cell in the LevelSelectScreen grid. Only *solved* cards show the
    /// movie title. Unlocked-but-unsolved cards show a plain "Play" prompt instead.
    /// Enhanced with rich visual styling, lock icons, and color-coded status text.
    /// </summary>
    public class LevelCard : MonoBehaviour
    {
        private Button _button;
        private TextMeshProUGUI _label;
        private Action _onClick;
        private UITheme _theme;

        public void Setup(Button button, TextMeshProUGUI label, UITheme theme)
        {
            _button = button;
            _label = label;
            _theme = theme;
            _button.onClick.AddListener(() => _onClick?.Invoke());
        }

        public void Bind(LevelData level, int index, bool unlocked, bool solved, int stars, Action onClick)
        {
            _onClick = onClick;
            _button.interactable = unlocked;
            var img = _button.GetComponent<Image>();

            if (_theme != null)
            {
                _theme.ApplyCard(img, unlocked);
                
                _label.fontStyle = FontStyles.Normal;
                _label.outlineWidth = 0.1f;
                _label.outlineColor = _theme.BackgroundTop;

                if (!unlocked)
                {
                    _label.text = $"<color=#6A5B80><size=18>{index + 1}</size>\n🔒 Locked</color>";
                }
                else if (solved)
                {
                    string starStr = stars > 0 ? $"<color=#{ColorUtility.ToHtmlStringRGB(_theme.AccentGold)}>{new string('★', stars)}</color>" : "";
                    _label.text = $"<color=#{ColorUtility.ToHtmlStringRGB(_theme.AccentGold)}><size=18>{index + 1}</size></color>\n<size=15><b>{level.MovieTitle}</b></size>\n{starStr}";
                }
                else
                {
                    _label.text = $"<color=#{ColorUtility.ToHtmlStringRGB(_theme.NeutralLight)}><size=18>{index + 1}</size>\n<size=18><b>PLAY</b></size></color>";
                }
            }
            else
            {
                _label.text = !unlocked
                    ? $"{index + 1}\nLocked"
                    : solved
                        ? $"{index + 1}\n{level.MovieTitle}\n{new string('★', stars)}"
                        : $"{index + 1}\nPlay";
            }
        }
    }
}
