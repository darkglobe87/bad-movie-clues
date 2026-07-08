using BadMovieClues.Progression;
using NUnit.Framework;

namespace BadMovieClues.Tests
{
    public class ProgressServiceTests
    {
        [Test]
        public void NewService_WithNoSavedData_OnlyLevelZeroUnlocked()
        {
            var progress = new ProgressService(new FakeSaveService());

            Assert.AreEqual(0, progress.HighestUnlockedIndex);
            Assert.IsTrue(progress.IsUnlocked(0));
            Assert.IsFalse(progress.IsUnlocked(1));
            Assert.IsFalse(progress.IsSolved("wizard-of-oz"));
        }

        [Test]
        public void MarkSolved_UnlocksNextLevel_AndFiresEvent()
        {
            var progress = new ProgressService(new FakeSaveService());
            var fired = false;
            progress.Changed += () => fired = true;

            progress.MarkSolved("wizard-of-oz", 0);

            Assert.IsTrue(progress.IsSolved("wizard-of-oz"));
            Assert.IsTrue(progress.IsUnlocked(1));
            Assert.IsFalse(progress.IsUnlocked(2));
            Assert.IsTrue(fired);
        }

        [Test]
        public void MarkSolved_SameLevelTwice_DoesNotFireEventSecondTime()
        {
            var progress = new ProgressService(new FakeSaveService());
            progress.MarkSolved("wizard-of-oz", 0);
            var fired = false;
            progress.Changed += () => fired = true;

            progress.MarkSolved("wizard-of-oz", 0);

            Assert.IsFalse(fired);
        }

        [Test]
        public void MarkSolved_OutOfOrderLevel_DoesNotLowerHighestUnlocked()
        {
            var progress = new ProgressService(new FakeSaveService());
            progress.MarkSolved("batman", 5);

            progress.MarkSolved("wizard-of-oz", 0);

            Assert.AreEqual(6, progress.HighestUnlockedIndex, "Solving an earlier level shouldn't lower progress from a later one.");
        }

        [Test]
        public void UnlockAllForTesting_UnlocksEveryIndex()
        {
            var progress = new ProgressService(new FakeSaveService(), unlockAllForTesting: true);

            Assert.IsTrue(progress.IsUnlocked(35));
        }

        [Test]
        public void Reset_ClearsSolvedAndUnlocked_AndFiresEvent()
        {
            var progress = new ProgressService(new FakeSaveService());
            progress.MarkSolved("wizard-of-oz", 0);
            var fired = false;
            progress.Changed += () => fired = true;

            progress.Reset();

            Assert.AreEqual(0, progress.HighestUnlockedIndex);
            Assert.IsFalse(progress.IsSolved("wizard-of-oz"));
            Assert.IsFalse(progress.IsUnlocked(1));
            Assert.IsTrue(fired);
        }

        [Test]
        public void Progress_PersistsAcrossInstances_ViaSharedSaveService()
        {
            var saveService = new FakeSaveService();
            var first = new ProgressService(saveService);
            first.MarkSolved("wizard-of-oz", 0);

            var second = new ProgressService(saveService);

            Assert.IsTrue(second.IsSolved("wizard-of-oz"));
            Assert.IsTrue(second.IsUnlocked(1));
        }
    }
}
