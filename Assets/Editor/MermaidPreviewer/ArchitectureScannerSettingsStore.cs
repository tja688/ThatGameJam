using System.IO;
using UnityEditor;
using UnityEngine;

namespace MermaidPreviewer
{
    internal static class ArchitectureScannerSettingsStore
    {
        private const string SettingsAssetPath = "Assets/Editor/MermaidPreviewer/ArchitectureScannerSettings.asset";
        private const string SettingsGuidKey = "MermaidPreviewer.ArchitectureScannerSettingsGuid";

        public static ArchitectureScannerSettings LoadOrCreate()
        {
            var settings = LoadFromGuid();
            if (settings != null)
            {
                settings.EnsureDefaults();
                Save(settings);
                return settings;
            }

            settings = LoadFromPath(SettingsAssetPath);
            if (settings != null)
            {
                StoreGuid(settings);
                settings.EnsureDefaults();
                Save(settings);
                return settings;
            }

            return CreateNew();
        }

        public static void Save(ArchitectureScannerSettings settings)
        {
            if (settings == null)
            {
                return;
            }

            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
            StoreGuid(settings);
        }

        private static ArchitectureScannerSettings LoadFromGuid()
        {
            var guid = EditorPrefs.GetString(SettingsGuidKey, string.Empty);
            if (string.IsNullOrEmpty(guid))
            {
                return null;
            }

            var path = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(path))
            {
                return null;
            }

            return LoadFromPath(path);
        }

        private static ArchitectureScannerSettings LoadFromPath(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return null;
            }

            return AssetDatabase.LoadAssetAtPath<ArchitectureScannerSettings>(path);
        }

        private static ArchitectureScannerSettings CreateNew()
        {
            var settings = ScriptableObject.CreateInstance<ArchitectureScannerSettings>();
            settings.EnsureDefaults();

            var directory = Path.GetDirectoryName(SettingsAssetPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            AssetDatabase.CreateAsset(settings, SettingsAssetPath);
            AssetDatabase.SaveAssets();
            StoreGuid(settings);
            return settings;
        }

        private static void StoreGuid(ArchitectureScannerSettings settings)
        {
            var path = AssetDatabase.GetAssetPath(settings);
            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            var guid = AssetDatabase.AssetPathToGUID(path);
            if (!string.IsNullOrEmpty(guid))
            {
                EditorPrefs.SetString(SettingsGuidKey, guid);
            }
        }
    }
}
