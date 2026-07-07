using BadMovieClues.Economy;
using NUnit.Framework;

namespace BadMovieClues.Tests
{
    public class CurrencyServiceTests
    {
        [Test]
        public void NewService_WithNoSavedData_UsesStartingBalance()
        {
            var currency = new CurrencyService(new FakeSaveService(), startingBalance: 42);

            Assert.AreEqual(42, currency.Balance);
        }

        [Test]
        public void Add_IncreasesBalance_AndFiresEvent()
        {
            var currency = new CurrencyService(new FakeSaveService(), startingBalance: 10);
            int? eventBalance = null;
            currency.OnBalanceChanged += b => eventBalance = b;

            currency.Add(5);

            Assert.AreEqual(15, currency.Balance);
            Assert.AreEqual(15, eventBalance);
        }

        [Test]
        public void Add_NegativeAmount_Throws()
        {
            var currency = new CurrencyService(new FakeSaveService());

            Assert.Throws<System.ArgumentOutOfRangeException>(() => currency.Add(-1));
        }

        [Test]
        public void TrySpend_SufficientBalance_Succeeds_AndFiresEvent()
        {
            var currency = new CurrencyService(new FakeSaveService(), startingBalance: 100);
            int? eventBalance = null;
            currency.OnBalanceChanged += b => eventBalance = b;

            var result = currency.TrySpend(40);

            Assert.IsTrue(result);
            Assert.AreEqual(60, currency.Balance);
            Assert.AreEqual(60, eventBalance);
        }

        [Test]
        public void TrySpend_InsufficientBalance_Fails_AndDoesNotChangeStateOrFireEvent()
        {
            var currency = new CurrencyService(new FakeSaveService(), startingBalance: 10);
            var eventFired = false;
            currency.OnBalanceChanged += _ => eventFired = true;

            var result = currency.TrySpend(11);

            Assert.IsFalse(result);
            Assert.AreEqual(10, currency.Balance);
            Assert.IsFalse(eventFired);
        }

        [Test]
        public void TrySpend_ExactBalance_Succeeds_ResultingInZero()
        {
            var currency = new CurrencyService(new FakeSaveService(), startingBalance: 25);

            Assert.IsTrue(currency.TrySpend(25));
            Assert.AreEqual(0, currency.Balance);
        }

        [Test]
        public void TrySpend_NegativeAmount_Throws()
        {
            var currency = new CurrencyService(new FakeSaveService(), startingBalance: 10);

            Assert.Throws<System.ArgumentOutOfRangeException>(() => currency.TrySpend(-5));
        }

        [Test]
        public void Balance_PersistsAcrossInstances_ViaSharedSaveService()
        {
            var saveService = new FakeSaveService();
            var first = new CurrencyService(saveService, startingBalance: 100);
            first.Add(50);

            var second = new CurrencyService(saveService, startingBalance: 999);

            Assert.AreEqual(150, second.Balance, "Should load the persisted balance, not the starting balance.");
        }
    }
}
