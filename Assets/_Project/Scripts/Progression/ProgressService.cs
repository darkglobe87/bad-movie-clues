using System;
using System.Collections.Generic;
using System.Linq;
using BadMovieClues.Services;

namespace BadMovieClues.Progression
{
    /// <summary>
    /// Tracks solved level ids, per-level star rating, and the highest
    /// unlocked catalog index. Solving a level unlocks the next
    /// (index + 1). Mirrors CurrencyService/SettingsService's shape:
    /// save-backed via ISaveService, only fires Changed on an actual
    /// change.
    /// </summary>
    public class ProgressService : IProgressService
    {
        private const string SaveKey = "progress";
        private readonly ISaveService _saveService;
        private readonly bool _unlockAllForTesting;
        private HashSet<string> _solvedIds;
        private Dictionary<string, int> _stars;
        private int _highestUnlockedIndex;

        public IReadOnlyCollection<string> SolvedIds => _solvedIds;
        public int HighestUnlockedIndex => _highestUnlockedIndex;
        public event Action Changed;

        public ProgressService(ISaveService saveService, bool unlockAllForTesting = false)
        {
            _saveService = saveService;
            _unlockAllForTesting = unlockAllForTesting;

            if (_saveService.TryLoad(SaveKey, out ProgressData loaded))
            {
                _solvedIds = new HashSet<string>(loaded.SolvedIds ?? Array.Empty<string>());
                _highestUnlockedIndex = loaded.HighestUnlockedIndex;
                _stars = loaded.Stars ?? new Dictionary<string, int>();
            }
            else
            {
                _solvedIds = new HashSet<string>();
                _highestUnlockedIndex = 0;
                _stars = new Dictionary<string, int>();
            }
        }

        public bool IsSolved(string levelId) => _solvedIds.Contains(levelId);

        public bool IsUnlocked(int index) => _unlockAllForTesting || index <= _highestUnlockedIndex;

        public int GetStars(string levelId) => _stars.TryGetValue(levelId, out var stars) ? stars : 0;

        public void MarkSolved(string levelId, int index, int stars)
        {
            var changed = _solvedIds.Add(levelId);

            // Never let a worse replay lower a previously-earned rating.
            if (!_stars.TryGetValue(levelId, out var existingStars) || stars > existingStars)
            {
                _stars[levelId] = stars;
                changed = true;
            }

            var newHighest = Math.Max(_highestUnlockedIndex, index + 1);
            if (newHighest != _highestUnlockedIndex)
            {
                _highestUnlockedIndex = newHighest;
                changed = true;
            }

            if (!changed) return;
            Persist();
            Changed?.Invoke();
        }

        public void Reset()
        {
            _solvedIds = new HashSet<string>();
            _stars = new Dictionary<string, int>();
            _highestUnlockedIndex = 0;
            Persist();
            Changed?.Invoke();
        }

        private void Persist() => _saveService.Save(SaveKey, new ProgressData
        {
            SolvedIds = _solvedIds.ToArray(),
            HighestUnlockedIndex = _highestUnlockedIndex,
            Stars = _stars,
        });

        private struct ProgressData
        {
            public string[] SolvedIds;
            public int HighestUnlockedIndex;
            public Dictionary<string, int> Stars;
        }
    }
}
