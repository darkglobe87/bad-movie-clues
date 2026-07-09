using System;
using BadMovieClues.Services;

namespace BadMovieClues.Progression
{
    /// <summary>
    /// Deterministically selects a daily puzzle from the catalog using
    /// the current UTC date as a seed, and tracks whether the player
    /// has already completed today's challenge. Pure C# POCO — no
    /// MonoBehaviour, no UnityEngine dependency beyond what ISaveService
    /// already abstracts.
    /// </summary>
    public class DailyPuzzleService
    {
        private const string SaveKey = "daily_challenge";
        private readonly ISaveService _saveService;
        private DailyChallengeData _data;

        public DailyPuzzleService(ISaveService saveService)
        {
            _saveService = saveService;
            if (!_saveService.TryLoad(SaveKey, out _data) || _data == null)
            {
                _data = new DailyChallengeData();
            }
        }

        /// <summary>
        /// Returns a catalog index for today's daily challenge. The same
        /// date always produces the same index for any given catalog size,
        /// so all players worldwide get the same puzzle each day.
        /// </summary>
        public int GetTodaysCatalogIndex(int catalogSize)
        {
            if (catalogSize <= 0)
                throw new ArgumentOutOfRangeException(nameof(catalogSize));

            var today = DateTime.UtcNow.Date;
            // Stable seed: year * 10000 + month * 100 + day avoids
            // GetHashCode platform differences across Mono/IL2CPP.
            var seed = today.Year * 10000 + today.Month * 100 + today.Day;
            var rng = new Random(seed);
            return rng.Next(catalogSize);
        }

        /// <summary>True if the player has already completed today's daily challenge.</summary>
        public bool IsCompletedToday
        {
            get
            {
                if (string.IsNullOrEmpty(_data.LastCompletedDateUtc)) return false;
                return DateTime.TryParse(_data.LastCompletedDateUtc, null,
                    System.Globalization.DateTimeStyles.RoundtripKind,
                    out var lastDate) && lastDate.Date == DateTime.UtcNow.Date;
            }
        }

        /// <summary>Marks today's daily challenge as completed and persists.</summary>
        public void MarkCompleted()
        {
            _data.LastCompletedDateUtc = DateTime.UtcNow.Date.ToString("O");
            _saveService.Save(SaveKey, _data);
        }

        [Serializable]
        public class DailyChallengeData
        {
            public string LastCompletedDateUtc;
        }
    }
}
