using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using BadMovieClues.Data;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;

namespace BadMovieClues.EditorTools
{
    /// <summary>
    /// Curation tool for the starter catalog: add/edit/delete movies, assign
    /// bad-art images (wiring them into Addressables automatically), and
    /// save back to the bundled catalog JSON. See CLAUDE.md for the schema.
    /// </summary>
    public class MovieAuthoringWindow : EditorWindow
    {
        private const string CatalogPath = "Assets/_Project/Content/Resources/StarterCatalog.json";
        private const string ImagesFolder = "Assets/_Project/Content/Images";

        private List<LevelData> _levels = new List<LevelData>();
        private Vector2 _listScroll;
        private Vector2 _formScroll;
        private int _selectedIndex = -1;
        private bool _dirty;

        [MenuItem("Bad Movie Clues/Movie Catalog Editor")]
        public static void Open()
        {
            GetWindow<MovieAuthoringWindow>("Movie Catalog");
        }

        private void OnEnable()
        {
            LoadCatalog();
        }

        private void LoadCatalog()
        {
            _levels = File.Exists(CatalogPath)
                ? JsonConvert.DeserializeObject<List<LevelData>>(File.ReadAllText(CatalogPath)) ?? new List<LevelData>()
                : new List<LevelData>();
            _dirty = false;
            _selectedIndex = _levels.Count > 0 ? 0 : -1;
        }

        private void SaveCatalog()
        {
            var json = JsonConvert.SerializeObject(_levels, Formatting.Indented);
            File.WriteAllText(CatalogPath, json);
            AssetDatabase.ImportAsset(CatalogPath);
            _dirty = false;
        }

        private void OnGUI()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                DrawList();
                DrawForm();
            }

