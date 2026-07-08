using System;

namespace BadMovieClues.Economy
{
    public interface ICurrencyService
    {
        int Balance { get; }
        void Add(int amount);
        bool TrySpend(int amount);
        void Reset(int startingBalance);
        event Action<int> OnBalanceChanged;
    }
}
