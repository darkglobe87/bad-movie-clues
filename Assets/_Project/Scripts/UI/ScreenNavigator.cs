using System;
using PrimeTween;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BadMovieClues.UI
{
    /// <summary>
    /// Persistent scene navigator with a simple black fade transition. Lives
    /// alongside AppRoot (both DontDestroyOnLoad) so the fade overlay
    /// survives the scene load it's covering.
    /// </summary>
    public class ScreenNavigator : MonoBehaviour
    {
        public static ScreenNavigator Instance { get; private set; }

        private const float FadeDuration = 0.3f;
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
            canvas.sortingOrder = 1000; // always render on top of scene-local canvases

            var image = canvasGo.GetComponent<Image>();
            image.color = Color.black;
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
            // Same lesson as GameBootstrap.Start(): guard against a silently
            // swallowed exception thrown before the first await.
            try
            {
                _fadeGroup.blocksRaycasts = true;
                await Tween.Alpha(_fadeGroup, endValue: 1f, duration: FadeDuration);

                var operation = SceneManager.LoadSceneAsync(sceneName);
                while (operation != null && !operation.isDone)
                {
                    await Awaitable.NextFrameAsync();
                }

                await Tween.Alpha(_fadeGroup, endValue: 0f, duration: FadeDuration);
                _fadeGroup.blocksRaycasts = false;
            }
            catch (Exception e)
            {
                Debug.LogError($"[ScreenNavigator] Exception loading scene '{sceneName}': {e}");
            }
        }
    }
}
