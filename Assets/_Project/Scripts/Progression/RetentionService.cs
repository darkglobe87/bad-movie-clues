using System;
using BadMovieClues.Services;

namespace BadMovieClues.Progression
{
    /// <summary>
    /// Tracks consecutive login streaks and dispensing daily rewards.
    /// Uses strict calendar-day model: missing any UTC calendar day
    /// resets the streak to Day 1. Pure C# POCO — persists via
    /// ISaveService.
    /// </summary>
    public class RetentionService
    {
        private const string SaveKey = "retention";
        private readonly ISaveService _saveService;
        private RetentionData _data;

        public RetentionService(ISaveService saveService)
        {
            _saveService = saveService;
            if (!_saveService.TryLoad(SaveKey, out _data) || _data == null)
            {
                _data = new RetentionData();
            }
        }

        /// <summary>Current streak day (1-7). 0 means never logged in.</summary>
        public int CurrentStreakDay => _data.CurrentStreakDay;

        /// <summary>True if the player has already claimed today's reward.</summary>
        public bool HasClaimedToday
        {
            get
            {
                if (string.IsNullOrEmpty(_data.LastLoginDateUtc)) return false;
                return DateTime.TryParse(_data.LastLoginDateUtc, null,
                    System.Globalization.DateTimeStyles.RoundtripKind,
                    out var lastDate) && lastDate.Date == DateTime.UtcNow.Date;
            }
        }

        /// <summary>
        /// Call on every app launch. Returns the reward amount if this is
        /// a new calendar day and the streak advanced (or reset), or 0 if
        /// already claimed today.
        /// </summary>
        /// <param name="rewardTrack">7-element array of coin rewards for Days 1-7.</param>
        public int RecordLogin(int[] rewardTrack)
        {
            if (rewardTrack == null || rewardTrack.Length == 0)
                throw new ArgumentException("Reward track must have at least one entry.", nameof(rewardTrack));

            var today = DateTime.UtcNow.Date;

            // Already claimed today — no reward.
            if (HasClaimedToday) return 0;

            bool hadPreviousLogin = DateTime.TryParse(_data.LastLoginDateUtc, null,
                System.Globalization.DateTimeStyles.RoundtripKind,
                out var lastDate);

            if (hadPreviousLogin)
            {
                var daysDiff = (today - lastDate.Date).Days;

                if (daysDiff == 1)
                {
                    // Consecutive day — continue streak.
                    _data.CurrentStreakDay++;
                    if (_data.CurrentStreakDay > rewardTrack.Length)
                    {
                        // Wrap around after completing the full track.
                        _data.CurrentStreakDay = 1;
                    }
                }
                else if (daysDiff > 1)
                {
                    // Streak broken — reset to Day 1.
                    _data.CurrentStreakDay = 1;
                    _data.StreakBroken = true;
                }
                // daysDiff == 0 is handled by HasClaimedToday above.
                // daysDiff < 0 (clock manipulation) — treat as same day, already guarded.
            }
            else
            {
                // First-ever login.
                _data.CurrentStreakDay = 1;
                _data.StreakBroken = false;
            }

            _data.LastLoginDateUtc = today.ToString("O");
            _data.StreakBroken = hadPreviousLogin && (today - lastDate.Date).Days > 1;
            Persist();

            var rewardIndex = _data.CurrentStreakDay - 1;
            return rewardTrack[Math.Min(rewardIndex, rewardTrack.Length - 1)];
        }

        /// <summary>True if the most recent RecordLogin detected a streak break.</summary>
        public bool WasStreakBroken => _data.StreakBroken;

        /// <summary>Resets the streak entirely (but does NOT affect progress or coins).</summary>
        public void Reset()
        {
            _data = new RetentionData();
            Persist();
        }

        private void Persist() => _saveService.Save(SaveKey, _data);

        [Serializable]
        public class RetentionData
        {
            public int CurrentStreakDay;
            public string LastLoginDateUtc;
            public bool StreakBroken;
        }
    }
}
