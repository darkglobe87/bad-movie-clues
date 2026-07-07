using System.Linq;
using BadMovieClues.Economy;
using BadMovieClues.Puzzle;
using NUnit.Framework;
using UnityEngine;

namespace BadMovieClues.Tests
{
    public class HintServiceTests
    {
        private static GameConfig MakeConfig(int picture = 50, int character = 30, int letter = 15)
        {
            var config = ScriptableObject.CreateInstance<GameConfig>();
            config.PictureHintCost = picture;
            config.CharacterHintCost = character;
            config.LetterHintCost = letter;
            return config;
        }

        [Test]
        public void TryRevealPicture_SufficientFunds_SpendsAndSucceeds()
        {
            var currency = new CurrencyService(new FakeSaveService(), 100);
            var hints = new HintService(currency, MakeConfig(picture: 50));

            var result = hints.TryRevealPicture();

            Assert.IsTrue(result);
            Assert.AreEqual(50, currency.Balance);
        }

        [Test]
        public void TryRevealPicture_InsufficientFunds_Fails_NoBalanceChange()
        {
            var currency = new CurrencyService(new FakeSaveService(), 10);
            var hints = new HintService(currency, MakeConfig(picture: 50));

            var result = hints.TryRevealPicture();

            Assert.IsFalse(result);
            Assert.AreEqual(10, currency.Balance);
        }

        [Test]
        public void TryRevealCharacterClue_SpendsCorrectCost()
        {
            var currency = new CurrencyService(new FakeSaveService(), 100);
            var hints = new HintService(currency, MakeConfig(character: 30));

            Assert.IsTrue(hints.TryRevealCharacterClue());
            Assert.AreEqual(70, currency.Balance);
        }

        [Test]
        public void TryRevealLetterHint_RevealsAHiddenLetter_AndSpendsCost()
        {
            var currency = new CurrencyService(new FakeSaveService(), 100);
            var hints = new HintService(currency, MakeConfig(letter: 15));
            var puzzle = new PuzzleState("Jaws");

            var result = hints.TryRevealLetterHint(puzzle);

            Assert.IsTrue(result);
            Assert.AreEqual(85, currency.Balance);
            Assert.AreEqual(1, puzzle.GuessedLetters.Count);
        }

        [Test]
        public void TryRevealLetterHint_NoHiddenLettersLeft_FailsWithoutSpending()
        {
            var currency = new CurrencyService(new FakeSaveService(), 100);
            var hints = new HintService(currency, MakeConfig(letter: 15));
            var puzzle = new PuzzleState("Jaws");
            foreach (var l in "JAWS") puzzle.Guess(l);

            var result = hints.TryRevealLetterHint(puzzle);

            Assert.IsFalse(result);
            Assert.AreEqual(100, currency.Balance, "Nothing left to hint - should not have charged coins.");
        }

        [Test]
        public void TryRevealLetterHint_InsufficientFunds_Fails_NoChange()
        {
            var currency = new CurrencyService(new FakeSaveService(), 5);
            var hints = new HintService(currency, MakeConfig(letter: 15));
            var puzzle = new PuzzleState("Jaws");

            var result = hints.TryRevealLetterHint(puzzle);

            Assert.IsFalse(result);
            Assert.AreEqual(5, currency.Balance);
            Assert.AreEqual(0, puzzle.GuessedLetters.Count);
        }

        [Test]
        public void TryRevealLetterHint_OnlyOneHiddenLetterLeft_RevealsExactlyThatOneAndWins()
        {
            var currency = new CurrencyService(new FakeSaveService(), 100);
            var hints = new HintService(currency, MakeConfig(letter: 15));
            var puzzle = new PuzzleState("Jaws");
            puzzle.Guess('J');
            puzzle.Guess('A');
            puzzle.Guess('W');

            var result = hints.TryRevealLetterHint(puzzle);

            Assert.IsTrue(result);
            Assert.IsTrue(puzzle.GuessedLetters.Contains('S'));
            Assert.IsTrue(puzzle.IsWon);
        }
    }
}
