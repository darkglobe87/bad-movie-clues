using System;
using System.Collections.Generic;
using System.Linq;
using BadMovieClues.Services;

namespace BadMovieClues.Progression
{
    /// <summary>
    /// Tracks solved level ids and the highest unlocked catalog index.
    /// Solving a level unlocks the next (index + 1). Mirrors
    /// CurrencyService/SettingsService's shape: save-backed via
    /// ISaveService, only fires Changed on an actual change.
    /// </summary>
    public class ProgressService : IProgressService
    {
        private const string SaveKey = "progress";
        private readonly ISaveService _saveService;
        private readonly bool _unlockAllForTesting;
        private HashSet<string> _solvedIds;
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
            }
            else
            {
                _solvedIds = new HashSet<string>();
                _highestUnlockedIndex = 0;
            }
        }

        public bool IsSolved(string levelId) => _solvedIds.Contains(levelId);

        public bool IsUnlocked(int index) => _unlockAllForTesting || index <= _highestUnlockedIndex;

        public void MarkSolved(string levelId, int index)
        {
            var changed = _solvedIds.Add(levelId);

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
            _highestUnlockedIndex = 0;
            Persist();
            Changed?.Invoke();
        }

        private void Persist() => _saveService.Save(SaveKey, new ProgressData
        {
            SolvedIds = _solvedIds.ToArray(),
            HighestUnlockedIndex = _highestUnlockedIndex,
        });

        private struct ProgressData
        {
            public string[] SolvedIds;
            public int HighestUnlockedIndex;
        }
    }
}
