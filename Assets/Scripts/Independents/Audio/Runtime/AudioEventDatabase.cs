using System.Collections.Generic;
using UnityEngine;

namespace ThatGameJam.Independents.Audio
{
    [CreateAssetMenu(menuName = "ThatGameJam/Audio/Audio Event Database", fileName = "AudioEventDatabase")]
    public class AudioEventDatabase : ScriptableObject
    {
        public List<AudioEventConfig> Events = new List<AudioEventConfig>();

        private Dictionary<string, AudioEventConfig> _lookup;

        private void OnEnable()
        {
            RebuildLookup();
        }

        private void OnValidate()
        {
            RebuildLookup();
        }

        public bool TryGetConfig(string eventId, out AudioEventConfig config)
        {
            if (string.IsNullOrWhiteSpace(eventId))
            {
                config = null;
                return false;
            }

            EnsureLookup();
            return _lookup.TryGetValue(eventId, out config);
        }

        public void EnsureLookup()
        {
            if (_lookup == null)
            {
                RebuildLookup();
                return;
            }

            if (_lookup.Count != Events.Count)
            {
                RebuildLookup();
            }
        }

        public void RebuildLookup()
        {
            _lookup = new Dictionary<string, AudioEventConfig>();
            for (int i = 0; i < Events.Count; i++)
            {
                AudioEventConfig config = Events[i];
                if (config == null || string.IsNullOrWhiteSpace(config.EventId))
                {
                    continue;
                }

                if (!_lookup.ContainsKey(config.EventId))
                {
                    _lookup.Add(config.EventId, config);
                }
            }
        }

#if UNITY_EDITOR
        public AudioEventConfig GetOrCreate(string eventId, string displayName)
        {
            if (string.IsNullOrWhiteSpace(eventId))
            {
                return null;
            }

            EnsureLookup();
            if (_lookup.TryGetValue(eventId, out AudioEventConfig existing))
            {
                if (!string.IsNullOrWhiteSpace(displayName) && string.IsNullOrWhiteSpace(existing.DisplayName))
                {
                    existing.DisplayName = displayName;
                }
                return existing;
            }

            AudioEventConfig created = new AudioEventConfig
            {
                EventId = eventId,
                DisplayName = displayName
            };
            Events.Add(created);
            _lookup[eventId] = created;
            return created;
        }
#endif
    }
}
