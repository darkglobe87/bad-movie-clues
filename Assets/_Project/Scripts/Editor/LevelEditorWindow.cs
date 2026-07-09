using System;
using System.Collections.Generic;
using System.IO;
using BadMovieClues.Data;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

namespace BadMovieClues.EditorTools
{
    public class LevelEditorWindow : EditorWindow
    {
        private const string CatalogPath = "Assets/_Project/Content/Resources/StarterCatalog.json";
        
        private List<LevelData> _levels = new List<LevelData>();
        private LevelData _selectedLevel;
        private Vector2 _scrollPosList;
        private Vector2 _scrollPosDetails;

        [MenuItem("Bad Movie Clues/Level Editor")]
        public static void ShowWindow()
        {
            var window = GetWindow<LevelEditorWindow>("Level Editor");
            window.minSize = new Vector2(800, 600);
            window.LoadCatalog();
        }

        private void OnEnable()
        {
            LoadCatalog();
        }

        private void LoadCatalog()
        {
            if (File.Exists(CatalogPath))
            {
                string json = File.ReadAllText(CatalogPath);
                _levels = JsonConvert.DeserializeObject<List<LevelData>>(json) ?? new List<LevelData>();
            }
            else
            {
                _levels = new List<LevelData>();
            }
            if (_levels.Count > 0 && _selectedLevel == null)
                _selectedLevel = _levels[0];
        }

        private void SaveCatalog()
        {
            string dir = Path.GetDirectoryName(CatalogPath);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

            string json = JsonConvert.SerializeObject(_levels, Formatting.Indented);
            File.WriteAllText(CatalogPath, json);
            AssetDatabase.Refresh();
            Debug.Log($"[LevelEditor] Saved {_levels.Count} levels to {CatalogPath}");
        }

        private void OnGUI()
        {
            EditorGUILayout.BeginHorizontal();

            // LEFT PANEL: List of levels
            DrawListPanel();

            // RIGHT PANEL: Details
            DrawDetailsPanel();

            EditorGUILayout.EndHorizontal();
        }

        private void DrawListPanel()
        {
            EditorGUILayout.BeginVertical("box", GUILayout.Width(300));
            
            GUILayout.Label("Levels", EditorStyles.boldLabel);
            
            _scrollPosList = EditorGUILayout.BeginScrollView(_scrollPosList);
            for (int i = 0; i < _levels.Count; i++)
            {
                var level = _levels[i];
                string displayName = string.IsNullOrEmpty(level.MovieTitle) ? "New Level" : level.MovieTitle;
                
                GUI.backgroundColor = _selectedLevel == level ? new Color(0.2f, 0.6f, 1f) : Color.white;
                
                EditorGUILayout.BeginHorizontal("box");
                if (GUILayout.Button(displayName, EditorStyles.label, GUILayout.ExpandWidth(true)))
                {
                    _selectedLevel = level;
                    GUI.FocusControl(null); // Remove focus to prevent text field cross-talk
                }
                
                if (GUILayout.Button("X", GUILayout.Width(24)))
                {
                    if (EditorUtility.DisplayDialog("Delete Level", $"Are you sure you want to delete '{displayName}'?", "Yes", "Cancel"))
                    {
                        _levels.RemoveAt(i);
                        if (_selectedLevel == level) _selectedLevel = null;
                        i--;
                    }
                }
                EditorGUILayout.EndHorizontal();
                GUI.backgroundColor = Color.white;
            }
            EditorGUILayout.EndScrollView();

            GUILayout.Space(10);
            if (GUILayout.Button("Add New Level", GUILayout.Height(30)))
            {
                var newLevel = new LevelData
                {
                    Id = $"level-{Guid.NewGuid().ToString().Substring(0, 8)}",
                    MovieTitle = "New Movie",
                    Difficulty = 1
                };
                _levels.Add(newLevel);
                _selectedLevel = newLevel;
            }
            
            if (GUILayout.Button("Save Changes", GUILayout.Height(40)))
            {
                SaveCatalog();
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawDetailsPanel()
        {
            EditorGUILayout.BeginVertical("box", GUILayout.ExpandWidth(true));
            
            if (_selectedLevel == null)
            {
                GUILayout.Label("Select a level to edit.", EditorStyles.centeredGreyMiniLabel);
                EditorGUILayout.EndVertical();
                return;
            }

            _scrollPosDetails = EditorGUILayout.BeginScrollView(_scrollPosDetails);

            GUILayout.Label("Level Details", EditorStyles.largeLabel);
            GUILayout.Space(10);

            _selectedLevel.Id = EditorGUILayout.TextField("ID (Unique)", _selectedLevel.Id);
            _selectedLevel.MovieTitle = EditorGUILayout.TextField("Movie Title", _selectedLevel.MovieTitle);
            
            GUILayout.Space(10);
            GUILayout.Label("Bad Description", EditorStyles.boldLabel);
            
            var textAreaStyle = new GUIStyle(EditorStyles.textArea) { wordWrap = true };
            _selectedLevel.BadDescription = EditorGUILayout.TextArea(_selectedLevel.BadDescription, textAreaStyle, GUILayout.Height(80));

            GUILayout.Space(10);
            GUILayout.Label("Hints & Clues", EditorStyles.boldLabel);
            _selectedLevel.CharacterClue = EditorGUILayout.TextField("Character Hint", _selectedLevel.CharacterClue);
            _selectedLevel.PlotHint = EditorGUILayout.TextField("Plot Hint", _selectedLevel.PlotHint);
            _selectedLevel.SceneHint = EditorGUILayout.TextField("Scene Hint", _selectedLevel.SceneHint);
            _selectedLevel.Difficulty = EditorGUILayout.IntSlider("Difficulty", _selectedLevel.Difficulty, 1, 3);

            GUILayout.Space(10);
            GUILayout.Label("Image Clue", EditorStyles.boldLabel);

            // Image Addressable Drop Zone
            Texture2D currentTexture = null;
            if (!string.IsNullOrEmpty(_selectedLevel.ImageKey))
            {
                // Find texture in project
                string[] guids = AssetDatabase.FindAssets($"{_selectedLevel.ImageKey} t:Texture2D");
                if (guids.Length > 0)
                {
                    currentTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(AssetDatabase.GUIDToAssetPath(guids[0]));
                }
            }

            EditorGUI.BeginChangeCheck();
            Texture2D newTexture = (Texture2D)EditorGUILayout.ObjectField("Image Asset", currentTexture, typeof(Texture2D), false, GUILayout.Height(100));
            if (EditorGUI.EndChangeCheck())
            {
                if (newTexture == null)
                {
                    _selectedLevel.ImageKey = "";
                }
                else
                {
                    string path = AssetDatabase.GetAssetPath(newTexture);
                    if (!string.IsNullOrEmpty(path))
                    {
                        var settings = AddressableAssetSettingsDefaultObject.Settings;
                        if (settings != null)
                        {
                            var group = settings.DefaultGroup;
                            string guid = AssetDatabase.AssetPathToGUID(path);
                            var entry = settings.CreateOrMoveEntry(guid, group);
                            entry.address = newTexture.name;
                            settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, entry, true);
                            Debug.Log($"[LevelEditor] Added {newTexture.name} to Addressables Default Group.");
                        }
                        _selectedLevel.ImageKey = newTexture.name;
                    }
                }
            }
            if (currentTexture != null)
            {
                EditorGUILayout.HelpBox($"Bound to Addressable Key: {_selectedLevel.ImageKey}", MessageType.Info);
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }
    }
}
