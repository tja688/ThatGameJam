using System.IO;
using ThatGameJam.Independents.Audio;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ThatGameJam.Independents.Audio.Editor
{
    public static class AudioSetupMenu
    {
        private const string RootName = "AudioSystem";
        private const string DatabasePath = "Assets/Audio/AudioEventDatabase.asset";
        private const string DebugSettingsPath = "Assets/Audio/AudioDebugSettings.asset";

        [MenuItem("Tools/Audio/Setup Audio (Audio Manager Pro)")]
        public static void SetupAudio()
        {
            AudioService service = Object.FindObjectOfType<AudioService>();
            GameObject root = service != null ? service.gameObject : GameObject.Find(RootName);

            if (root == null)
            {
                root = new GameObject(RootName);
                Undo.RegisterCreatedObjectUndo(root, "Create AudioSystem");
            }

            service = EnsureComponent<AudioService>(root);
            AudioManagerProBackend backend = EnsureComponent<AudioManagerProBackend>(root);

            SFXManager sfxManager = Object.FindObjectOfType<SFXManager>();
            MusicManager musicManager = Object.FindObjectOfType<MusicManager>();

            if (sfxManager == null)
            {
                sfxManager = EnsureComponent<SFXManager>(root);
            }

            if (musicManager == null)
            {
                musicManager = EnsureComponent<MusicManager>(root);
            }

            AudioEventDatabase database = FindOrCreateAsset<AudioEventDatabase>(DatabasePath);
            AudioDebugSettings debugSettings = FindOrCreateAsset<AudioDebugSettings>(DebugSettingsPath);

            AssignSerialized(service, "database", database);
            AssignSerialized(service, "backend", backend);
            AssignSerialized(service, "debugSettings", debugSettings);
            AssignSerialized(backend, "sfxManager", sfxManager);
            AssignSerialized(backend, "musicManager", musicManager);

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

            string report = $"Audio setup complete.\n" +
                            $"- Root: {root.name}\n" +
                            $"- AudioService: {(service != null ? "OK" : "Missing")}\n" +
                            $"- Backend: {(backend != null ? "OK" : "Missing")}\n" +
                            $"- SFXManager: {(sfxManager != null ? sfxManager.name : "Missing")}\n" +
                            $"- MusicManager: {(musicManager != null ? musicManager.name : "Missing")}\n" +
                            $"- Database: {(database != null ? AssetDatabase.GetAssetPath(database) : "Missing")}";

            Debug.Log(report);
            EditorUtility.DisplayDialog("Audio Setup", report, "OK");
        }

        private static T EnsureComponent<T>(GameObject root) where T : Component
        {
            T component = root.GetComponent<T>();
            if (component == null)
            {
                component = Undo.AddComponent<T>(root);
            }
            return component;
        }

        private static T FindOrCreateAsset<T>(string path) where T : ScriptableObject
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null)
            {
                return asset;
            }

            string directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            return asset;
        }

        private static void AssignSerialized(Object target, string propertyName, Object value)
        {
            if (target == null)
            {
                return;
            }

            SerializedObject serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property != null)
            {
                property.objectReferenceValue = value;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
        }
    }
}
