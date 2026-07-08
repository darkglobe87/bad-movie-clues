using System;
using System.Collections.Generic;

namespace BadMovieClues.Progression
{
    public interface IProgressService
    {
        IReadOnlyCollection<string> SolvedIds { get; }
        int HighestUnlockedIndex { get; }
        bool IsSolved(string levelId);
        bool IsUnlocked(int index);

        /// <summary>0 if never solved. 0-3 otherwise - the best result
        /// across every playthrough of that level, never lowered by a
        /// worse replay.</summary>
        int GetStars(string levelId);

        void MarkSolved(string levelId, int index, int stars);
        void Reset();
        event Action Changed;
    }
}
