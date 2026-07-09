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

        private RectTransform _leftCurtain;
        private RectTransform _rightCurtain;
        private CanvasGroup _fadeGroup;

        private void Awake()
        {
            Instance = this;
            BuildFadeCanvas();
        }

        private void BuildFadeCanvas()
        {
            var canvasGo = new GameObject("FadeCanvas", typeof(Canvas), typeof(CanvasGroup));
            canvasGo.transform.SetParent(transform, false);

            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000;

            _fadeGroup = canvasGo.GetComponent<CanvasGroup>();
            _fadeGroup.alpha = 0f;
            _fadeGroup.blocksRaycasts = false;

            var fadeRt = (RectTransform)canvasGo.transform;
            fadeRt.anchorMin = Vector2.zero;
            fadeRt.anchorMax = Vector2.one;
            fadeRt.offsetMin = Vector2.zero;
            fadeRt.offsetMax = Vector2.zero;

            // Left Curtain (Pivot on the right edge so it slides left/right from center)
            var leftGo = new GameObject("LeftCurtain", typeof(RectTransform), typeof(Image));
            leftGo.transform.SetParent(canvasGo.transform, false);
            _leftCurtain = (RectTransform)leftGo.transform;
            _leftCurtain.anchorMin = new Vector2(0f, 0f);
            _leftCurtain.anchorMax = new Vector2(0.5f, 1f);
            _leftCurtain.pivot = new Vector2(1f, 0.5f);
            _leftCurtain.offsetMin = _leftCurtain.offsetMax = Vector2.zero;
            
            var leftImg = leftGo.GetComponent<Image>();
            leftImg.sprite = ProceduralIcons.RoundedRect;
            leftImg.type = Image.Type.Sliced;
            leftImg.color = new Color32(0x80, 0x10, 0x20, 0xFF); // Velvet Red

            // Right Curtain (Pivot on the left edge so it slides left/right from center)
            var rightGo = new GameObject("RightCurtain", typeof(RectTransform), typeof(Image));
            rightGo.transform.SetParent(canvasGo.transform, false);
            _rightCurtain = (RectTransform)rightGo.transform;
            _rightCurtain.anchorMin = new Vector2(0.5f, 0f);
            _rightCurtain.anchorMax = new Vector2(1f, 1f);
            _rightCurtain.pivot = new Vector2(0f, 0.5f);
            _rightCurtain.offsetMin = _rightCurtain.offsetMax = Vector2.zero;
            
            var rightImg = rightGo.GetComponent<Image>();
            rightImg.sprite = ProceduralIcons.RoundedRect;
            rightImg.type = Image.Type.Sliced;
            rightImg.color = new Color32(0x80, 0x10, 0x20, 0xFF); // Velvet Red

            // Gold border trims
            AddGoldTrim(leftGo.transform, true);
            AddGoldTrim(rightGo.transform, false);
        }

        private void AddGoldTrim(Transform parent, bool isLeft)
        {
            var trimGo = new GameObject("GoldTrim", typeof(RectTransform), typeof(Image));
            trimGo.transform.SetParent(parent, false);
            var trimRt = (RectTransform)trimGo.transform;
            trimRt.anchorMin = isLeft ? new Vector2(1f, 0f) : new Vector2(0f, 0f);
            trimRt.anchorMax = isLeft ? new Vector2(1f, 1f) : new Vector2(0f, 1f);
            trimRt.pivot = isLeft ? new Vector2(1f, 0.5f) : new Vector2(0f, 0.5f);
            trimRt.sizeDelta = new Vector2(8f, 0f);
            trimRt.offsetMin = new Vector2(trimRt.offsetMin.x, 0f);
            trimRt.offsetMax = new Vector2(trimRt.offsetMax.x, 0f);
            
            var img = trimGo.GetComponent<Image>();
            img.color = new Color32(0xFF, 0xC2, 0x4B, 0xFF); // Marquee Gold
        }

        public async Awaitable LoadScene(string sceneName)
        {
            await LoadScene(sceneName, TransitionType.Fade);
        }

        public async Awaitable LoadScene(string sceneName, TransitionType transition)
        {
            Debug.Log($"[ScreenNavigator] LoadScene('{sceneName}') called with transition: {transition}");
            try
            {
                var rt = (RectTransform)_fadeGroup.transform;
                var rect = rt.rect;
                var screenWidth = rect.width;
                if (screenWidth <= 0) screenWidth = Screen.width; // Fallback
                var halfWidth = screenWidth * 0.5f;

                Debug.Log($"[ScreenNavigator] Screen width: {screenWidth}, Half width: {halfWidth}");
                _fadeGroup.blocksRaycasts = true;
                _fadeGroup.alpha = 1f;

                // Close the curtains: slide from offscreen sides to the center
                Debug.Log("[ScreenNavigator] Setting curtains to offscreen position...");
                _leftCurtain.anchoredPosition = new Vector2(-halfWidth, 0f);
                _rightCurtain.anchoredPosition = new Vector2(halfWidth, 0f);

                Debug.Log("[ScreenNavigator] Creating close curtains tween...");
                var closeSequence = Sequence.Create()
                    .Group(Tween.UIAnchoredPosition(_leftCurtain, endValue: Vector2.zero, duration: 0.35f, ease: Ease.OutQuad))
                    .Group(Tween.UIAnchoredPosition(_rightCurtain, endValue: Vector2.zero, duration: 0.35f, ease: Ease.OutQuad));

                Debug.Log("[ScreenNavigator] Awaiting close curtains tween sequence...");
                await closeSequence;
                Debug.Log("[ScreenNavigator] Close curtains tween finished!");

                Debug.Log($"[ScreenNavigator] Loading scene '{sceneName}' asynchronously...");
                var operation = SceneManager.LoadSceneAsync(sceneName);
                while (operation != null && !operation.isDone)
                {
                    await Awaitable.NextFrameAsync();
                }
                Debug.Log($"[ScreenNavigator] Scene '{sceneName}' load operation completed!");

                // Wait one frame to avoid visual pop during scene initialization
                await Awaitable.NextFrameAsync();

                // Open the curtains: slide back offscreen
                Debug.Log("[ScreenNavigator] Creating open curtains tween...");
                var openSequence = Sequence.Create()
                    .Group(Tween.UIAnchoredPosition(_leftCurtain, endValue: new Vector2(-halfWidth, 0f), duration: 0.35f, ease: Ease.InQuad))
                    .Group(Tween.UIAnchoredPosition(_rightCurtain, endValue: new Vector2(halfWidth, 0f), duration: 0.35f, ease: Ease.InQuad));

                Debug.Log("[ScreenNavigator] Awaiting open curtains tween sequence...");
                await openSequence;
                Debug.Log("[ScreenNavigator] Open curtains tween finished!");

                _fadeGroup.blocksRaycasts = false;
                _fadeGroup.alpha = 0f;
                Debug.Log($"[ScreenNavigator] LoadScene('{sceneName}') completed successfully!");
            }
            catch (Exception e)
            {
                Debug.LogError($"[ScreenNavigator] Exception loading scene '{sceneName}': {e}");
            }
        }
    }
}
