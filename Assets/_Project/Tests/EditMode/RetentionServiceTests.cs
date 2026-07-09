using System;
using System.IO;
using BadMovieClues.Progression;
using BadMovieClues.Services;
using NUnit.Framework;

namespace BadMovieClues.Tests
{
    [TestFixture]
    public class RetentionServiceTests
    {
        private string _tempDir;
        private ISaveService _saveService;
        private readonly int[] _rewards = { 10, 15, 20, 30, 40, 50, 100 };

        [SetUp]
        public void SetUp()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), $"bmc_retention_test_{Guid.NewGuid():N}");
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
        public void FirstLogin_SetsDay1_ReturnsFirstReward()
        {
            var service = new RetentionService(_saveService);
            Assert.AreEqual(0, service.CurrentStreakDay);
            Assert.IsFalse(service.HasClaimedToday);

            int reward = service.RecordLogin(_rewards);
            Assert.AreEqual(1, service.CurrentStreakDay);
            Assert.IsTrue(service.HasClaimedToday);
            Assert.AreEqual(_rewards[0], reward);
            Assert.IsFalse(service.WasStreakBroken);
        }

        [Test]
        public void SameDayLogin_ReturnsZero_DoesNotAdvanceStreak()
        {
            var service = new RetentionService(_saveService);
            service.RecordLogin(_rewards);

            // Second login same day
            int reward = service.RecordLogin(_rewards);
            Assert.AreEqual(1, service.CurrentStreakDay);
            Assert.AreEqual(0, reward);
        }

        [Test]
        public void Reset_ClearsState()
        {
            var service = new RetentionService(_saveService);
            service.RecordLogin(_rewards);
            service.Reset();

            Assert.AreEqual(0, service.CurrentStreakDay);
            Assert.IsFalse(service.HasClaimedToday);
        }
    }
}
