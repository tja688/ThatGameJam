using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ThatGameJam.UI.Models;
using ThatGameJam.UI.Services.Interfaces;
using UnityEngine;
using DeviceType = ThatGameJam.UI.Models.DeviceType;

namespace ThatGameJam.UI.Mock
{
    public class MockInputRebindService : IInputRebindService
    {
        private const string PlayerPrefsKey = "UI_MOCK_REBIND_OVERRIDES";

        private readonly Dictionary<DeviceType, List<RebindEntry>> _entries;
        private bool _isRebinding;

        public MockInputRebindService()
        {
            _entries = new Dictionary<DeviceType, List<RebindEntry>>
            {
                {
                    DeviceType.Keyboard, new List<RebindEntry>
                    {
                        CreateEntry("move", "Move", "WASD"),
                        CreateEntry("jump", "Jump", "Space"),
                        CreateEntry("interact", "Interact", "E")
                    }
                },
                {
                    DeviceType.Gamepad, new List<RebindEntry>
                    {
                        CreateEntry("move", "Move", "Left Stick"),
                        CreateEntry("jump", "Jump", "South Button"),
                        CreateEntry("interact", "Interact", "West Button")
                    }
                },
                {
                    DeviceType.Mouse, new List<RebindEntry>
                    {
                        CreateEntry("look", "Look", "Mouse Delta"),
                        CreateEntry("fire", "Fire", "Left Button")
                    }
                }
            };
        }

        public IEnumerable<RebindEntry> GetEntries(DeviceType device)
        {
            return _entries.TryGetValue(device, out var list) ? list : Array.Empty<RebindEntry>();
        }

        public async Task StartRebind(string actionId, int bindingIndex)
        {
            if (_isRebinding)
            {
                return;
            }

            _isRebinding = true;

            await Task.Delay(350);

            if (TryGetEntry(actionId, bindingIndex, out var entry))
            {
                entry.BindingDisplay = PickRandomBinding(entry);
            }

            _isRebinding = false;
        }

        public void ResetBinding(string actionId, int bindingIndex)
        {
            if (TryGetEntry(actionId, bindingIndex, out var entry))
            {
                entry.BindingDisplay = entry.DefaultBinding;
            }
        }

        public void CancelRebind()
        {
            _isRebinding = false;
        }

        public void SaveOverrides()
        {
            var data = new RebindOverridesData();
            foreach (var list in _entries.Values)
            {
                foreach (var entry in list)
                {
                    data.Overrides.Add(new RebindOverride
                    {
                        ActionId = entry.ActionId,
                        BindingIndex = entry.BindingIndex,
                        BindingDisplay = entry.BindingDisplay
                    });
                }
            }

            var json = JsonUtility.ToJson(data);
            PlayerPrefs.SetString(PlayerPrefsKey, json);
            PlayerPrefs.Save();
        }

        public void LoadOverrides()
        {
            if (!PlayerPrefs.HasKey(PlayerPrefsKey))
            {
                return;
            }

            var json = PlayerPrefs.GetString(PlayerPrefsKey);
            var data = JsonUtility.FromJson<RebindOverridesData>(json);
            if (data == null || data.Overrides == null)
            {
                return;
            }

            foreach (var entry in data.Overrides)
            {
                if (TryGetEntry(entry.ActionId, entry.BindingIndex, out var target))
                {
                    target.BindingDisplay = entry.BindingDisplay;
                }
            }
        }

        private static RebindEntry CreateEntry(string actionId, string displayName, string binding)
        {
            return new RebindEntry
            {
                ActionId = actionId,
                DisplayName = displayName,
                BindingDisplay = binding,
                DefaultBinding = binding,
                BindingIndex = 0
            };
        }

        private bool TryGetEntry(string actionId, int bindingIndex, out RebindEntry entry)
        {
            foreach (var list in _entries.Values)
            {
                entry = list.FirstOrDefault(item => item.ActionId == actionId && item.BindingIndex == bindingIndex);
                if (entry != null)
                {
                    return true;
                }
            }

            entry = null;
            return false;
        }

        private static string PickRandomBinding(RebindEntry entry)
        {
            if (entry == null)
            {
                return "Unbound";
            }

            var options = new[] { "K", "L", "Mouse Button", "North Button", "Right Trigger" };
            return options[UnityEngine.Random.Range(0, options.Length)];
        }

        [Serializable]
        private class RebindOverridesData
        {
            public List<RebindOverride> Overrides = new List<RebindOverride>();
        }

        [Serializable]
        private class RebindOverride
        {
            public string ActionId;
            public int BindingIndex;
            public string BindingDisplay;
        }
    }
}
