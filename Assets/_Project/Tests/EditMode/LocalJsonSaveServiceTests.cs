using System;
using System.IO;
using BadMovieClues.Services;
using NUnit.Framework;

namespace BadMovieClues.Tests
{
    public class LocalJsonSaveServiceTests
    {
        private string _tempDir;

        [SetUp]
        public void SetUp()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), "BadMovieCluesTests_" + Guid.NewGuid());
            Directory.CreateDirectory(_tempDir);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_tempDir)) Directory.Delete(_tempDir, true);
        }

        [Test]
        public void SaveThenLoad_RoundTripsValue()
        {
            var service = new LocalJsonSaveService(_tempDir);

            service.Save("test_key", 42);
            var loaded = service.TryLoad("test_key", out int value);

            Assert.IsTrue(loaded);
            Assert.AreEqual(42, value);
        }

        [Test]
        public void Load_MissingKey_ReturnsFalse()
        {
            var service = new LocalJsonSaveService(_tempDir);

            var loaded = service.TryLoad("does_not_exist", out int _);

            Assert.IsFalse(loaded);
        }

        [Test]
        public void Save_OverwritesPreviousValue()
        {
            var service = new LocalJsonSaveService(_tempDir);

            service.Save("key", 1);
            service.Save("key", 2);
            service.TryLoad("key", out int value);

            Assert.AreEqual(2, value);
        }
    }
}
