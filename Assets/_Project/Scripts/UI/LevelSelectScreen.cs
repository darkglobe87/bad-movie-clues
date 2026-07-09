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
    /// navigates to Gameplay. Built procedurally by MainMenuScreen.
    /// Enhanced with card slide/pop animations, grid adjustments, and detailed progress stats header.
    /// </summary>
    public class LevelSelectScreen : MonoBehaviour
    {
        private UITheme _theme;
        private AudioClip _clickSound;
        private IProgressService _progress;
        private LevelCatalog _catalog;
        private Action _onClose;
        private readonly List<LevelCard> _cards = new List<LevelCard>();
        private TextMeshProUGUI _headerText;

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

        public async void Refresh()
        {
            for (var i = 0; i < _cards.Count; i++)
            {
                var level = _catalog.Levels[i];
                var index = i;
                _cards[i].Bind(level, index, _progress.IsUnlocked(index), _progress.IsSolved(level.Id),
                    _progress.GetStars(level.Id), () => OnCardClicked(index));
            }
            
            UpdateHeader();

            // Wait one frame for the GameObject's activeInHierarchy status to propagate from the parent
            await Awaitable.NextFrameAsync();

            // Stagger scale pop for all card transforms row-by-row
            for (var i = 0; i < _cards.Count; i++)
            {
                if (_cards[i] == null) continue;
                var cardTrans = _cards[i].transform;
                cardTrans.localScale = Vector3.zero;
                float delay = (i / 3) * 0.04f;
                Tween.Scale(cardTrans, endValue: 1f, duration: 0.35f, ease: Ease.OutBack, startDelay: delay);
            }
        }

        private void Build()
        {
            BuildHeader();

            var scrollGo = new GameObject("ScrollRect", typeof(RectTransform), typeof(ScrollRect));
            scrollGo.transform.SetParent(transform, false);
            var scrollRt = (RectTransform)scrollGo.transform;
            scrollRt.anchorMin = new Vector2(0.05f, 0.15f);
            scrollRt.anchorMax = new Vector2(0.95f, 0.84f);
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
            grid.cellSize = new Vector2(260, 160);
            grid.spacing = new Vector2(16, 16);
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
                label.textWrappingMode = TextWrappingModes.Normal;

                var card = cardGo.AddComponent<LevelCard>();
                card.Setup(button, label, _theme);
                button.onClick.AddListener(() => PlayClick());
                _cards.Add(card);
            }

            BuildBackButton();
        }

        private void BuildHeader()
        {
            var headerGo = new GameObject("Header", typeof(RectTransform));
            headerGo.transform.SetParent(transform, false);
            var headerRt = (RectTransform)headerGo.transform;
            headerRt.anchorMin = new Vector2(0.05f, 0.85f);
            headerRt.anchorMax = new Vector2(0.95f, 0.96f);
            headerRt.offsetMin = headerRt.offsetMax = Vector2.zero;

            _headerText = headerGo.AddComponent<TextMeshProUGUI>();
            _headerText.alignment = TextAlignmentOptions.Center;
            _headerText.fontSize = 28;
            _headerText.fontStyle = FontStyles.Bold;
            if (_theme != null)
            {
                _headerText.color = _theme.NeutralLight;
                if (_theme.HeadingFont != null) _headerText.font = _theme.HeadingFont;
            }
            else
            {
                _headerText.color = new Color32(0xF5, 0xEC, 0xD9, 0xFF);
            }
        }

        private void UpdateHeader()
        {
            if (_headerText != null)
            {
                int totalLevels = _catalog.Levels.Count;
                int solvedCount = 0;
                int totalStars = 0;
                for (int i = 0; i < totalLevels; i++)
                {
                    var id = _catalog.Levels[i].Id;
                    if (_progress.IsSolved(id))
                    {
                        solvedCount++;
                        totalStars += _progress.GetStars(id);
                    }
                }
                
                string goldHex = _theme != null ? ColorUtility.ToHtmlStringRGB(_theme.AccentGold) : "FFC24B";
                _headerText.text = $"Choose a Level\n<size=18><color=#{goldHex}>Stars: {totalStars}</color>  |  Solved {solvedCount}/{totalLevels}</size>";
            }
        }

        private void BuildBackButton()
        {
            var backGo = new GameObject("Button_Back", typeof(RectTransform), typeof(Image), typeof(Button));
            backGo.transform.SetParent(transform, false);
            var backRt = (RectTransform)backGo.transform;
            backRt.anchorMin = new Vector2(0.3f, 0.03f);
            backRt.anchorMax = new Vector2(0.7f, 0.03f);
            backRt.anchoredPosition = new Vector2(0f, 28f);
            backRt.sizeDelta = new Vector2(0f, 56f);
            var button = backGo.GetComponent<Button>();
            var text = MainMenuScreen.UIText(backGo.transform, "< Back", 22, FontStyles.Normal);
            if (_theme != null) _theme.ApplyButton(button, backGo.GetComponent<Image>());
            if (_theme != null && _theme.BodyFont != null) text.font = _theme.BodyFont;
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
            _ = ScreenNavigator.Instance.LoadScene("Gameplay", TransitionType.SlideLeft);
        }

        private void PlayClick()
        {
            AppRoot.Instance.Haptics?.VibrateClick();
            AppRoot.Instance.AudioService.PlayOneShot(_clickSound);
        }
    }
}
