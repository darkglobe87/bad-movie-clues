using System.Collections.Generic;
using BadMovieClues.Services;
using UnityEngine;

namespace BadMovieClues.Economy
{
    /// <summary>
    /// Stub IAP - grants coins immediately, always succeeds. Real Play
    /// Billing integration (M15) plugs in behind IPurchaseService with no
    /// call-site changes.
    ///
    /// Lives in Economy, not Services, despite IPurchaseService itself
    /// living in Services - it needs ICurrencyService to grant coins, and
    /// Economy already references Services (not the reverse), so this is
    /// the only direction that doesn't invert/circularize the dependency
    /// graph. Same class of deviation as GameBootstrap living in UI
    /// instead of Core (M3): the interface stays where the plan put it,
    /// the concrete implementation goes wherever the dependency direction
    /// actually allows it to compile.
    /// </summary>
    public class StubPurchaseService : IPurchaseService
    {
        private readonly ICurrencyService _currency;
        private readonly List<CoinPack> _packs;

        public IReadOnlyList<CoinPack> Packs => _packs;

        public StubPurchaseService(ICurrencyService currency)
        {
            _currency = currency;
            _packs = new List<CoinPack>
            {
                new CoinPack("coins_small", 500, "500 Coins"),
                new CoinPack("coins_medium", 1200, "1200 Coins"),
                new CoinPack("coins_large", 3000, "3000 Coins"),
            };
        }

        public async Awaitable<bool> PurchaseAsync(string packId)
        {
            if (Application.isPlaying)
            {
                await Awaitable.NextFrameAsync();
            }
            var pack = _packs.Find(p => p.Id == packId);
            if (pack.Id == null) return false;

            _currency.Add(pack.Coins);
            return true;
        }

        public async Awaitable<bool> RestorePurchasesAsync()
        {
            if (Application.isPlaying)
            {
                await Awaitable.NextFrameAsync();
            }
            // Stub: no real store backend to restore from yet. Always
            // succeeds - matches the "no-op stub until the real SDK lands"
            // pattern used by every other service in this project.
            return true;
        }
    }
}
