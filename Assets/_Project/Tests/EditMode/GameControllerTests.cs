using System;
using System.Collections.Generic;
using BadMovieClues.Core;
using BadMovieClues.Data;
using BadMovieClues.Economy;
using BadMovieClues.Progression;
using BadMovieClues.Services;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace BadMovieClues.Tests
{
    public class GameControllerTests
    {
        private GameConfig _config;
        private FakeSaveService _saveService;
        private CurrencyService _currency;
        private HintService _hintService;
        private ProgressService _progress;
        private FakeContentProvider _contentProvider;
        private LevelCatalog _catalog;

        [SetUp]
        public void Setup()
        {
            _config = ScriptableObject.CreateInstance<GameConfig>();
            _config.StartingBalance = 100;
            _config.SkipLevelCost = 100;
            _config.LevelCompleteReward = 20;

            _saveService = new FakeSaveService();
            _currency = new CurrencyService(_saveService, _config.StartingBalance);
            _hintService = new HintService(_currency, _config);
            _progress = new ProgressService(_saveService);

            var levels = new List<LevelData>
            {
                new LevelData { Id = "oz", MovieTitle = "The Wizard of Oz", ImageKey = "" },
                new LevelData { Id = "batman", MovieTitle = "Batman", ImageKey = "" }
            };
            _catalog = new LevelCatalog(levels);
            _contentProvider = new FakeContentProvider(_catalog);
        }

        [UnityTest]
        public System.Collections.IEnumerator TrySkipLevel_WithCoins_DeductsCoinsAndAdvancesProgress()
        {
            var controller = new GameController(_contentProvider, _currency, _hintService, _config, _progress);
            var loadTask = controller.LoadLevelAsync(0);
            
            // Wait for async level load
            var awaiter = loadTask.GetAwaiter();
            while (!awaiter.IsCompleted) yield return null;

            Assert.AreEqual("oz", controller.CurrentLevel.Id);
            Assert.AreEqual(100, _currency.Balance);
            Assert.AreEqual(0, _progress.HighestUnlockedIndex);

            var skippedFired = false;
            controller.LevelSkipped += () => skippedFired = true;

            var result = controller.TrySkipLevel(useCoins: true);

            Assert.IsTrue(result);
            Assert.IsTrue(skippedFired);
            Assert.AreEqual(0, _currency.Balance, "Skip cost of 100 should be deducted.");
            Assert.IsTrue(_progress.IsSolved("oz"), "Skipped level should be marked solved.");
            Assert.AreEqual(0, _progress.GetStars("oz"), "Skipped level should receive 0 stars.");
            Assert.AreEqual(1, _progress.HighestUnlockedIndex, "Next level should be unlocked.");
        }

        [UnityTest]
        public System.Collections.IEnumerator TrySkipLevel_WithAd_AdvancesProgressWithoutDeductingCoins()
        {
            var controller = new GameController(_contentProvider, _currency, _hintService, _config, _progress);
            var loadTask = controller.LoadLevelAsync(0);
            
            var awaiter = loadTask.GetAwaiter();
            while (!awaiter.IsCompleted) yield return null;

            var skippedFired = false;
            controller.LevelSkipped += () => skippedFired = true;

            var result = controller.TrySkipLevel(useCoins: false);

            Assert.IsTrue(result);
            Assert.IsTrue(skippedFired);
            Assert.AreEqual(100, _currency.Balance, "No coins should be deducted when skipping via Ad.");
            Assert.IsTrue(_progress.IsSolved("oz"));
            Assert.AreEqual(0, _progress.GetStars("oz"));
            Assert.AreEqual(1, _progress.HighestUnlockedIndex);
        }

        [UnityTest]
        public System.Collections.IEnumerator TrySkipLevel_InsufficientCoins_Fails()
        {
            _currency.TrySpend(50); // Balance becomes 50
            var controller = new GameController(_contentProvider, _currency, _hintService, _config, _progress);
            var loadTask = controller.LoadLevelAsync(0);
            
            var awaiter = loadTask.GetAwaiter();
            while (!awaiter.IsCompleted) yield return null;

            var skippedFired = false;
            controller.LevelSkipped += () => skippedFired = true;

            var result = controller.TrySkipLevel(useCoins: true);

            Assert.IsFalse(result);
            Assert.IsFalse(skippedFired);
            Assert.AreEqual(50, _currency.Balance);
            Assert.IsFalse(_progress.IsSolved("oz"));
        }

        [UnityTest]
        public System.Collections.IEnumerator TrySkipLevel_DailyChallenge_ThrowsException()
        {
            var controller = new GameController(_contentProvider, _currency, _hintService, _config, _progress, isDailyChallenge: true);
            var loadTask = controller.LoadLevelAsync(0);
            
            var awaiter = loadTask.GetAwaiter();
            while (!awaiter.IsCompleted) yield return null;

            Assert.Throws<InvalidOperationException>(() => controller.TrySkipLevel(useCoins: false));
        }

        private class FakeContentProvider : IContentProvider
        {
            private readonly LevelCatalog _catalog;

            public FakeContentProvider(LevelCatalog catalog)
            {
                _catalog = catalog;
            }

            public async Awaitable<LevelCatalog> LoadCatalogAsync()
            {
                return _catalog;
            }

            public async Awaitable<Sprite> LoadImageAsync(string imageKey)
            {
                return null;
            }
        }
    }
}
