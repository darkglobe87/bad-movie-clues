using System;
using UnityEngine;

namespace BadMovieClues.Services
{
    public interface IAdService
    {
        bool IsAdAvailable { get; }
        event Action AdAvailabilityChanged;
        
        /// <summary>
        /// Shows a rewarded video ad. Invokes onComplete callback with true if completed, false if skipped/failed.
        /// </summary>
        void ShowRewardedAd(Action<bool> onComplete);
    }
}
