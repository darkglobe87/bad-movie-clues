using System;
using PrimeTween;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BadMovieClues.UI
{
    /// <summary>
    /// ~2s animated splash: logo pop-in, tagline fade, tap-anywhere-to-skip,
    /// auto-advance to MainMenu. Built procedurally, same pattern as
    /// MainMenuScreen/GameHud. Uses the same Awaitable try/catch safety net
    /// as every other async lifecycle method in this project (M6 lesson).
    /// Requires an Image on its own GameObject - IPointerClickHandler only
    /// fires where a raycastable Graphic actually gets hit, so this class
    /// makes its own full-screen invisible tap target rather than depending
    /// on exact scene setup.
    /// </summary>
    [RequireComponent(typeof(Image))]
    public class SplashScreen : MonoBehaviour, IPointerClickHandler
    {
        private const float AutoAdvanceDelay = 2f;
        private bool _advancing;

        private async void Start()
        {
            try
            {
                var canvasRoot = (RectTransform)transform;
                var tapTarget = GetComponent<Image>();
                tapTarget.color = new Color(0f, 0f, 0f, 0f);
                tapTarget.raycastTarget = true;
                MainMenuScreen.StretchFull(canvasRoot);

                var logo = MainMenuScreen.UIText(canvasRoot, "Bad Movie Clues", 60, FontStyles.Bold);
                var logoRt = logo.rectTransform;
                logoRt.anchorMin = new Vector2(0.1f, 0.5f);
                logoRt.anchorMax = new Vector2(0.9f, 0.65f);
                logoRt.offsetMin = logoRt.offsetMax = Vector2.zero;
                logo.color = new Color32(0xF5, 0xEC, 0xD9, 0xFF);
                logo.transform.localScale = Vector3.zero;

                var tagline = MainMenuScreen.UIText(canvasRoot, "a hangman game for terrible movie fans", 22, FontStyles.Italic);
                var tagRt = tagline.rectTransform;
                tagRt.anchorMin = new Vector2(0.1f, 0.42f);
                tagRt.anchorMax = new Vector2(0.9f, 0.5f);
                tagRt.offsetMin = tagRt.offsetMax = Vector2.zero;
                tagline.color = new Color32(0xF5, 0xEC, 0xD9, 0xFF);
                var tagCanvasGroup = tagline.gameObject.AddComponent<CanvasGroup>();
                tagCanvasGroup.alpha = 0f;

                await Tween.Scale(logo.transform, endValue: 1f, duration: 0.5f, ease: Ease.OutBack);
                await Tween.Alpha(tagCanvasGroup, endValue: 1f, duration: 0.4f);

                var elapsed = 0f;
                while (elapsed < AutoAdvanceDelay)
                {
                    await Awaitable.NextFrameAsync();
                    elapsed += Time.deltaTime;
                }
                Advance();
            }
            catch (Exception e)
            {
                Debug.LogError($"[SplashScreen] Exception in Start(): {e}");
            }
        }

        public void OnPointerClick(PointerEventData eventData) => Advance();

        private void Advance()
        {
            if (_advancing) return;
            _advancing = true;
            _ = ScreenNavigator.Instance.LoadScene("MainMenu");
        }
    }
}
