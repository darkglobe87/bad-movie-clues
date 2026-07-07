using System;
using System.Collections.Generic;
using System.Linq;

namespace BadMovieClues.Puzzle
{
    public enum GuessOutcome
    {
        Correct,
        Incorrect,
        AlreadyGuessed,
        GameAlreadyOver
    }

    /// <summary>
    /// Pure hangman-style guessing logic for a single movie title. No Unity
    /// dependency by design (BadMovieClues.Puzzle.asmdef has
    /// noEngineReferences: true) so it's fully unit-testable in isolation.
    /// Only letters are guessable - spaces, punctuation, and digits are
    /// revealed from the start, since there's no key for them on the
    /// on-screen A-Z keyboard.
    /// </summary>
    public class PuzzleState
    {
        private readonly string _title;
        private readonly bool[] _revealed;
        private readonly HashSet<char> _guessedLetters = new HashSet<char>();

        public string Title => _title;
        public int MaxWrongGuesses { get; }
        public int WrongGuessCount { get; private set; }
        public IReadOnlyCollection<char> GuessedLetters => _guessedLetters;

        public bool IsWon { get; private set; }
        public bool IsLost { get; private set; }
        public bool IsOver => IsWon || IsLost;

        public event Action Won;
        public event Action Lost;

        public PuzzleState(string title, int maxWrongGuesses = 8)
        {
            if (string.IsNullOrEmpty(title))
                throw new ArgumentException("Title must not be null or empty.", nameof(title));
            if (maxWrongGuesses <= 0)
                throw new ArgumentOutOfRangeException(nameof(maxWrongGuesses));

            _title = title;
            MaxWrongGuesses = maxWrongGuesses;
            _revealed = new bool[title.Length];

            for (var i = 0; i < title.Length; i++)
            {
                if (!char.IsLetter(title[i])) _revealed[i] = true;
            }

            // A title with no letters at all (e.g. "1984") is trivially won.
            if (_revealed.All(r => r)) IsWon = true;
        }

        public GuessOutcome Guess(char letter)
        {
            if (!char.IsLetter(letter))
                throw new ArgumentException($"'{letter}' is not a guessable letter.", nameof(letter));

            if (IsOver) return GuessOutcome.GameAlreadyOver;

            var normalized = char.ToUpperInvariant(letter);
            if (_guessedLetters.Contains(normalized)) return GuessOutcome.AlreadyGuessed;
            _guessedLetters.Add(normalized);

            var found = false;
            for (var i = 0; i < _title.Length; i++)
            {
                if (!char.IsLetter(_title[i])) continue;
                if (char.ToUpperInvariant(_title[i]) != normalized) continue;
                _revealed[i] = true;
                found = true;
            }

            if (found)
            {
                if (_revealed.All(r => r))
                {
                    IsWon = true;
                    Won?.Invoke();
                }
                return GuessOutcome.Correct;
            }

            WrongGuessCount++;
            if (WrongGuessCount >= MaxWrongGuesses)
            {
                IsLost = true;
                Lost?.Invoke();
            }
            return GuessOutcome.Incorrect;
        }

        public string MaskedDisplay(char blank = '_')
        {
            var chars = new char[_title.Length];
            for (var i = 0; i < _title.Length; i++)
            {
                chars[i] = _revealed[i] ? _title[i] : blank;
            }
            return new string(chars);
        }
    }
}
