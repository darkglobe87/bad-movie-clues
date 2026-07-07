using System.Collections.Generic;
using System.Linq;

namespace BadMovieClues.Data
{
    public class LevelCatalog
    {
        public IReadOnlyList<LevelData> Levels { get; }

        public LevelCatalog(IEnumerable<LevelData> levels)
        {
            Levels = levels.ToList();
        }

        public LevelData FindById(string id)
        {
            return Levels.FirstOrDefault(level => level.Id == id);
        }
    }
}
