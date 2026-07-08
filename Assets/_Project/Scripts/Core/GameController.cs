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
        private LevelCatalog _catalog;

        public ICurrencyService Currency { get; }
        public GameConfig Config { get; }

        public LevelData CurrentLevel { get; private set; }
        public PuzzleState CurrentPuzzle { get; private set; }
        public Sprite CurrentImage { get; private set; }
        public int CurrentIndex { get; private set; }

        public event Action<LevelData, Sprite> LevelLoaded;
        public event Action Won;
        public event Action Lost;

        public GameController(
            IContentProvider contentProvider,
            ICurrencyService currency,
            HintService hintService,
            GameConfig config,
            IProgressService progressService)
        {
            _contentProvider = contentProvider;
            Currency = currency;
            _hintService = hintService;
            Config = config;
            _progressService = progressService;
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
            var capturedIndex = CurrentIndex;
            CurrentPuzzle.Won += () =>
            {
                _progressService.MarkSolved(level.Id, capturedIndex);
                Won?.Invoke();
            };
            CurrentPuzzle.Lost += () => Lost?.Invoke();

            LevelLoaded?.Invoke(level, sprite);
        }

        public GuessOutcome GuessLetter(char letter)
        {
            if (CurrentPuzzle == null)
                throw new InvalidOperationException("No level loaded yet.");
            return CurrentPuzzle.Guess(letter);
        }

        public bool TryRevealPictureHint() => _hintService.TryRevealPicture();

        public bool TryRevealCharacterHint() => _hintService.TryRevealCharacterClue();

        public bool TryRevealLetterHint()
        {
            if (CurrentPuzzle == null)
                throw new InvalidOperationException("No level loaded yet.");
            return _hintService.TryRevealLetterHint(CurrentPuzzle);
        }
    }
}
