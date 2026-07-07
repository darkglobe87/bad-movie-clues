using System.Collections.Generic;
using BadMovieClues.Services;

namespace BadMovieClues.Tests
{
    /// <summary>In-memory ISaveService for tests, shared by
    /// CurrencyServiceTests and HintServiceTests so neither hits disk.</summary>
    internal class FakeSaveService : ISaveService
    {
        private readonly Dictionary<string, object> _store = new Dictionary<string, object>();

        public bool TryLoad<T>(string key, out T value)
        {
            if (_store.TryGetValue(key, out var raw) && raw is T typed)
            {
                value = typed;
                return true;
            }
            value = default;
            return false;
        }

        public void Save<T>(string key, T value) => _store[key] = value;
    }
}