            EditorGUILayout.Space();
            using (new EditorGUILayout.HorizontalScope())
            {
                GUI.enabled = _dirty;
                if (GUILayout.Button("Save Catalog", GUILayout.Height(28))) SaveCatalog();
                GUI.enabled = true;

                if (GUILayout.Button("Reload From Disk", GUILayout.Height(28))) LoadCatalog();

                if (GUILayout.Button("Sync All Images To Addressables", GUILayout.Height(28)))
                    SyncAllImages();
            }
        }

        private void SyncAllImages()
        {
            var synced = 0;
            foreach (var level in _levels)
            {
                if (string.IsNullOrEmpty(level.ImageKey)) continue;
                var texture = FindImageByKey(level.ImageKey);
                if (texture == null) continue;
                AddressableImageUtility.EnsureAddressable(AssetDatabase.GetAssetPath(texture), level.ImageKey);
                synced++;
            }
            AssetDatabase.SaveAssets();
            Debug.Log($"[MovieAuthoringWindow] Synced {synced} image(s) to Addressables.");
        }

        private void DrawList()
        {
            using (new EditorGUILayout.VerticalScope(GUILayout.Width(220)))
            {
                EditorGUILayout.LabelField($"Movies ({_levels.Count})", EditorStyles.boldLabel);
                _listScroll = EditorGUILayout.BeginScrollView(_listScroll, GUILayout.ExpandHeight(true));
                for (var i = 0; i < _levels.Count; i++)
                {
                    var label = string.IsNullOrEmpty(_levels[i].MovieTitle) ? "(untitled)" : _levels[i].MovieTitle;
                    var style = i == _selectedIndex ? EditorStyles.miniButtonMid : EditorStyles.label;
                    if (GUILayout.Button(label, style)) _selectedIndex = i;
                }
                EditorGUILayout.EndScrollView();

                if (GUILayout.Button("+ New Movie"))
                {
                    _levels.Add(new LevelData
                    {
                        Id = "new-movie",
                        MovieTitle = "New Movie",
                        Tags = Array.Empty<string>(),
                        Difficulty = 1
                    });
                    _selectedIndex = _levels.Count - 1;
                    _dirty = true;
                }
            }
        }

        private void DrawForm()
        {
            using (new EditorGUILayout.VerticalScope())
            {
                if (_selectedIndex < 0 || _selectedIndex >= _levels.Count)
                {
                    EditorGUILayout.HelpBox("Select or create a movie.", MessageType.Info);
                    return;
                }

                var level = _levels[_selectedIndex];
                _formScroll = EditorGUILayout.BeginScrollView(_formScroll);
                EditorGUI.BeginChangeCheck();

                level.Id = EditorGUILayout.TextField("Id (slug)", level.Id);
                level.MovieTitle = EditorGUILayout.TextField("Movie Title", level.MovieTitle);
                EditorGUILayout.LabelField("Bad Description");
                level.BadDescription = EditorGUILayout.TextArea(level.BadDescription, GUILayout.Height(50));
                level.CharacterClue = EditorGUILayout.TextField("Character Clue", level.CharacterClue);
                level.PlotHint = EditorGUILayout.TextField("Plot Hint (optional)", level.PlotHint);
                level.SceneHint = EditorGUILayout.TextField("Scene Hint (optional)", level.SceneHint);
                level.Difficulty = EditorGUILayout.IntSlider("Difficulty", level.Difficulty, 1, 5);

                var tagsJoined = string.Join(", ", level.Tags ?? Array.Empty<string>());
                var newTagsJoined = EditorGUILayout.TextField("Tags (comma separated)", tagsJoined);
                if (newTagsJoined != tagsJoined)
                {
                    level.Tags = newTagsJoined.Split(',')
                        .Select(t => t.Trim())
                        .Where(t => t.Length > 0)
                        .ToArray();
                }

                DrawImagePicker(level);

                if (EditorGUI.EndChangeCheck()) _dirty = true;

                EditorGUILayout.Space();
                if (GUILayout.Button("Delete This Movie"))
                {
                    _levels.RemoveAt(_selectedIndex);
                    _selectedIndex = Mathf.Clamp(_selectedIndex - 1, -1, _levels.Count - 1);
                    _dirty = true;
                    EditorGUILayout.EndScrollView();
                    GUIUtility.ExitGUI();
                }

                EditorGUILayout.EndScrollView();
            }
        }

        private void DrawImagePicker(LevelData level)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Image", EditorStyles.boldLabel);

            var currentTexture = FindImageByKey(level.ImageKey);
            var pickedTexture = (Texture2D)EditorGUILayout.ObjectField(
                "Bad Art Image", currentTexture, typeof(Texture2D), false);

            if (pickedTexture != currentTexture)
            {
                if (pickedTexture == null)
                {
                    level.ImageKey = "";
                }
                else
                {
                    var path = AssetDatabase.GetAssetPath(pickedTexture);
                    if (!path.StartsWith(ImagesFolder))
                    {
                        Debug.LogWarning($"Pick an image from {ImagesFolder} so it stays addressable by filename.");
                    }
                    else
                    {
                        level.ImageKey = Path.GetFileNameWithoutExtension(path);
                        AddressableImageUtility.EnsureAddressable(path, level.ImageKey);
                    }
                }
                _dirty = true;
            }

            if (currentTexture != null)
            {
                var rect = GUILayoutUtility.GetRect(128, 128, GUILayout.ExpandWidth(false));
                EditorGUI.DrawPreviewTexture(rect, currentTexture);
            }
            else if (!string.IsNullOrEmpty(level.ImageKey))
            {
                EditorGUILayout.HelpBox(
                    $"No image file found in {ImagesFolder} matching key '{level.ImageKey}'.",
                    MessageType.Warning);
            }
        }

        private static Texture2D FindImageByKey(string imageKey)
        {
            if (string.IsNullOrEmpty(imageKey)) return null;

            var guids = AssetDatabase.FindAssets($"{imageKey} t:Texture2D", new[] { ImagesFolder });
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (Path.GetFileNameWithoutExtension(path) == imageKey)
                    return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            }
            return null;
        }
    }
}
