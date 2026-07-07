using System;

namespace BadMovieClues.Economy
{
    public interface ICurrencyService
    {
        int Balance { get; }
        void Add(int amount);
        bool TrySpend(int amount);
        event Action<int> OnBalanceChanged;
    }
}
