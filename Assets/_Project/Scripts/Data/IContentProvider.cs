using UnityEngine;

namespace BadMovieClues.Data
{
    /// <summary>
    /// Abstraction over where level content comes from. BundledContentProvider
    /// reads local JSON + Addressables now; RemoteContentProvider (M6) fetches
    /// from a URL behind the same interface, so call sites never change.
    /// </summary>
    public interface IContentProvider
    {
        Awaitable<LevelCatalog> LoadCatalogAsync();

        /// <summary>Returns null if imageKey is empty or the asset can't be resolved.</summary>
        Awaitable<Sprite> LoadImageAsync(string imageKey);
    }
}
