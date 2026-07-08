using System;
using System.Collections.Generic;
using BadMovieClues.Data;
using BadMovieClues.Progression;
using PrimeTween;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BadMovieClues.UI
{
    /// <summary>
    /// Scrollable grid of all catalog movies showing solved/locked/unlocked
    /// state. Selecting an unlocked card sets AppRoot.SelectedLevelIndex and
    /// navigates to Gameplay. Built procedurally by MainMenuScreen, same
    /// pattern as SettingsScreen - LevelCard sizing uses LayoutElement for
    /// the same reason SettingsScreen's toggles/buttons do (see that file's
    /// summary comment for the underlying sizeDelta-vs-ControlHeight lesson).
    /// </summary>
    public class LevelSelectScreen : MonoBehaviour
    {
        private UITheme _theme;
        private AudioClip _clickSound;
        private IProgressService _progress;
        private LevelCatalog _catalog;
        private Action _onClose;
        private readonly List<LevelCard> _cards = new List<LevelCard>();

        public void Init(UITheme theme, AudioClip clickSound, IProgressService progress,
            LevelCatalog catalog, Action onClose)
        {
            _theme = theme;
            _clickSound = clickSound;
            _progress = progress;
            _catalog = catalog;
            _onClose = onClose;
            Build();
        }

        public void Refresh()
        {
            for (var i = 0; i < _cards.Count; i++)
            {
                var level = _catalog.Levels[i];
                var index = i;
                _cards[i].Bind(level, index, _progress.IsUnlocked(index), _progress.IsSolved(level.Id),
                    () => OnCardClicked(index));
            }
        }

        private void Build()
        {
            var scrollGo = new GameObject("ScrollRect", typeof(RectTransform), typeof(ScrollRect));
            scrollGo.transform.SetParent(transform, false);
            var scrollRt = (RectTransform)scrollGo.transform;
            scrollRt.anchorMin = new Vector2(0.05f, 0.15f);
            scrollRt.anchorMax = new Vector2(0.95f, 0.85f);
            scrollRt.offsetMin = scrollRt.offsetMax = Vector2.zero;

            var viewportGo = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(RectMask2D));
            viewportGo.transform.SetParent(scrollGo.transform, false);
            MainMenuScreen.StretchFull((RectTransform)viewportGo.transform);
            viewportGo.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.001f);

            var contentGo = new GameObject("Content", typeof(RectTransform), typeof(GridLayoutGroup), typeof(ContentSizeFitter));
            contentGo.transform.SetParent(viewportGo.transform, false);
            var contentRt = (RectTransform)contentGo.transform;
            contentRt.anchorMin = new Vector2(0f, 1f);
            contentRt.anchorMax = new Vector2(1f, 1f);
            contentRt.pivot = new Vector2(0.5f, 1f);
            contentRt.offsetMin = new Vector2(0f, contentRt.offsetMin.y);
            contentRt.offsetMax = new Vector2(0f, contentRt.offsetMax.y);

            var grid = contentGo.GetComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(150, 90);
            grid.spacing = new Vector2(10, 10);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 3;
            grid.childAlignment = TextAnchor.UpperCenter;

            var fitter = contentGo.GetComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var scrollRect = scrollGo.GetComponent<ScrollRect>();
            scrollRect.content = contentRt;
            scrollRect.viewport = (RectTransform)viewportGo.transform;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;

            for (var i = 0; i < _catalog.Levels.Count; i++)
            {
                var cardGo = new GameObject($"Card_{i}", typeof(RectTransform), typeof(Image), typeof(Button));
                cardGo.transform.SetParent(contentGo.transform, false);
                var button = cardGo.GetComponent<Button>();
                if (_theme != null) _theme.ApplyButton(button, cardGo.GetComponent<Image>());

                var label = MainMenuScreen.UIText(cardGo.transform, "", 16, FontStyles.Normal);
                MainMenuScreen.StretchFull(label.rectTransform);
                label.enableWordWrapping = true;

                var card = cardGo.AddComponent<LevelCard>();
                card.Setup(button, label);
                button.onClick.AddListener(() => PlayClick());
                _cards.Add(card);
            }

            BuildBackButton();
        }

        private void BuildBackButton()
        {
            var backGo = new GameObject("Button_Back", typeof(RectTransform), typeof(Image), typeof(Button));
            backGo.transform.SetParent(transform, false);
            var backRt = (RectTransform)backGo.transform;
            backRt.anchorMin = new Vector2(0.3f, 0.03f);
            backRt.anchorMax = new Vector2(0.7f, 0.12f);
            backRt.offsetMin = backRt.offsetMax = Vector2.zero;
            var button = backGo.GetComponent<Button>();
            if (_theme != null) _theme.ApplyButton(button, backGo.GetComponent<Image>());
            var text = MainMenuScreen.UIText(backGo.transform, "< Back", 22, FontStyles.Normal);
            MainMenuScreen.StretchFull(text.rectTransform);
            button.onClick.AddListener(() =>
            {
                PlayClick();
                _onClose();
            });
        }

        private void OnCardClicked(int index)
        {
            AppRoot.Instance.SelectedLevelIndex = index;
            _ = ScreenNavigator.Instance.LoadScene("Gameplay");
        }

        private void PlayClick() => AppRoot.Instance.AudioService.PlayOneShot(_clickSound);
    }
}
