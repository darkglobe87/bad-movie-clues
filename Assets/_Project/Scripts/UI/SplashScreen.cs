using System;
using PrimeTween;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BadMovieClues.UI
{
    /// <summary>
    /// ~2s animated splash: logo staggered pop-in, tagline slide-up fade,
    /// micro-confetti burst, tap-anywhere-to-skip, and auto-advance to MainMenu.
    /// Built procedurally under AppRoot's persistent ambient background.
    /// </summary>
    [RequireComponent(typeof(Image))]
    public class SplashScreen : MonoBehaviour, IPointerClickHandler
    {
        private const float AutoAdvanceDelay = 2.5f;
        
        [SerializeField] private UITheme theme;
        
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

                // Pulsing spotlight background glow behind title
                var spotlightGo = new GameObject("Spotlight", typeof(RectTransform), typeof(Image));
                spotlightGo.transform.SetParent(canvasRoot, false);
                var spotRt = (RectTransform)spotlightGo.transform;
                spotRt.anchorMin = new Vector2(0.5f, 0.5f);
                spotRt.anchorMax = new Vector2(0.5f, 0.5f);
                spotRt.sizeDelta = new Vector2(500f, 500f);
                spotRt.anchoredPosition = new Vector2(0f, 50f);
                
                var spotImg = spotlightGo.GetComponent<Image>();
                spotImg.sprite = ProceduralIcons.RoundedRect;
                spotImg.color = new Color32(0xFF, 0x4E, 0x8B, 0x1A); // Soft AccentMagenta glow
                
                _ = Tween.Scale(spotlightGo.transform, endValue: 1.25f, duration: 2.5f, cycles: -1, cycleMode: CycleMode.Yoyo, ease: Ease.InOutSine);
                _ = Tween.Color(spotImg, endValue: new Color32(0xFF, 0x4E, 0x8B, 0x05), duration: 2.5f, cycles: -1, cycleMode: CycleMode.Yoyo, ease: Ease.InOutSine);

                // Add chasing marquee border lights around the splash screen edges (removed per user request)

                // Staggered letters title container
                var titleContainer = new GameObject("TitleContainer", typeof(RectTransform), typeof(HorizontalLayoutGroup));
                titleContainer.transform.SetParent(canvasRoot, false);
                var containerRt = (RectTransform)titleContainer.transform;
                containerRt.anchorMin = new Vector2(0.05f, 0.5f);
                containerRt.anchorMax = new Vector2(0.95f, 0.65f);
                containerRt.offsetMin = containerRt.offsetMax = Vector2.zero;

                var layout = titleContainer.GetComponent<HorizontalLayoutGroup>();
                layout.spacing = 1f;
                layout.childAlignment = TextAnchor.MiddleCenter;
                layout.childControlWidth = true;
                layout.childControlHeight = true;
                layout.childForceExpandWidth = false;
                layout.childForceExpandHeight = true;

                string titleText = "Bad Movie Clues";
                var letterGos = new GameObject[titleText.Length];

                Debug.Log("[SplashScreen] Building staggered letter objects...");
                for (int i = 0; i < titleText.Length; i++)
                {
                    char c = titleText[i];
                    var letterText = MainMenuScreen.UIText(titleContainer.transform, c.ToString(), 60, FontStyles.Bold);
                    letterText.color = theme != null ? theme.NeutralLight : new Color32(0xF5, 0xEC, 0xD9, 0xFF);
                    if (theme != null && theme.HeadingFont != null) letterText.font = theme.HeadingFont;
                    
                    // Add subtle dark outline for pop
                    letterText.outlineWidth = 0.2f;
                    letterText.outlineColor = theme != null ? theme.BackgroundTop : new Color32(0x2A, 0x1A, 0x3E, 0xFF);
                    
                    // Add neon flickering effect to each letter
                    letterText.gameObject.AddComponent<NeonMarquee>();
                    
                    letterText.transform.localScale = Vector3.zero;
                    letterGos[i] = letterText.gameObject;
                }

                // Tagline slides up and fades in
                var tagline = MainMenuScreen.UIText(canvasRoot, "a hangman game for terrible movie fans", 22, FontStyles.Italic);
                if (theme != null && theme.BodyFont != null) tagline.font = theme.BodyFont;
                var tagRt = tagline.rectTransform;
                tagRt.anchorMin = new Vector2(0.1f, 0.4f);
                tagRt.anchorMax = new Vector2(0.9f, 0.48f);
                tagRt.offsetMin = tagRt.offsetMax = Vector2.zero;
                tagline.color = theme != null ? theme.NeutralLight : new Color32(0xF5, 0xEC, 0xD9, 0xFF);
                
                var tagCanvasGroup = tagline.gameObject.AddComponent<CanvasGroup>();
                tagCanvasGroup.alpha = 0f;
                tagline.transform.localPosition = new Vector3(0f, -40f, 0f);

                // Play cinematic letter stagger-in sequence
                Debug.Log("[SplashScreen] Starting letter stagger-in scale tweens...");
                for (int i = 0; i < titleText.Length; i++)
                {
                    _ = Tween.Scale(letterGos[i].transform, endValue: 1f, duration: 0.4f, ease: Ease.OutBack, startDelay: i * 0.04f);
                }

                // Wait for letters to fully stagger in
                Debug.Log("[SplashScreen] Waiting 1.0s for letters stagger-in...");
                var elapsedIntro = 0f;
                while (elapsedIntro < 1.0f)
                {
                    await Awaitable.NextFrameAsync();
                    elapsedIntro += Time.deltaTime;
                }

                // Confetti celebration burst
                Debug.Log("[SplashScreen] Playing ConfettiBurst...");
                ConfettiBurst.Play(canvasRoot, 25);

                // Slide tagline up & fade in
                _ = Tween.LocalPosition(tagline.transform, endValue: Vector3.zero, duration: 0.6f, ease: Ease.OutCubic);
                await Tween.Alpha(tagCanvasGroup, endValue: 1f, duration: 0.6f);

                // Main delay before auto-advance
                Debug.Log($"[SplashScreen] Waiting {AutoAdvanceDelay}s before auto-advancing...");
                var elapsed = 0f;
                while (elapsed < AutoAdvanceDelay)
                {
                    await Awaitable.NextFrameAsync();
                    elapsed += Time.deltaTime;
                }
                Debug.Log("[SplashScreen] Delay completed. Advancing...");
                Advance();
            }
            catch (Exception e)
            {
                Debug.LogError($"[SplashScreen] Exception in Start(): {e}");
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            Debug.Log("[SplashScreen] Tap skip detected. Advancing...");
            Advance();
        }

        private void Advance()
        {
            if (_advancing) return;
            _advancing = true;
            Debug.Log("[SplashScreen] Calling ScreenNavigator.LoadScene('MainMenu')");
            _ = ScreenNavigator.Instance.LoadScene("MainMenu");
        }
    }
}
