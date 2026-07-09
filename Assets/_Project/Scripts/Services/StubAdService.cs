using System;
using UnityEngine;

namespace BadMovieClues.Services
{
    /// <summary>
    /// Mock advertisement service. Simulates a 1.5-second ad playback
    /// with logs and returns success to enable testing without live SDK setup.
    /// </summary>
    public class StubAdService : IAdService
    {
        public bool IsAdAvailable => true;
        public event Action AdAvailabilityChanged;

        public async void ShowRewardedAd(Action<bool> onComplete)
        {
            Debug.Log("[StubAdService] Ad started playback. Simulating 1.5s delay...");
            
            // Wait 1.5 seconds to simulate ad duration
            try
            {
                await Awaitable.WaitForSecondsAsync(1.5f);
                Debug.Log("[StubAdService] Ad finished playback successfully.");
                onComplete?.Invoke(true);
            }
            catch (Exception e)
            {
                Debug.LogError($"[StubAdService] Ad simulation error: {e}");
                onComplete?.Invoke(false);
            }
        }
    }
}
