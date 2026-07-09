using System;
using BadMovieClues.Data;
using BadMovieClues.Economy;
using BadMovieClues.Progression;
using BadMovieClues.Puzzle;
using UnityEngine;

namespace BadMovieClues.Core
{
    /// <summary>
    /// Orchestrates loading a level from content and driving its puzzle.
    /// Plain C# (no MonoBehaviour) - the composition root (in UI, see
    /// GameBootstrap) constructs one and wires a view to its events.
    /// </summary>
    public class GameController
    {
        private readonly IContentProvider _contentProvider;
        private readonly HintService _hintService;
        private readonly IProgressService _progressService;
        private readonly bool _isDailyChallenge;
        private readonly int _dailyRewardMultiplier;
        private LevelCatalog _catalog;
        private bool _usedPictureHint;
        private bool _usedCharacterHint;
        private bool _usedLetterHint;

        public ICurrencyService Currency { get; }
        public GameConfig Config { get; }

        public LevelData CurrentLevel { get; private set; }
        public PuzzleState CurrentPuzzle { get; private set; }
        public Sprite CurrentImage { get; private set; }
        public int CurrentIndex { get; private set; }

        /// <summary>Set once Won fires - 3 minus one point per distinct hint
        /// type used this round (picture/character/letter), floored at 0.
        /// Multiple letter hints in the same round still only cost one
        /// point, matching "lose a star if a clue [type] is used."</summary>
        public int StarsEarned { get; private set; }

        public event Action<LevelData, Sprite> LevelLoaded;
        public event Action Won;
        public event Action Lost;
        public event Action LevelSkipped;

        public GameController(
            IContentProvider contentProvider,
            ICurrencyService currency,
            HintService hintService,
            GameConfig config,
            IProgressService progressService,
            bool isDailyChallenge = false,
            int dailyRewardMultiplier = 1)
        {
            _contentProvider = contentProvider;
            Currency = currency;
            _hintService = hintService;
            Config = config;
            _progressService = progressService;
            _isDailyChallenge = isDailyChallenge;
            _dailyRewardMultiplier = dailyRewardMultiplier;
        }

        public async Awaitable LoadLevelAsync(int index)
        {
            _catalog ??= await _contentProvider.LoadCatalogAsync();

            if (_catalog.Levels.Count == 0)
                throw new InvalidOperationException("Catalog has no levels.");

            CurrentIndex = index % _catalog.Levels.Count;
            var level = _catalog.Levels[CurrentIndex];
            var sprite = await _contentProvider.LoadImageAsync(level.ImageKey);

            CurrentLevel = level;
            CurrentImage = sprite;
            CurrentPuzzle = new PuzzleState(level.MovieTitle);
            _usedPictureHint = false;
            _usedCharacterHint = false;
            _usedLetterHint = false;
            StarsEarned = 0;

            var capturedIndex = CurrentIndex;
            CurrentPuzzle.Won += () =>
            {
                var hintsUsed = (_usedPictureHint ? 1 : 0) + (_usedCharacterHint ? 1 : 0) + (_usedLetterHint ? 1 : 0);
                StarsEarned = Mathf.Max(0, 3 - hintsUsed);
                _progressService.MarkSolved(level.Id, capturedIndex, StarsEarned);
                var reward = _isDailyChallenge
                    ? Config.LevelCompleteReward * _dailyRewardMultiplier
                    : Config.LevelCompleteReward;
                Currency.Add(reward);
                Won?.Invoke();
            };
            CurrentPuzzle.Lost += () => Lost?.Invoke();

            LevelLoaded?.Invoke(level, sprite);
        }

        public bool TrySkipLevel(bool useCoins)
        {
            if (CurrentPuzzle == null)
                throw new InvalidOperationException("No level loaded yet.");

            if (_isDailyChallenge)
                throw new InvalidOperationException("Daily challenges cannot be skipped.");

            if (useCoins)
            {
                if (!Currency.TrySpend(Config.SkipLevelCost))
                    return false;
            }

            StarsEarned = 0;
            _progressService.MarkSolved(CurrentLevel.Id, CurrentIndex, 0);
            LevelSkipped?.Invoke();
            return true;
        }

        public GuessOutcome GuessLetter(char letter)
        {
            if (CurrentPuzzle == null)
                throw new InvalidOperationException("No level loaded yet.");
            return CurrentPuzzle.Guess(letter);
        }

        public bool TryRevealPictureHint()
        {
            var result = _hintService.TryRevealPicture();
            if (result) _usedPictureHint = true;
            return result;
        }

        public bool TryRevealCharacterHint()
        {
            var result = _hintService.TryRevealCharacterClue();
            if (result) _usedCharacterHint = true;
            return result;
        }

        public bool TryRevealLetterHint()
        {
            if (CurrentPuzzle == null)
                throw new InvalidOperationException("No level loaded yet.");
            var result = _hintService.TryRevealLetterHint(CurrentPuzzle);
            if (result) _usedLetterHint = true;
            return result;
        }
    }
}
