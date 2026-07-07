using System;
using BadMovieClues.Data;
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
        private LevelCatalog _catalog;

        public LevelData CurrentLevel { get; private set; }
        public PuzzleState CurrentPuzzle { get; private set; }
        public Sprite CurrentImage { get; private set; }

        public event Action<LevelData, Sprite> LevelLoaded;
        public event Action Won;
        public event Action Lost;

        public GameController(IContentProvider contentProvider)
        {
            _contentProvider = contentProvider;
        }

        public async Awaitable LoadLevelAsync(int index)
        {
            _catalog ??= await _contentProvider.LoadCatalogAsync();

            if (_catalog.Levels.Count == 0)
                throw new InvalidOperationException("Catalog has no levels.");

            var level = _catalog.Levels[index % _catalog.Levels.Count];
            var sprite = await _contentProvider.LoadImageAsync(level.ImageKey);

            CurrentLevel = level;
            CurrentImage = sprite;
            CurrentPuzzle = new PuzzleState(level.MovieTitle);
            CurrentPuzzle.Won += () => Won?.Invoke();
            CurrentPuzzle.Lost += () => Lost?.Invoke();

            LevelLoaded?.Invoke(level, sprite);
        }

        public GuessOutcome GuessLetter(char letter)
        {
            if (CurrentPuzzle == null)
                throw new InvalidOperationException("No level loaded yet.");
            return CurrentPuzzle.Guess(letter);
        }
    }
}
