using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;

namespace BadMovieClues.EditorTools
{
    /// <summary>
    /// Registers a curated "bad art" image as an Addressable asset under a
    /// stable address (the movie's imageKey). Used by the movie authoring
    /// tool whenever an image is assigned to a level. Deliberately does not
    /// touch the texture import type - BundledContentProvider loads these as
    /// Texture2D (matching what Addressables actually has cached for the
    /// entry) and wraps the result in a Sprite itself, rather than depending
    /// on import-type/type-cache timing.
    /// </summary>
    public static class AddressableImageUtility
    {
        private const string GroupName = "BadMovieClues-Images";

        public static void EnsureAddressable(string assetPath, string address)
        {
            if (string.IsNullOrEmpty(assetPath) || string.IsNullOrEmpty(address)) return;

            var settings = AddressableAssetSettingsDefaultObject.GetSettings(true);
            var group = settings.FindGroup(GroupName) ?? settings.CreateGroup(
                GroupName, false, false, true, null,
                typeof(BundledAssetGroupSchema), typeof(ContentUpdateGroupSchema));

            var guid = AssetDatabase.AssetPathToGUID(assetPath);
            var entry = settings.CreateOrMoveEntry(guid, group);
            entry.address = address;
        }
    }
}
