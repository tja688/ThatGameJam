using System;
using System.Collections.Generic;
using ThatGameJam.Independents;
using UnityEngine;

namespace ThatGameJam.SaveSystem.Adapters
{
    [Serializable]
    public class IndependentsSaveData
    {
        public List<TaskMonitorState> taskMonitors = new List<TaskMonitorState>();
        public List<IndependentTriggerState> itemDeletionTriggers = new List<IndependentTriggerState>();
        public List<IndependentTriggerState> soilCollisionListeners = new List<IndependentTriggerState>();
        public List<IndependentTriggerState> itemCollisionTriggers = new List<IndependentTriggerState>();
        public List<AutoGrowthState> autoGrowthControllers = new List<AutoGrowthState>();
    }

    [Serializable]
    public class TaskMonitorState
    {
        public string id;
        public bool interactedElla;
        public bool interactedBenjamin;
    }

    [Serializable]
    public class IndependentTriggerState
    {
        public string id;
        public bool triggered;
    }

    [Serializable]
    public class AutoGrowthState
    {
        public string id;
        public float normalized;
        public bool resumeGrowth;
    }

    public class IndependentsSaveAdapter : SaveParticipant<IndependentsSaveData>
    {
        [SerializeField] private string saveKey = "feature.independents";
        [SerializeField] private bool includeInactive = true;
        [SerializeField] private bool logMissing = false;

        public override string SaveKey => saveKey;

        private void Reset()
        {
            saveKey = "feature.independents";
            includeInactive = true;
        }

        protected override IndependentsSaveData Capture()
        {
            var data = new IndependentsSaveData();

            CaptureTaskMonitors(data);
            CaptureItemDeletionTriggers(data);
            CaptureSoilCollisionListeners(data);
            CaptureItemCollisionTriggers(data);
            CaptureAutoGrowthControllers(data);

            return data;
        }

        protected override void Restore(IndependentsSaveData data)
        {
            if (data == null)
            {
                return;
            }

            RestoreTaskMonitors(data);
            RestoreItemDeletionTriggers(data);
            RestoreSoilCollisionListeners(data);
            RestoreItemCollisionTriggers(data);
            RestoreAutoGrowthControllers(data);
        }

        private void CaptureTaskMonitors(IndependentsSaveData data)
        {
            var monitors = FindSceneObjects<TaskMonitor>();
            var seen = new HashSet<string>(StringComparer.Ordinal);

            for (int i = 0; i < monitors.Count; i++)
            {
                var monitor = monitors[i];
                var id = GetStableId(monitor);
                if (string.IsNullOrEmpty(id) || !seen.Add(id))
                {
                    continue;
                }

                data.taskMonitors.Add(new TaskMonitorState
                {
                    id = id,
                    interactedElla = monitor.HasInteractedWithElla,
                    interactedBenjamin = monitor.HasInteractedWithBenjamin
                });
            }
        }

        private void RestoreTaskMonitors(IndependentsSaveData data)
        {
            if (data.taskMonitors == null)
            {
                return;
            }

            var monitors = FindSceneObjects<TaskMonitor>();
            var map = BuildMap(monitors);

            for (int i = 0; i < data.taskMonitors.Count; i++)
            {
                var state = data.taskMonitors[i];
                if (state == null || string.IsNullOrEmpty(state.id))
                {
                    continue;
                }

                if (map.TryGetValue(state.id, out var monitor))
                {
                    monitor.ApplyInteractionState(state.interactedElla, state.interactedBenjamin);
                }
                else if (logMissing)
                {
                    SaveLog.Warn($"Missing TaskMonitor for id: {state.id}");
                }
            }
        }

        private void CaptureItemDeletionTriggers(IndependentsSaveData data)
        {
            var triggers = FindSceneObjects<ItemDeletionTrigger>();
            var seen = new HashSet<string>(StringComparer.Ordinal);

            for (int i = 0; i < triggers.Count; i++)
            {
                var trigger = triggers[i];
                var id = GetStableId(trigger);
                if (string.IsNullOrEmpty(id) || !seen.Add(id))
                {
                    continue;
                }

                data.itemDeletionTriggers.Add(new IndependentTriggerState
                {
                    id = id,
                    triggered = trigger.IsTriggered
                });
            }
        }

        private void RestoreItemDeletionTriggers(IndependentsSaveData data)
        {
            if (data.itemDeletionTriggers == null)
            {
                return;
            }

            var triggers = FindSceneObjects<ItemDeletionTrigger>();
            var map = BuildMap(triggers);

            for (int i = 0; i < data.itemDeletionTriggers.Count; i++)
            {
                var state = data.itemDeletionTriggers[i];
                if (state == null || string.IsNullOrEmpty(state.id))
                {
                    continue;
                }

                if (map.TryGetValue(state.id, out var trigger))
                {
                    trigger.ApplyTriggeredState(state.triggered);
                }
                else if (logMissing)
                {
                    SaveLog.Warn($"Missing ItemDeletionTrigger for id: {state.id}");
                }
            }
        }

        private void CaptureSoilCollisionListeners(IndependentsSaveData data)
        {
            var listeners = FindSceneObjects<SoilCollisionListener>();
            var seen = new HashSet<string>(StringComparer.Ordinal);

            for (int i = 0; i < listeners.Count; i++)
            {
                var listener = listeners[i];
                var id = GetStableId(listener);
                if (string.IsNullOrEmpty(id) || !seen.Add(id))
                {
                    continue;
                }

                data.soilCollisionListeners.Add(new IndependentTriggerState
                {
                    id = id,
                    triggered = listener.IsTriggered
                });
            }
        }

