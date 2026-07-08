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
            Assert.AreEqual(0, progress.GetStars("wizard-of-oz"));
        }

        [Test]
        public void MarkSolved_UnlocksNextLevel_RecordsStars_AndFiresEvent()
        {
            var progress = new ProgressService(new FakeSaveService());
            var fired = false;
            progress.Changed += () => fired = true;

            progress.MarkSolved("wizard-of-oz", 0, stars: 3);

            Assert.IsTrue(progress.IsSolved("wizard-of-oz"));
            Assert.IsTrue(progress.IsUnlocked(1));
            Assert.IsFalse(progress.IsUnlocked(2));
            Assert.AreEqual(3, progress.GetStars("wizard-of-oz"));
            Assert.IsTrue(fired);
        }

        [Test]
        public void MarkSolved_SameLevelSameStarsTwice_DoesNotFireEventSecondTime()
        {
            var progress = new ProgressService(new FakeSaveService());
            progress.MarkSolved("wizard-of-oz", 0, stars: 3);
            var fired = false;
            progress.Changed += () => fired = true;

            progress.MarkSolved("wizard-of-oz", 0, stars: 3);

            Assert.IsFalse(fired);
        }

        [Test]
        public void MarkSolved_WorseReplay_DoesNotLowerStars()
        {
            var progress = new ProgressService(new FakeSaveService());
            progress.MarkSolved("wizard-of-oz", 0, stars: 3);

            progress.MarkSolved("wizard-of-oz", 0, stars: 1);

            Assert.AreEqual(3, progress.GetStars("wizard-of-oz"), "A worse replay shouldn't lower a previously-earned rating.");
        }

        [Test]
        public void MarkSolved_BetterReplay_RaisesStars_AndFiresEvent()
        {
            var progress = new ProgressService(new FakeSaveService());
            progress.MarkSolved("wizard-of-oz", 0, stars: 1);
            var fired = false;
            progress.Changed += () => fired = true;

            progress.MarkSolved("wizard-of-oz", 0, stars: 3);

            Assert.AreEqual(3, progress.GetStars("wizard-of-oz"));
            Assert.IsTrue(fired);
        }

        [Test]
        public void MarkSolved_OutOfOrderLevel_DoesNotLowerHighestUnlocked()
        {
            var progress = new ProgressService(new FakeSaveService());
            progress.MarkSolved("batman", 5, stars: 3);

            progress.MarkSolved("wizard-of-oz", 0, stars: 3);

            Assert.AreEqual(6, progress.HighestUnlockedIndex, "Solving an earlier level shouldn't lower progress from a later one.");
        }

        [Test]
        public void UnlockAllForTesting_UnlocksEveryIndex()
        {
            var progress = new ProgressService(new FakeSaveService(), unlockAllForTesting: true);

            Assert.IsTrue(progress.IsUnlocked(35));
        }

        [Test]
        public void Reset_ClearsSolvedUnlockedAndStars_AndFiresEvent()
        {
            var progress = new ProgressService(new FakeSaveService());
            progress.MarkSolved("wizard-of-oz", 0, stars: 3);
            var fired = false;
            progress.Changed += () => fired = true;

            progress.Reset();

            Assert.AreEqual(0, progress.HighestUnlockedIndex);
            Assert.IsFalse(progress.IsSolved("wizard-of-oz"));
            Assert.IsFalse(progress.IsUnlocked(1));
            Assert.AreEqual(0, progress.GetStars("wizard-of-oz"));
            Assert.IsTrue(fired);
        }

        [Test]
        public void Progress_PersistsAcrossInstances_ViaSharedSaveService()
        {
            var saveService = new FakeSaveService();
            var first = new ProgressService(saveService);
            first.MarkSolved("wizard-of-oz", 0, stars: 2);

            var second = new ProgressService(saveService);

            Assert.IsTrue(second.IsSolved("wizard-of-oz"));
            Assert.IsTrue(second.IsUnlocked(1));
            Assert.AreEqual(2, second.GetStars("wizard-of-oz"));
        }
    }
}
