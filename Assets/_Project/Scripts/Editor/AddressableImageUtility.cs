using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;

namespace BadMovieClues.EditorTools
{
    /// <summary>
    /// Ensures a curated "bad art" image is set up correctly for runtime
    /// loading: imported as a Sprite and registered as an Addressable asset
    /// under a stable address (the movie's imageKey). Used by the movie
    /// authoring tool whenever an image is assigned to a level.
    /// </summary>
    public static class AddressableImageUtility
    {
        private const string GroupName = "BadMovieClues-Images";

        public static void EnsureAddressable(string assetPath, string address)
        {
            if (string.IsNullOrEmpty(assetPath) || string.IsNullOrEmpty(address)) return;

            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer != null && importer.textureType != TextureImporterType.Sprite)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.SaveAndReimport();
            }

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