        private void RestoreSoilCollisionListeners(IndependentsSaveData data)
        {
            if (data.soilCollisionListeners == null)
            {
                return;
            }

            var listeners = FindSceneObjects<SoilCollisionListener>();
            var map = BuildMap(listeners);

            for (int i = 0; i < data.soilCollisionListeners.Count; i++)
            {
                var state = data.soilCollisionListeners[i];
                if (state == null || string.IsNullOrEmpty(state.id))
                {
                    continue;
                }

                if (map.TryGetValue(state.id, out var listener))
                {
                    listener.ApplyTriggeredState(state.triggered);
                }
                else if (logMissing)
                {
                    SaveLog.Warn($"Missing SoilCollisionListener for id: {state.id}");
                }
            }
        }

        private void CaptureItemCollisionTriggers(IndependentsSaveData data)
        {
            var triggers = FindSceneObjects<ItemCollisionTrigger>();
            var seen = new HashSet<string>(StringComparer.Ordinal);

            for (int i = 0; i < triggers.Count; i++)
            {
                var trigger = triggers[i];
                var id = GetStableId(trigger);
                if (string.IsNullOrEmpty(id) || !seen.Add(id))
                {
                    continue;
                }

                data.itemCollisionTriggers.Add(new IndependentTriggerState
                {
                    id = id,
                    triggered = trigger.IsTriggered
                });
            }
        }

        private void RestoreItemCollisionTriggers(IndependentsSaveData data)
        {
            if (data.itemCollisionTriggers == null)
            {
                return;
            }

            var triggers = FindSceneObjects<ItemCollisionTrigger>();
            var map = BuildMap(triggers);

            for (int i = 0; i < data.itemCollisionTriggers.Count; i++)
            {
                var state = data.itemCollisionTriggers[i];
                if (state == null || string.IsNullOrEmpty(state.id))
                {
                    continue;
                }

                if (map.TryGetValue(state.id, out var trigger))
                {
                    trigger.ApplyTriggeredState(state.triggered);
                }
                else if (logMissing)
                {
                    SaveLog.Warn($"Missing ItemCollisionTrigger for id: {state.id}");
                }
            }
        }

        private void CaptureAutoGrowthControllers(IndependentsSaveData data)
        {
            var controllers = FindSceneObjects<AutoGrowthController>();
            var seen = new HashSet<string>(StringComparer.Ordinal);

            for (int i = 0; i < controllers.Count; i++)
            {
                var controller = controllers[i];
                var id = GetStableId(controller);
                if (string.IsNullOrEmpty(id) || !seen.Add(id))
                {
                    continue;
                }

                data.autoGrowthControllers.Add(new AutoGrowthState
                {
                    id = id,
                    normalized = controller.GrowthNormalized,
                    resumeGrowth = controller.IsGrowthActive
                });
            }
        }

        private void RestoreAutoGrowthControllers(IndependentsSaveData data)
        {
            if (data.autoGrowthControllers == null)
            {
                return;
            }

            var controllers = FindSceneObjects<AutoGrowthController>();
            var map = BuildMap(controllers);

            for (int i = 0; i < data.autoGrowthControllers.Count; i++)
            {
                var state = data.autoGrowthControllers[i];
                if (state == null || string.IsNullOrEmpty(state.id))
                {
                    continue;
                }

                if (map.TryGetValue(state.id, out var controller))
                {
                    controller.ApplyGrowthState(state.normalized, state.resumeGrowth);
                }
                else if (logMissing)
                {
                    SaveLog.Warn($"Missing AutoGrowthController for id: {state.id}");
                }
            }
        }

        private List<T> FindSceneObjects<T>() where T : Component
        {
            var results = new List<T>();
            var objects = includeInactive
                ? Resources.FindObjectsOfTypeAll<T>()
                : FindObjectsOfType<T>();

            for (int i = 0; i < objects.Length; i++)
            {
                var obj = objects[i];
                if (obj == null)
                {
                    continue;
                }

                if (!obj.gameObject.scene.IsValid())
                {
                    continue;
                }

                results.Add(obj);
            }

            return results;
        }

        private Dictionary<string, T> BuildMap<T>(List<T> objects) where T : Component
        {
            var map = new Dictionary<string, T>(StringComparer.Ordinal);

            for (int i = 0; i < objects.Count; i++)
            {
                var obj = objects[i];
                var id = GetStableId(obj);
                if (string.IsNullOrEmpty(id) || map.ContainsKey(id))
                {
                    continue;
                }

                map.Add(id, obj);
            }

            return map;
        }

        private string GetStableId(Component component)
        {
            if (component == null)
            {
                return string.Empty;
            }

            var persistentId = component.GetComponent<PersistentId>();
            if (persistentId != null && !string.IsNullOrEmpty(persistentId.Id))
            {
                return $"{persistentId.Id}|{component.GetType().FullName}";
            }

            string path = BuildHierarchyPath(component.transform);
            if (string.IsNullOrEmpty(path))
            {
                return string.Empty;
            }

            return $"{path}|{component.GetType().FullName}";
        }

        private static string BuildHierarchyPath(Transform transform)
        {
            if (transform == null)
            {
                return string.Empty;
            }

            string path = transform.name;
            Transform current = transform.parent;
            while (current != null)
            {
                path = current.name + "/" + path;
                current = current.parent;
            }

            string sceneName = transform.gameObject.scene.IsValid()
                ? transform.gameObject.scene.name
                : "UnknownScene";

            return $"{sceneName}:{path}";
        }
    }
}
