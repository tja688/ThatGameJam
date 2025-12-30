using System.Collections.Generic;
using System.IO;
using System.Linq;
using ThatGameJam.Independents.Audio;
using UnityEditor;
using UnityEngine;

namespace ThatGameJam.Independents.Audio.Editor
{
    public class AudioBindingPanel : EditorWindow
    {
        private AudioEventDatabase _database;
        private List<AudioEventDocParser.EventDocEntry> _docEntries = new List<AudioEventDocParser.EventDocEntry>();
        private readonly Dictionary<string, int> _dbIndex = new Dictionary<string, int>();
        private readonly List<string> _categories = new List<string>();

        private string _search = string.Empty;
        private int _selectedCategoryIndex;
        private string _selectedEventId;

        private Vector2 _categoryScroll;
        private Vector2 _eventScroll;
        private Vector2 _detailScroll;

        private AudioBus _batchBus = AudioBus.SFX;
        private float _batchCooldown = 0.2f;
        private AudioClip _previewClip;

        [MenuItem("Tools/Audio/Audio Binding Panel")]
        public static void Open()
        {
            GetWindow<AudioBindingPanel>("Audio Binding Panel");
        }

        private void OnEnable()
        {
            ReloadDocs();
            LoadDatabase();
        }

        private void OnGUI()
        {
            DrawDatabaseBar();

            EditorGUILayout.Space();
            DrawBatchTools();

            EditorGUILayout.Space();
            DrawMainPanel();
        }

        private void DrawDatabaseBar()
        {
            EditorGUILayout.BeginHorizontal();
            _database = (AudioEventDatabase)EditorGUILayout.ObjectField("Database", _database, typeof(AudioEventDatabase), false);
            if (GUILayout.Button("Find", GUILayout.Width(60)))
            {
                LoadDatabase();
            }
            if (GUILayout.Button("Create", GUILayout.Width(70)))
            {
                CreateDatabase();
            }
            if (GUILayout.Button("Sync Docs", GUILayout.Width(80)))
            {
                SyncFromDocs();
            }
            EditorGUILayout.EndHorizontal();

            if (_database == null)
            {
                EditorGUILayout.HelpBox("No AudioEventDatabase assigned. Use Create or Find to select one.", MessageType.Warning);
            }
        }

