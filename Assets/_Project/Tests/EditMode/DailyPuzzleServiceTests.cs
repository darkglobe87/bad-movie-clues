using System;
using System.IO;
using BadMovieClues.Progression;
using BadMovieClues.Services;
using NUnit.Framework;

namespace BadMovieClues.Tests
{
    [TestFixture]
    public class DailyPuzzleServiceTests
    {
        private string _tempDir;
        private ISaveService _saveService;

        [SetUp]
        public void SetUp()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), $"bmc_test_{Guid.NewGuid():N}");
            Directory.CreateDirectory(_tempDir);
            _saveService = new LocalJsonSaveService(_tempDir);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_tempDir))
                Directory.Delete(_tempDir, true);
        }

        [Test]
        public void SameDateSeed_ProducesSameIndex()
        {
            var svc1 = new DailyPuzzleService(_saveService);
            var svc2 = new DailyPuzzleService(_saveService);
            Assert.AreEqual(
                svc1.GetTodaysCatalogIndex(36),
                svc2.GetTodaysCatalogIndex(36));
        }

        [Test]
        public void IndexIsWithinCatalogBounds()
        {
            var svc = new DailyPuzzleService(_saveService);
            var index = svc.GetTodaysCatalogIndex(36);
            Assert.GreaterOrEqual(index, 0);
            Assert.Less(index, 36);
        }

        [Test]
        public void IndexIsWithinBounds_SmallCatalog()
        {
            var svc = new DailyPuzzleService(_saveService);
            var index = svc.GetTodaysCatalogIndex(1);
            Assert.AreEqual(0, index);
        }

        [Test]
        public void ThrowsForZeroCatalogSize()
        {
            var svc = new DailyPuzzleService(_saveService);
            Assert.Throws<ArgumentOutOfRangeException>(() => svc.GetTodaysCatalogIndex(0));
        }

        [Test]
        public void IsCompletedToday_FalseInitially()
        {
            var svc = new DailyPuzzleService(_saveService);
            Assert.IsFalse(svc.IsCompletedToday);
        }

        [Test]
        public void IsCompletedToday_TrueAfterMarkCompleted()
        {
            var svc = new DailyPuzzleService(_saveService);
            svc.MarkCompleted();
            Assert.IsTrue(svc.IsCompletedToday);
        }

        [Test]
        public void Completion_PersistsAcrossInstances()
        {
            var svc1 = new DailyPuzzleService(_saveService);
            svc1.MarkCompleted();

            // Recreate from same save
            var svc2 = new DailyPuzzleService(_saveService);
            Assert.IsTrue(svc2.IsCompletedToday);
        }
    }
}
