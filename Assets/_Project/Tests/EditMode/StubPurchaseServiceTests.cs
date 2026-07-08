using System.Collections;
using BadMovieClues.Economy;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace BadMovieClues.Tests
{
    // [UnityTest] rather than [Test]: PurchaseAsync returns Awaitable<bool>,
    // the first async-returning method any test in this project needs to
    // await. Pumping the awaiter via `yield return null` between checks lets
    // the player loop actually advance, unlike the Thread.Sleep spin-wait
    // that deadlocked a throwaway batch script back in M8 - same underlying
    // lesson (don't block the thread that's supposed to drive completion),
    // applied here via the test framework's own coroutine support instead.
    public class StubPurchaseServiceTests
    {
        [UnityTest]
        public IEnumerator PurchaseAsync_ValidPack_AddsExactCoins_AndReturnsTrue()
        {
            var currency = new CurrencyService(new FakeSaveService(), startingBalance: 0);
            var purchases = new StubPurchaseService(currency);

            var awaiter = purchases.PurchaseAsync("coins_small").GetAwaiter();
            while (!awaiter.IsCompleted) yield return null;

            Assert.IsTrue(awaiter.GetResult());
            Assert.AreEqual(500, currency.Balance);
        }

        [UnityTest]
        public IEnumerator PurchaseAsync_UnknownPackId_ReturnsFalse_AndDoesNotChangeBalance()
        {
            var currency = new CurrencyService(new FakeSaveService(), startingBalance: 100);
            var purchases = new StubPurchaseService(currency);

            var awaiter = purchases.PurchaseAsync("not_a_real_pack").GetAwaiter();
            while (!awaiter.IsCompleted) yield return null;

            Assert.IsFalse(awaiter.GetResult());
            Assert.AreEqual(100, currency.Balance);
        }

        [Test]
        public void Packs_ContainsThreeConfiguredPacks()
        {
            var purchases = new StubPurchaseService(new CurrencyService(new FakeSaveService()));

            Assert.AreEqual(3, purchases.Packs.Count);
        }

        [UnityTest]
        public IEnumerator RestorePurchasesAsync_ReturnsTrue()
        {
            var purchases = new StubPurchaseService(new CurrencyService(new FakeSaveService()));

            var awaiter = purchases.RestorePurchasesAsync().GetAwaiter();
            while (!awaiter.IsCompleted) yield return null;

            Assert.IsTrue(awaiter.GetResult());
        }
    }
}
