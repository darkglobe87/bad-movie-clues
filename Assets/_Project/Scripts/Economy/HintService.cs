using System;
using System.Collections.Generic;
using System.Linq;
using BadMovieClues.Puzzle;

namespace BadMovieClues.Economy
{
    /// <summary>
    /// Spends coins to reveal the three hint types. Picture/character hints
    /// are pure gating (the UI decides what "revealed" looks like); the
    /// letter hint actually picks a random hidden letter and guesses it on
    /// the puzzle's behalf, guaranteed correct so it never costs a wrong guess.
    /// </summary>
    public class HintService
    {
        private readonly ICurrencyService _currency;
        private readonly GameConfig _config;
        private readonly Random _random;

        public HintService(ICurrencyService currency, GameConfig config, Random random = null)
        {
            _currency = currency;
            _config = config;
            _random = random ?? new Random();
        }

        public bool TryRevealPicture() => _currency.TrySpend(_config.PictureHintCost);

        public bool TryRevealCharacterClue() => _currency.TrySpend(_config.CharacterHintCost);

        public bool TryRevealLetterHint(PuzzleState puzzle)
        {
            var hidden = GetHiddenLetters(puzzle);
            if (hidden.Count == 0) return false;
            if (!_currency.TrySpend(_config.LetterHintCost)) return false;

            puzzle.Guess(hidden[_random.Next(hidden.Count)]);
            return true;
        }

        private static List<char> GetHiddenLetters(PuzzleState puzzle)
        {
            var hidden = new List<char>();
            foreach (var c in puzzle.Title)
            {
                if (!char.IsLetter(c)) continue;
                var upper = char.ToUpperInvariant(c);
                if (!puzzle.GuessedLetters.Contains(upper) && !hidden.Contains(upper))
                    hidden.Add(upper);
            }
            return hidden;
        }
    }
}
