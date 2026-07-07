using System;
using BadMovieClues.Puzzle;
using NUnit.Framework;

namespace BadMovieClues.Tests
{
    public class PuzzleStateTests
    {
        [Test]
        public void InitialDisplay_MasksLettersButRevealsSpacesAndPunctuation()
        {
            var puzzle = new PuzzleState("E.T. the Extra-Terrestrial");

            Assert.AreEqual("_._. ___ _____-___________", puzzle.MaskedDisplay());
        }

        [Test]
        public void CorrectGuess_RevealsAllOccurrencesOfLetter()
        {
            var puzzle = new PuzzleState("Grease");

            var outcome = puzzle.Guess('e');

            Assert.AreEqual(GuessOutcome.Correct, outcome);
            Assert.AreEqual("__e__e", puzzle.MaskedDisplay());
        }

        [Test]
        public void IncorrectGuess_IncrementsWrongCount()
        {
            var puzzle = new PuzzleState("Jaws");

            var outcome = puzzle.Guess('x');

            Assert.AreEqual(GuessOutcome.Incorrect, outcome);
            Assert.AreEqual(1, puzzle.WrongGuessCount);
        }

        [Test]
        public void RepeatedGuess_ReturnsAlreadyGuessed_AndDoesNotChangeState()
        {
            var puzzle = new PuzzleState("Jaws");

            puzzle.Guess('a');
            var before = puzzle.MaskedDisplay();
            var wrongCountBefore = puzzle.WrongGuessCount;

            var outcome = puzzle.Guess('a');

            Assert.AreEqual(GuessOutcome.AlreadyGuessed, outcome);
            Assert.AreEqual(before, puzzle.MaskedDisplay());
            Assert.AreEqual(wrongCountBefore, puzzle.WrongGuessCount);
        }

        [Test]
        public void Guessing_IsCaseInsensitive()
        {
            var puzzle = new PuzzleState("Jaws");

            puzzle.Guess('j');
            var outcome = puzzle.Guess('J');

            Assert.AreEqual(GuessOutcome.AlreadyGuessed, outcome);
            Assert.IsTrue(puzzle.MaskedDisplay().StartsWith("J"));
        }

        [Test]
        public void Guessing_NonLetterCharacter_Throws()
        {
            var puzzle = new PuzzleState("Jaws");

            Assert.Throws<ArgumentException>(() => puzzle.Guess('2'));
            Assert.Throws<ArgumentException>(() => puzzle.Guess(' '));
            Assert.Throws<ArgumentException>(() => puzzle.Guess('-'));
        }

        [Test]
        public void TitleWithNoLetters_IsWonImmediately()
        {
            var puzzle = new PuzzleState("1984");

            Assert.IsTrue(puzzle.IsWon);
            Assert.AreEqual("1984", puzzle.MaskedDisplay());
        }

        [Test]
        public void SequelTitleWithDigit_DigitAutoRevealed_OnlyLettersNeedGuessing()
        {
            var puzzle = new PuzzleState("Toy Story 2");

            Assert.AreEqual("___ _____ 2", puzzle.MaskedDisplay());

            foreach (var letter in "TOYSR")
            {
                puzzle.Guess(letter);
            }

            Assert.IsTrue(puzzle.IsWon);
            Assert.AreEqual(0, puzzle.WrongGuessCount);
            Assert.AreEqual("Toy Story 2", puzzle.MaskedDisplay());
        }

        [Test]
        public void WonEvent_FiresExactlyOnceWhenLastLetterGuessed()
        {
            var puzzle = new PuzzleState("Jaws");
            var wonCount = 0;
            puzzle.Won += () => wonCount++;

            foreach (var letter in "JAWS") puzzle.Guess(letter);

            Assert.IsTrue(puzzle.IsWon);
            Assert.AreEqual(1, wonCount);
        }

        [Test]
        public void LostEvent_FiresExactlyOnceWhenWrongGuessesReachMax()
        {
            var puzzle = new PuzzleState("Jaws", maxWrongGuesses: 2);
            var lostCount = 0;
            puzzle.Lost += () => lostCount++;

            puzzle.Guess('x');
            puzzle.Guess('y');

            Assert.IsTrue(puzzle.IsLost);
            Assert.AreEqual(1, lostCount);
        }

        [Test]
        public void GuessingAfterGameOver_ReturnsGameAlreadyOver_AndDoesNotChangeState()
        {
            var puzzle = new PuzzleState("Jaws", maxWrongGuesses: 1);

            puzzle.Guess('x');
            Assert.IsTrue(puzzle.IsLost);

            var wrongCountBefore = puzzle.WrongGuessCount;
            var outcome = puzzle.Guess('j');

            Assert.AreEqual(GuessOutcome.GameAlreadyOver, outcome);
            Assert.AreEqual(wrongCountBefore, puzzle.WrongGuessCount);
            Assert.AreEqual("____", puzzle.MaskedDisplay());
        }

        [Test]
        public void Constructor_RejectsNullOrEmptyTitle()
        {
            Assert.Throws<ArgumentException>(() => new PuzzleState(""));
            Assert.Throws<ArgumentException>(() => new PuzzleState(null));
        }

        [Test]
        public void Constructor_RejectsNonPositiveMaxWrongGuesses()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new PuzzleState("Jaws", 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new PuzzleState("Jaws", -1));
        }
    }
}
