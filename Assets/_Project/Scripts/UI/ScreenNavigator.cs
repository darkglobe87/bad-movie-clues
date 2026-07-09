using System;
using PrimeTween;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BadMovieClues.UI
{
    public enum TransitionType
    {
        Fade,
        SlideLeft,
        SlideRight
    }

    /// <summary>
    /// Persistent scene navigator with fade and slide transition effects.
    /// Lives alongside AppRoot (DontDestroyOnLoad).
    /// </summary>
    public class ScreenNavigator : MonoBehaviour
    {
        public static ScreenNavigator Instance { get; private set; }

        private const float FadeDuration = 0.35f;
        private CanvasGroup _fadeGroup;

        private void Awake()
        {
            Instance = this;
            BuildFadeCanvas();
        }

        private void BuildFadeCanvas()
        {
            var canvasGo = new GameObject("FadeCanvas", typeof(Canvas), typeof(CanvasGroup), typeof(Image));
            canvasGo.transform.SetParent(transform, false);

            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000;

            var image = canvasGo.GetComponent<Image>();
            // Use B-movie dark top color for a styled transition
            image.color = new Color32(0x1A, 0x0E, 0x2E, 0xFF);
            var rt = (RectTransform)canvasGo.transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            _fadeGroup = canvasGo.GetComponent<CanvasGroup>();
            _fadeGroup.alpha = 0f;
            _fadeGroup.blocksRaycasts = false;
        }

        public async Awaitable LoadScene(string sceneName)
        {
            await LoadScene(sceneName, TransitionType.Fade);
        }

        public async Awaitable LoadScene(string sceneName, TransitionType transition)
        {
            try
            {
                var rt = (RectTransform)_fadeGroup.transform;
                var rect = rt.rect;
                var screenWidth = rect.width;
                if (screenWidth <= 0) screenWidth = Screen.width; // Fallback

                _fadeGroup.blocksRaycasts = true;

                if (transition == TransitionType.Fade)
                {
                    rt.anchoredPosition = Vector2.zero;
                    _fadeGroup.alpha = 0f;
                    await Tween.Alpha(_fadeGroup, endValue: 1f, duration: FadeDuration);
                }
                else if (transition == TransitionType.SlideLeft)
                {
                    _fadeGroup.alpha = 1f;
                    rt.anchoredPosition = new Vector2(screenWidth, 0f);
                    await Tween.UIAnchoredPosition(rt, endValue: Vector2.zero, duration: FadeDuration, ease: Ease.OutQuad);
                }
                else if (transition == TransitionType.SlideRight)
                {
                    _fadeGroup.alpha = 1f;
                    rt.anchoredPosition = new Vector2(-screenWidth, 0f);
                    await Tween.UIAnchoredPosition(rt, endValue: Vector2.zero, duration: FadeDuration, ease: Ease.OutQuad);
                }

                var operation = SceneManager.LoadSceneAsync(sceneName);
                while (operation != null && !operation.isDone)
                {
                    await Awaitable.NextFrameAsync();
                }

                // Wait one frame to avoid visual pop during scene initialization
                await Awaitable.NextFrameAsync();

                if (transition == TransitionType.Fade)
                {
                    await Tween.Alpha(_fadeGroup, endValue: 0f, duration: FadeDuration);
                }
                else if (transition == TransitionType.SlideLeft)
                {
                    await Tween.UIAnchoredPosition(rt, endValue: new Vector2(-screenWidth, 0f), duration: FadeDuration, ease: Ease.InQuad);
                }
                else if (transition == TransitionType.SlideRight)
                {
                    await Tween.UIAnchoredPosition(rt, endValue: new Vector2(screenWidth, 0f), duration: FadeDuration, ease: Ease.InQuad);
                }

                _fadeGroup.blocksRaycasts = false;
                _fadeGroup.alpha = 0f;
                rt.anchoredPosition = Vector2.zero;
            }
            catch (Exception e)
            {
                Debug.LogError($"[ScreenNavigator] Exception loading scene '{sceneName}': {e}");
            }
        }
    }
}
