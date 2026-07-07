using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace BadMovieClues.Data
{
    /// <summary>
    /// Loads the starter catalog bundled with the app: the JSON comes from
    /// Resources (so it's always present, no Addressables build needed for
    /// text), images come from Addressables keyed by LevelData.ImageKey.
    /// </summary>
    public class BundledContentProvider : IContentProvider
    {
        private const string CatalogResourcePath = "StarterCatalog";

        public async Awaitable<LevelCatalog> LoadCatalogAsync()
        {
            var textAsset = Resources.Load<TextAsset>(CatalogResourcePath);
            if (textAsset == null)
            {
                throw new InvalidOperationException(
                    $"Catalog resource not found at Resources/{CatalogResourcePath}.json");
            }

            var levels = JsonConvert.DeserializeObject<List<LevelData>>(textAsset.text)
                         ?? new List<LevelData>();

            await Awaitable.NextFrameAsync();
            return new LevelCatalog(levels);
        }

        public async Awaitable<Sprite> LoadImageAsync(string imageKey)
        {
            if (string.IsNullOrEmpty(imageKey)) return null;

            var handle = Addressables.LoadAssetAsync<Sprite>(imageKey);
            await handle.Task;

            return handle.Status == AsyncOperationStatus.Succeeded ? handle.Result : null;
        }
    }
}
