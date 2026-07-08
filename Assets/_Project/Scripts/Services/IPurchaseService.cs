using System.Collections.Generic;
using UnityEngine;

namespace BadMovieClues.Services
{
    public interface IPurchaseService
    {
        IReadOnlyList<CoinPack> Packs { get; }
        Awaitable<bool> PurchaseAsync(string packId);
        Awaitable<bool> RestorePurchasesAsync();
    }
}
