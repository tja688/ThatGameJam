using System;
using System.Collections.Generic;
using ThatGameJam.Features.KeroseneLamp.Controllers;
using ThatGameJam.Features.PlayerCharacter2D.Controllers;
using ThatGameJam.SaveSystem;
using ThatGameJam.SaveSystem.Adapters;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class SaveAdapterAutoBinderWindow : EditorWindow
{
    private enum AdapterState
    {
        Installed,
        Ready,
        MissingTarget,
        AmbiguousTargets
    }

    private sealed class AdapterBinding
    {
        public AdapterBinding(Type adapterType, Type targetType, string targetLabel, Action<MonoBehaviour, Component> configure = null)
        {
            AdapterType = adapterType;
            TargetType = targetType;
            TargetLabel = targetLabel;
            Configure = configure;
        }

        public Type AdapterType { get; }
        public Type TargetType { get; }
        public string TargetLabel { get; }
        public Action<MonoBehaviour, Component> Configure { get; }
    }

    private sealed class AdapterStatus
    {
        public AdapterBinding Binding { get; set; }
        public AdapterState State { get; set; }
        public string Detail { get; set; }
        public Component Target { get; set; }
        public int ExistingCount { get; set; }
        public int TargetCount { get; set; }
    }

    private static readonly AdapterBinding[] Bindings =
    {
        new AdapterBinding(typeof(PlayerSaveAdapter), typeof(PlatformerCharacterController), "PlatformerCharacterController", ConfigurePlayerAdapter),
        new AdapterBinding(typeof(KeroseneLampSaveAdapter), typeof(KeroseneLampManager), "KeroseneLampManager", ConfigureLampAdapter),
        new AdapterBinding(typeof(AreaSystemSaveAdapter), typeof(SaveManager), "SaveManager"),
        new AdapterBinding(typeof(CheckpointSaveAdapter), typeof(SaveManager), "SaveManager"),
        new AdapterBinding(typeof(DarknessSaveAdapter), typeof(SaveManager), "SaveManager"),
        new AdapterBinding(typeof(DoorGateSaveAdapter), typeof(SaveManager), "SaveManager"),
        new AdapterBinding(typeof(LightVitalitySaveAdapter), typeof(SaveManager), "SaveManager"),
        new AdapterBinding(typeof(SafeZoneSaveAdapter), typeof(SaveManager), "SaveManager"),
        new AdapterBinding(typeof(StoryFlagsSaveAdapter), typeof(SaveManager), "SaveManager")
    };

    private readonly List<AdapterStatus> _status = new List<AdapterStatus>();
    private bool _includeInactive = true;
    private Vector2 _scroll;

    [MenuItem("Tools/Save System/Save Adapter Binder")]
    private static void Open()
    {
        var window = GetWindow<SaveAdapterAutoBinderWindow>("Save Adapter Binder");
        window.minSize = new Vector2(540, 320);
        window.Scan();
    }

    private void OnEnable()
    {
        Scan();
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Auto-attach save adapters to scene objects.", EditorStyles.boldLabel);

        EditorGUI.BeginChangeCheck();
        _includeInactive = EditorGUILayout.Toggle("Include Inactive Objects", _includeInactive);
        if (EditorGUI.EndChangeCheck())
        {
            Scan();
        }

        EditorGUILayout.Space(8);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Scan", GUILayout.Height(24)))
        {
            Scan();
        }
        if (GUILayout.Button("Auto Bind Missing Adapters", GUILayout.Height(24)))
        {
            AutoBind();
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(8);
        DrawSceneSummary();
        EditorGUILayout.Space(4);
        DrawStatusList();
    }

    private void DrawSceneSummary()
    {
        var sceneCount = SceneManager.sceneCount;
        EditorGUILayout.LabelField($"Loaded Scenes: {sceneCount}", EditorStyles.boldLabel);

        for (var i = 0; i < sceneCount; i++)
        {
            var scene = SceneManager.GetSceneAt(i);
            var label = scene.IsValid() ? scene.name : "(invalid)";
            if (scene == SceneManager.GetActiveScene())
            {
                label += " (Active)";
            }

            EditorGUILayout.LabelField(label);
        }
    }

    private void DrawStatusList()
    {
        if (_status.Count == 0)
        {
            EditorGUILayout.HelpBox("No scan results. Click Scan to refresh.", MessageType.Info);
            return;
        }

        var installed = 0;
        var ready = 0;
        var missing = 0;
        var ambiguous = 0;

        for (var i = 0; i < _status.Count; i++)
        {
            switch (_status[i].State)
            {
                case AdapterState.Installed:
                    installed++;
                    break;
                case AdapterState.Ready:
                    ready++;
                    break;
                case AdapterState.MissingTarget:
                    missing++;
                    break;
                case AdapterState.AmbiguousTargets:
                    ambiguous++;
                    break;
            }
        }

        EditorGUILayout.HelpBox(
            $"Installed: {installed} | Ready: {ready} | Missing target: {missing} | Ambiguous: {ambiguous}",
            MessageType.None);

        _scroll = EditorGUILayout.BeginScrollView(_scroll);
        for (var i = 0; i < _status.Count; i++)
        {
            DrawStatusEntry(_status[i]);
        }
        EditorGUILayout.EndScrollView();
    }

    private static void DrawStatusEntry(AdapterStatus entry)
    {
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField(entry.Binding.AdapterType.Name, EditorStyles.boldLabel);
        EditorGUILayout.LabelField(GetStateLabel(entry.State, entry));
        if (!string.IsNullOrEmpty(entry.Detail))
        {
            EditorGUILayout.LabelField(entry.Detail, EditorStyles.miniLabel);
        }
        EditorGUILayout.EndVertical();
    }

    private static string GetStateLabel(AdapterState state, AdapterStatus entry)
    {
        switch (state)
        {
            case AdapterState.Installed:
                return $"Installed ({entry.ExistingCount})";
            case AdapterState.Ready:
                return $"Ready -> {entry.Target?.name ?? entry.Binding.TargetLabel}";
            case AdapterState.MissingTarget:
                return $"Missing target: {entry.Binding.TargetLabel}";
            case AdapterState.AmbiguousTargets:
                return $"Ambiguous targets ({entry.TargetCount})";
            default:
                return "Unknown";
        }
    }

    private void Scan()
    {
        _status.Clear();

        for (var i = 0; i < Bindings.Length; i++)
        {
            var binding = Bindings[i];
            var existing = FindSceneComponents(binding.AdapterType, _includeInactive);

            if (existing.Count > 0)
            {
                _status.Add(new AdapterStatus
                {
                    Binding = binding,
                    State = AdapterState.Installed,
                    ExistingCount = existing.Count,
                    Detail = $"Found {existing.Count} instance(s) in loaded scenes."
                });
                continue;
            }

            var targets = FindSceneComponents(binding.TargetType, _includeInactive);
            if (targets.Count == 0)
            {
                _status.Add(new AdapterStatus
                {
                    Binding = binding,
                    State = AdapterState.MissingTarget,
                    TargetCount = 0,
                    Detail = $"No {binding.TargetLabel} found in loaded scenes."
                });
                continue;
            }

            if (targets.Count > 1)
            {
                _status.Add(new AdapterStatus
                {
                    Binding = binding,
                    State = AdapterState.AmbiguousTargets,
                    TargetCount = targets.Count,
                    Detail = $"Found {targets.Count} targets. Auto bind skipped to avoid duplicate save keys."
                });
                continue;
            }

            _status.Add(new AdapterStatus
            {
                Binding = binding,
                State = AdapterState.Ready,
                Target = targets[0],
                TargetCount = 1,
                Detail = $"Target: {targets[0].name}"
            });
        }
    }

    private void AutoBind()
    {
        Scan();

        var added = 0;
        var skipped = 0;

        for (var i = 0; i < _status.Count; i++)
        {
            var entry = _status[i];
            if (entry.State != AdapterState.Ready || entry.Target == null)
            {
                skipped++;
                continue;
            }

            var adapter = Undo.AddComponent(entry.Target.gameObject, entry.Binding.AdapterType) as MonoBehaviour;
            if (adapter == null)
            {
                skipped++;
                continue;
            }

            entry.Binding.Configure?.Invoke(adapter, entry.Target);
            EditorSceneManager.MarkSceneDirty(entry.Target.gameObject.scene);
            added++;
        }

        EditorUtility.DisplayDialog(
            "Save Adapter Binder",
            $"Auto bind finished.\nAdded: {added}\nSkipped: {skipped}",
            "OK");

        Scan();
    }

    private static List<Component> FindSceneComponents(Type type, bool includeInactive)
    {
        var results = new List<Component>();
        if (type == null || !typeof(Component).IsAssignableFrom(type))
        {
            return results;
        }

        var objects = Resources.FindObjectsOfTypeAll(type);
        for (var i = 0; i < objects.Length; i++)
        {
            var obj = objects[i];
            if (obj == null || EditorUtility.IsPersistent(obj))
            {
                continue;
            }

            var component = obj as Component;
            if (component == null)
            {
                continue;
            }

            var scene = component.gameObject.scene;
            if (!scene.IsValid() || !scene.isLoaded || EditorSceneManager.IsPreviewScene(scene))
            {
                continue;
            }

            if (!includeInactive && !component.gameObject.activeInHierarchy)
            {
                continue;
            }

            results.Add(component);
        }

        return results;
    }

    private static void ConfigurePlayerAdapter(MonoBehaviour adapter, Component target)
    {
        var playerAdapter = adapter as PlayerSaveAdapter;
        var controller = target as PlatformerCharacterController;
        if (playerAdapter == null || controller == null)
        {
            return;
        }

        var serialized = new SerializedObject(playerAdapter);
        var controllerProp = serialized.FindProperty("playerController");
        if (controllerProp != null)
        {
            controllerProp.objectReferenceValue = controller;
        }

        var rigidbodyProp = serialized.FindProperty("playerRigidbody");
        if (rigidbodyProp != null)
        {
            rigidbodyProp.objectReferenceValue = controller.GetComponent<Rigidbody2D>();
        }

        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void ConfigureLampAdapter(MonoBehaviour adapter, Component target)
    {
        var lampAdapter = adapter as KeroseneLampSaveAdapter;
        var manager = target as KeroseneLampManager;
        if (lampAdapter == null || manager == null)
        {
            return;
        }

        var serialized = new SerializedObject(lampAdapter);
        var managerProp = serialized.FindProperty("lampManager");
        if (managerProp != null)
        {
            managerProp.objectReferenceValue = manager;
        }

        serialized.ApplyModifiedPropertiesWithoutUndo();
    }
}
