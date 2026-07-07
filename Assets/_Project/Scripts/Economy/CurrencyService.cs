using System;
using BadMovieClues.Services;

namespace BadMovieClues.Economy
{
    public class CurrencyService : ICurrencyService
    {
        private const string SaveKey = "coin_balance";
        private readonly ISaveService _saveService;
        private int _balance;

        public int Balance => _balance;
        public event Action<int> OnBalanceChanged;

        public CurrencyService(ISaveService saveService, int startingBalance = 0)
        {
            _saveService = saveService;
            _balance = _saveService.TryLoad(SaveKey, out int loaded) ? loaded : startingBalance;
        }

        public void Add(int amount)
        {
            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount), "Amount must not be negative.");

            _balance += amount;
            Persist();
            OnBalanceChanged?.Invoke(_balance);
        }

        public bool TrySpend(int amount)
        {
            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount), "Amount must not be negative.");
            if (amount > _balance) return false;

            _balance -= amount;
            Persist();
            OnBalanceChanged?.Invoke(_balance);
            return true;
        }

        private void Persist() => _saveService.Save(SaveKey, _balance);
    }
}
