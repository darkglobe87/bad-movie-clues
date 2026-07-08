using BadMovieClues.Services;
using NUnit.Framework;

namespace BadMovieClues.Tests
{
    public class SettingsServiceTests
    {
        [Test]
        public void NewService_WithNoSavedData_DefaultsToAudioOnReducedEffectsOff()
        {
            var settings = new SettingsService(new FakeSaveService());

            Assert.IsTrue(settings.AudioEnabled);
            Assert.IsFalse(settings.ReducedEffects);
        }

        [Test]
        public void SetReducedEffects_ChangesValue_AndFiresEvent()
        {
            var settings = new SettingsService(new FakeSaveService());
            var fired = false;
            settings.Changed += () => fired = true;

            settings.ReducedEffects = true;

            Assert.IsTrue(settings.ReducedEffects);
            Assert.IsTrue(fired);
        }

        [Test]
        public void SetAudioEnabled_ChangesValue_AndFiresEvent()
        {
            var settings = new SettingsService(new FakeSaveService());
            var fired = false;
            settings.Changed += () => fired = true;

            settings.AudioEnabled = false;

            Assert.IsFalse(settings.AudioEnabled);
            Assert.IsTrue(fired);
        }

        [Test]
        public void SettingSameValue_DoesNotFireEvent()
        {
            var settings = new SettingsService(new FakeSaveService());
            var fired = false;
            settings.Changed += () => fired = true;

            settings.AudioEnabled = true; // already true by default

            Assert.IsFalse(fired);
        }

        [Test]
        public void Settings_PersistAcrossInstances_ViaSharedSaveService()
        {
            var saveService = new FakeSaveService();
            var first = new SettingsService(saveService);
            first.ReducedEffects = true;
            first.AudioEnabled = false;

            var second = new SettingsService(saveService);

            Assert.IsTrue(second.ReducedEffects);
            Assert.IsFalse(second.AudioEnabled);
        }
    }
}
