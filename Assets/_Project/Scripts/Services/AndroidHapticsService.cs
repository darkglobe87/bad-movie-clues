using UnityEngine;

namespace BadMovieClues.Services
{
    /// <summary>
    /// Provides Android native haptic vibration utilizing the Android Java Bridge.
    /// Safely falls back to no-op in Editor and Handheld.Vibrate on other platforms.
    /// </summary>
    public class AndroidHapticsService : IHapticsService
    {
        public bool Enabled { get; set; } = true;

        public void VibrateClick()
        {
            if (!Enabled) return;
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (var currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
                using (var vibrator = currentActivity.Call<AndroidJavaObject>("getSystemService", "vibrator"))
                {
                    if (vibrator != null)
                    {
                        // 15ms short pulse for key click
                        vibrator.Call("vibrate", 15L);
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[AndroidHapticsService] Vibrate click failed: {e.Message}");
            }
#endif
        }

        public void VibrateWin()
        {
            if (!Enabled) return;
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (var currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
                using (var vibrator = currentActivity.Call<AndroidJavaObject>("getSystemService", "vibrator"))
                {
                    if (vibrator != null)
                    {
                        // Pattern: [delay, vibrate, sleep, vibrate]
                        long[] pattern = { 0, 100, 50, 150 };
                        vibrator.Call("vibrate", pattern, -1);
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[AndroidHapticsService] Vibrate win failed: {e.Message}");
            }
#else
            Handheld.Vibrate();
#endif
        }

        public void VibrateFailure()
        {
            if (!Enabled) return;
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (var currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
                using (var vibrator = currentActivity.Call<AndroidJavaObject>("getSystemService", "vibrator"))
                {
                    if (vibrator != null)
                    {
                        // Single 250ms buzz for wrong answer / game over
                        vibrator.Call("vibrate", 250L);
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[AndroidHapticsService] Vibrate failure failed: {e.Message}");
            }
#else
            Handheld.Vibrate();
#endif
        }
    }
}
