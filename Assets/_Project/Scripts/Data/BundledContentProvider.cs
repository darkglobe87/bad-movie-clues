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

            // Try loading from Resources first
            var resourcePath = $"Images/{imageKey}";
            var request = Resources.LoadAsync<Sprite>(resourcePath);
            while (!request.isDone)
            {
                await Awaitable.NextFrameAsync();
            }

            if (request.asset is Sprite resourceSprite)
            {
                return resourceSprite;
            }

            try
            {
                // Loaded as Texture2D (not Sprite) because that's the type
                // Addressables actually has registered for these entries; wrap it
                // in a Sprite here rather than depend on the texture's import
                // type, which can drift from what's cached in the entry.
                var handle = Addressables.LoadAssetAsync<Texture2D>(imageKey);
                await handle.Task;

                if (handle.Status != AsyncOperationStatus.Succeeded) return null;

                var texture = handle.Result;
                var sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
                sprite.name = imageKey;
                return sprite;
            }
            catch (Exception e)
            {
                Debug.LogError($"[BundledContentProvider] Exception loading Addressable image '{imageKey}': {e}");
                return null;
            }
        }
    }
}