        private void DrawBatchTools()
        {
            EditorGUILayout.LabelField("Batch Tools", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            _batchBus = (AudioBus)EditorGUILayout.EnumPopup("Set Bus", _batchBus);
            if (GUILayout.Button("Apply To Category", GUILayout.Width(140)))
            {
                ApplyBusToCategory();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            _batchCooldown = EditorGUILayout.FloatField("Set Cooldown", _batchCooldown);
            if (GUILayout.Button("Apply To Category", GUILayout.Width(140)))
            {
                ApplyCooldownToCategory();
            }
            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button("Export Unbound Events"))
            {
                ExportUnboundEvents();
            }
        }

        private void DrawMainPanel()
        {
            if (_database == null)
            {
                return;
            }

            EditorGUILayout.BeginHorizontal();

            DrawCategoryList();
            DrawEventList();

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();
            DrawDetailsPanel();
        }

        private void DrawCategoryList()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(180));
            EditorGUILayout.LabelField("Categories", EditorStyles.boldLabel);
            _categoryScroll = EditorGUILayout.BeginScrollView(_categoryScroll, GUILayout.Height(300));
            for (int i = 0; i < _categories.Count; i++)
            {
                bool selected = i == _selectedCategoryIndex;
                if (GUILayout.Toggle(selected, _categories[i], "Button"))
                {
                    _selectedCategoryIndex = i;
                }
            }
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private void DrawEventList()
        {
            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField("Events", EditorStyles.boldLabel);
            _search = EditorGUILayout.TextField("Search", _search);

            List<AudioEventDocParser.EventDocEntry> entries = GetFilteredEntries();
            _eventScroll = EditorGUILayout.BeginScrollView(_eventScroll, GUILayout.Height(300));
            foreach (AudioEventDocParser.EventDocEntry entry in entries)
            {
                string label = string.IsNullOrWhiteSpace(entry.DisplayName)
                    ? entry.Id
                    : $"{entry.Id}  {entry.DisplayName}";

                bool selected = entry.Id == _selectedEventId;
                if (GUILayout.Toggle(selected, label, "Button"))
                {
                    _selectedEventId = entry.Id;
                }
            }
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private void DrawDetailsPanel()
        {
            if (string.IsNullOrWhiteSpace(_selectedEventId))
            {
                EditorGUILayout.HelpBox("Select an event to edit its settings.", MessageType.Info);
                return;
            }

            if (!_dbIndex.TryGetValue(_selectedEventId, out int index))
            {
                if (GUILayout.Button("Create Config For Selected Event"))
                {
                    CreateConfigForSelected();
                }
                return;
            }

            SerializedObject serialized = new SerializedObject(_database);
            SerializedProperty eventsProp = serialized.FindProperty("Events");
            if (eventsProp == null || index >= eventsProp.arraySize)
            {
                return;
            }

            SerializedProperty configProp = eventsProp.GetArrayElementAtIndex(index);
            SerializedProperty eventIdProp = configProp.FindPropertyRelative("EventId");
            SerializedProperty displayNameProp = configProp.FindPropertyRelative("DisplayName");
            SerializedProperty categoryProp = configProp.FindPropertyRelative("Category");
            SerializedProperty busProp = configProp.FindPropertyRelative("Bus");
            SerializedProperty playModeProp = configProp.FindPropertyRelative("PlayMode");
            SerializedProperty loopProp = configProp.FindPropertyRelative("Loop");
            SerializedProperty useVolumeRangeProp = configProp.FindPropertyRelative("UseVolumeRange");
            SerializedProperty volumeProp = configProp.FindPropertyRelative("Volume");
            SerializedProperty volumeRangeProp = configProp.FindPropertyRelative("VolumeRange");
            SerializedProperty usePitchRangeProp = configProp.FindPropertyRelative("UsePitchRange");
            SerializedProperty pitchProp = configProp.FindPropertyRelative("Pitch");
            SerializedProperty pitchRangeProp = configProp.FindPropertyRelative("PitchRange");
            SerializedProperty cooldownProp = configProp.FindPropertyRelative("Cooldown");
            SerializedProperty randomMinProp = configProp.FindPropertyRelative("RandomIntervalMin");
            SerializedProperty randomMaxProp = configProp.FindPropertyRelative("RandomIntervalMax");
            SerializedProperty spatialModeProp = configProp.FindPropertyRelative("SpatialMode");
            SerializedProperty spatialBlendProp = configProp.FindPropertyRelative("SpatialBlend");
            SerializedProperty minDistanceProp = configProp.FindPropertyRelative("MinDistance");
            SerializedProperty maxDistanceProp = configProp.FindPropertyRelative("MaxDistance");
            SerializedProperty stopPolicyProp = configProp.FindPropertyRelative("StopPolicy");
            SerializedProperty fadeOutProp = configProp.FindPropertyRelative("FadeOutDuration");
            SerializedProperty clipsProp = configProp.FindPropertyRelative("Clips");

            _detailScroll = EditorGUILayout.BeginScrollView(_detailScroll, GUILayout.Height(330));
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.PropertyField(eventIdProp);
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.PropertyField(displayNameProp);
            EditorGUILayout.PropertyField(categoryProp);
            EditorGUILayout.PropertyField(busProp);
            EditorGUILayout.PropertyField(playModeProp);
            EditorGUILayout.PropertyField(loopProp);
            EditorGUILayout.PropertyField(cooldownProp);
            EditorGUILayout.PropertyField(randomMinProp);
            EditorGUILayout.PropertyField(randomMaxProp);
            EditorGUILayout.PropertyField(stopPolicyProp);
            EditorGUILayout.PropertyField(fadeOutProp);
            EditorGUILayout.PropertyField(spatialModeProp);
            EditorGUILayout.PropertyField(spatialBlendProp);
            EditorGUILayout.PropertyField(minDistanceProp);
            EditorGUILayout.PropertyField(maxDistanceProp);

            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(useVolumeRangeProp);
            if (useVolumeRangeProp.boolValue)
            {
                EditorGUILayout.PropertyField(volumeRangeProp);
            }
            else
            {
                EditorGUILayout.PropertyField(volumeProp);
            }

            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(usePitchRangeProp);
            if (usePitchRangeProp.boolValue)
            {
                EditorGUILayout.PropertyField(pitchRangeProp);
            }
            else
            {
                EditorGUILayout.PropertyField(pitchProp);
            }

            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(clipsProp, true);

            EditorGUILayout.Space();
            _previewClip = (AudioClip)EditorGUILayout.ObjectField("Preview Clip", _previewClip, typeof(AudioClip), false);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Play"))
            {
                AudioPreviewUtility.PlayClip(_previewClip);
            }
            if (GUILayout.Button("Stop"))
            {
                AudioPreviewUtility.StopAllClips();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndScrollView();

            serialized.ApplyModifiedProperties();
            EditorUtility.SetDirty(_database);
        }

        private void LoadDatabase()
        {
            if (_database == null)
            {
                string[] guids = AssetDatabase.FindAssets("t:AudioEventDatabase");
                if (guids.Length > 0)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                    _database = AssetDatabase.LoadAssetAtPath<AudioEventDatabase>(path);
                }
            }

            RebuildIndex();
        }

        private void CreateDatabase()
        {
            string path = EditorUtility.SaveFilePanelInProject("Create AudioEventDatabase", "AudioEventDatabase", "asset", "Choose location for AudioEventDatabase");
            if (string.IsNullOrWhiteSpace(path))
            {
                return;
            }

            _database = ScriptableObject.CreateInstance<AudioEventDatabase>();
            AssetDatabase.CreateAsset(_database, path);
            AssetDatabase.SaveAssets();
            RebuildIndex();
        }

        private void ReloadDocs()
        {
            _docEntries = AudioEventDocParser.Load();
            BuildCategories();
        }

        private void BuildCategories()
        {
            _categories.Clear();
            _categories.Add("All");
            foreach (string group in _docEntries.Select(entry => entry.Group).Distinct())
            {
                if (!_categories.Contains(group))
                {
                    _categories.Add(group);
                }
            }
        }

        private void RebuildIndex()
        {
            _dbIndex.Clear();
            if (_database == null)
            {
                return;
            }

            for (int i = 0; i < _database.Events.Count; i++)
            {
                AudioEventConfig config = _database.Events[i];
                if (config == null || string.IsNullOrWhiteSpace(config.EventId))
                {
                    continue;
                }

                _dbIndex[config.EventId] = i;
            }
        }

        private List<AudioEventDocParser.EventDocEntry> GetFilteredEntries()
        {
            IEnumerable<AudioEventDocParser.EventDocEntry> entries = _docEntries;
            if (_docEntries.Count == 0 && _database != null)
            {
                entries = _database.Events.Select(config => new AudioEventDocParser.EventDocEntry
                {
                    Id = config.EventId,
                    DisplayName = config.DisplayName,
                    Group = AudioEventDocParser.ToGroup(config.EventId)
                });
            }

            string selectedGroup = _categories.Count > _selectedCategoryIndex ? _categories[_selectedCategoryIndex] : "All";
            if (selectedGroup != "All")
            {
                entries = entries.Where(entry => entry.Group == selectedGroup);
            }

            if (!string.IsNullOrWhiteSpace(_search))
            {
                string lower = _search.ToLowerInvariant();
                entries = entries.Where(entry =>
                    entry.Id.ToLowerInvariant().Contains(lower) ||
                    (!string.IsNullOrWhiteSpace(entry.DisplayName) && entry.DisplayName.ToLowerInvariant().Contains(lower)));
            }

            return entries.OrderBy(entry => entry.Id).ToList();
        }

        private void SyncFromDocs()
        {
            if (_database == null)
            {
                return;
            }

            Undo.RecordObject(_database, "Sync Audio Events");
            foreach (AudioEventDocParser.EventDocEntry entry in _docEntries)
            {
                AudioEventConfig config = _database.GetOrCreate(entry.Id, entry.DisplayName);
                ApplyDefaults(config, entry);
            }

            EditorUtility.SetDirty(_database);
            _database.RebuildLookup();
            AssetDatabase.SaveAssets();
            RebuildIndex();
        }

        private void CreateConfigForSelected()
        {
            if (_database == null || string.IsNullOrWhiteSpace(_selectedEventId))
            {
                return;
            }

            AudioEventDocParser.EventDocEntry entry = _docEntries.FirstOrDefault(item => item.Id == _selectedEventId);
            string displayName = entry != null ? entry.DisplayName : string.Empty;
            Undo.RecordObject(_database, "Create Audio Event");
            AudioEventConfig config = _database.GetOrCreate(_selectedEventId, displayName);
            if (entry != null)
            {
                ApplyDefaults(config, entry);
            }

            EditorUtility.SetDirty(_database);
            _database.RebuildLookup();
            RebuildIndex();
        }

        private void ApplyDefaults(AudioEventConfig config, AudioEventDocParser.EventDocEntry entry)
        {
            if (config == null || entry == null)
            {
                return;
            }

            switch (entry.Group)
            {
                case "UI":
                    config.Category = AudioCategory.UI;
                    config.Bus = AudioBus.UI;
                    break;
                case "Environment":
                    config.Category = AudioCategory.Ambient;
                    config.Bus = AudioBus.Ambient;
                    break;
                default:
                    config.Category = AudioCategory.SFX;
                    config.Bus = AudioBus.SFX;
                    break;
            }
        }

        private void ApplyBusToCategory()
        {
            if (_database == null)
            {
                return;
            }

            string selectedGroup = _categories.Count > _selectedCategoryIndex ? _categories[_selectedCategoryIndex] : "All";
            IEnumerable<AudioEventDocParser.EventDocEntry> entries = GetFilteredEntries();

            Undo.RecordObject(_database, "Batch Set Bus");
            foreach (AudioEventDocParser.EventDocEntry entry in entries)
            {
                if (selectedGroup != "All" && entry.Group != selectedGroup)
                {
                    continue;
                }

                AudioEventConfig config = _database.GetOrCreate(entry.Id, entry.DisplayName);
                config.Bus = _batchBus;
            }

            EditorUtility.SetDirty(_database);
            _database.RebuildLookup();
            RebuildIndex();
        }

        private void ApplyCooldownToCategory()
        {
            if (_database == null)
            {
                return;
            }

            string selectedGroup = _categories.Count > _selectedCategoryIndex ? _categories[_selectedCategoryIndex] : "All";
            IEnumerable<AudioEventDocParser.EventDocEntry> entries = GetFilteredEntries();

            Undo.RecordObject(_database, "Batch Set Cooldown");
            foreach (AudioEventDocParser.EventDocEntry entry in entries)
            {
                if (selectedGroup != "All" && entry.Group != selectedGroup)
                {
                    continue;
                }

                AudioEventConfig config = _database.GetOrCreate(entry.Id, entry.DisplayName);
                config.Cooldown = _batchCooldown;
            }

            EditorUtility.SetDirty(_database);
            _database.RebuildLookup();
            RebuildIndex();
        }

        private void ExportUnboundEvents()
        {
            if (_database == null)
            {
                return;
            }

            List<string> unbound = new List<string>();
            foreach (AudioEventDocParser.EventDocEntry entry in _docEntries)
            {
                if (!_dbIndex.TryGetValue(entry.Id, out int index))
                {
                    unbound.Add($"{entry.Id} (missing config)");
                    continue;
                }

                AudioEventConfig config = _database.Events[index];
                if (config == null || config.Clips == null || config.Clips.Count == 0)
                {
                    unbound.Add($"{entry.Id} {entry.DisplayName}".Trim());
                }
            }

            string exportPath = Path.Combine(Application.dataPath, "Audio Doc", "Unbound_Audio_List.md");
            List<string> lines = new List<string>
            {
                "# Unbound Audio Events",
                string.Empty,
                $"Total: {unbound.Count}",
                string.Empty
            };
            lines.AddRange(unbound.Select(item => $"- {item}"));

            File.WriteAllText(exportPath, string.Join("\n", lines));
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("Export Unbound Events", $"Exported {unbound.Count} entries to Assets/Audio Doc/Unbound_Audio_List.md", "OK");
        }
    }
}
