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
        void MarkSolved(string levelId, int index);
        void Reset();
        event Action Changed;
    }
}
