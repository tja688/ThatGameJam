using System.Collections;
using System.Collections.Generic;
using ThatGameJam.UI.Services;
using UnityEngine;

namespace ThatGameJam.Independents.Audio
{
    public class AudioService : MonoBehaviour
    {
        public static AudioService Instance { get; private set; }

        [SerializeField] private AudioEventDatabase database;
        [SerializeField] private AudioDebugSettings debugSettings;
        [SerializeField] private AudioManagerProBackend backend;
        [SerializeField] private bool dontDestroyOnLoad = true;
        [SerializeField] private bool registerUIAudioSettings = true;

        private readonly Dictionary<string, AudioEventState> _eventStates = new Dictionary<string, AudioEventState>();
        private readonly Dictionary<LoopKey, ActiveLoop> _activeLoops = new Dictionary<LoopKey, ActiveLoop>();
        private readonly Dictionary<AudioBus, float> _busVolumes = new Dictionary<AudioBus, float>
        {
            { AudioBus.SFX, 1f },
            { AudioBus.Music, 1f },
            { AudioBus.UI, 1f },
            { AudioBus.Ambient, 1f }
        };

        private AudioSettingsService _settingsService;

        public static void Play(string eventId, AudioContext ctx = default)
        {
            if (Instance == null)
            {
                return;
            }

            Instance.PlayInternal(eventId, ctx);
        }

        public static void Stop(string eventId, AudioContext ctx = default)
        {
            if (Instance == null)
            {
                return;
            }

            Instance.StopInternal(eventId, ctx);
        }

        public static void Stop(AudioHandle handle, float fadeOut = 0f)
        {
            if (Instance == null || handle == null)
            {
                return;
            }

            Instance.StopHandle(handle, fadeOut);
        }

        public float SetBusVolume(AudioBus bus, float volume)
        {
            float clamped = Mathf.Clamp01(volume);
            _busVolumes[bus] = clamped;

            if (bus == AudioBus.Music && backend != null)
            {
                backend.SetBusVolume(bus, clamped);
            }

            UpdateActiveLoopVolumes(bus);
            return clamped;
        }

        public float GetBusVolume(AudioBus bus)
        {
            return _busVolumes.TryGetValue(bus, out float volume) ? volume : 1f;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            if (dontDestroyOnLoad)
            {
                DontDestroyOnLoad(gameObject);
            }

            if (backend == null)
            {
                backend = GetComponent<AudioManagerProBackend>();
            }

            if (registerUIAudioSettings)
            {
                _settingsService = new AudioSettingsService(this);
                UIServiceRegistry.SetAudioSettings(_settingsService);
            }
        }

        private void PlayInternal(string eventId, AudioContext ctx)
        {
            if (!TryGetConfig(eventId, out AudioEventConfig config))
            {
                LogMissingEvent(eventId);
                return;
            }

            if (config.Clips == null || config.Clips.Count == 0)
            {
                LogMissingClips(eventId);
                return;
            }

            AudioContext normalized = NormalizeContext(ctx);
            AudioBus bus = ResolveBus(config, normalized);

            if (config.Loop)
            {
                if (config.RandomIntervalMax > 0f && config.RandomIntervalMax >= config.RandomIntervalMin)
                {
                    StartRandomIntervalLoop(eventId, config, normalized, bus);
                }
                else
                {
                    StartLoop(eventId, config, normalized, bus);
                }
                return;
            }

            PlayOneShot(eventId, config, normalized, bus);
        }

        private void StopInternal(string eventId, AudioContext ctx)
        {
            if (!TryGetConfig(eventId, out AudioEventConfig config))
            {
                return;
            }

            AudioContext normalized = NormalizeContext(ctx);
            AudioBus bus = ResolveBus(config, normalized);
            bool stopByOwner = config.StopPolicy == AudioStopPolicy.StopByOwner && normalized.Owner != null;

            List<LoopKey> toStop = new List<LoopKey>();
            foreach (KeyValuePair<LoopKey, ActiveLoop> loop in _activeLoops)
            {
                if (loop.Key.EventId != eventId)
                {
                    continue;
                }

                if (stopByOwner && loop.Key.OwnerId != LoopKey.GetOwnerId(normalized.Owner))
                {
                    continue;
                }

                toStop.Add(loop.Key);
            }

            for (int i = 0; i < toStop.Count; i++)
            {
                StopLoop(toStop[i], bus);
            }
        }

        private void StopHandle(AudioHandle handle, float fadeOut)
        {
            if (backend == null || handle == null)
            {
                return;
            }

            backend.Stop(handle, fadeOut);
        }

        private void StartLoop(string eventId, AudioEventConfig config, AudioContext ctx, AudioBus bus)
        {
            LoopKey key = new LoopKey(eventId, ctx.Owner);
            if (_activeLoops.ContainsKey(key))
            {
                return;
            }

            if (IsOnCooldown(eventId, config))
            {
                return;
            }

            AudioClip clip = SelectClip(eventId, config);
            if (clip == null)
            {
                LogMissingClips(eventId);
                return;
            }

            float baseVolume = GetBaseVolume(config, ctx);
            float pitch = GetPitch(config, ctx);
            AudioPlaybackParams parameters = BuildParams(config, ctx, bus, baseVolume, pitch);
            AudioHandle handle = backend != null ? backend.PlayLoop(clip, parameters) : null;

            ActiveLoop loop = new ActiveLoop
            {
                EventId = eventId,
                Bus = bus,
                Handle = handle,
                BaseVolume = baseVolume,
                IsRandomInterval = false
            };

            _activeLoops[key] = loop;
            LogPlay(eventId, ctx, clip);
        }

        private void StartRandomIntervalLoop(string eventId, AudioEventConfig config, AudioContext ctx, AudioBus bus)
        {
            LoopKey key = new LoopKey(eventId, ctx.Owner);
            if (_activeLoops.ContainsKey(key))
            {
                return;
            }

            ActiveLoop loop = new ActiveLoop
            {
                EventId = eventId,
                Bus = bus,
                IsRandomInterval = true
            };

            loop.RandomIntervalRoutine = StartCoroutine(RandomIntervalRoutine(eventId, config, ctx, bus));
            _activeLoops[key] = loop;
        }

        private IEnumerator RandomIntervalRoutine(string eventId, AudioEventConfig config, AudioContext ctx, AudioBus bus)
        {
            while (true)
            {
                PlayOneShot(eventId, config, ctx, bus);
                float delay = Random.Range(config.RandomIntervalMin, config.RandomIntervalMax);
                if (delay > 0f)
                {
                    yield return new WaitForSeconds(delay);
                }
                else
                {
                    yield return null;
                }
            }
        }

        private void PlayOneShot(string eventId, AudioEventConfig config, AudioContext ctx, AudioBus bus)
        {
            if (IsOnCooldown(eventId, config))
            {
                return;
            }

            AudioClip clip = SelectClip(eventId, config);
            if (clip == null)
            {
                LogMissingClips(eventId);
                return;
            }

            float baseVolume = GetBaseVolume(config, ctx);
            float pitch = GetPitch(config, ctx);
            AudioPlaybackParams parameters = BuildParams(config, ctx, bus, baseVolume, pitch);

            if (backend != null)
            {
                backend.PlayOneShot(clip, parameters);
            }

            LogPlay(eventId, ctx, clip);
        }

        private bool IsOnCooldown(string eventId, AudioEventConfig config)
        {
            AudioEventState state = GetState(eventId);
            float now = Time.unscaledTime;
            if (config.Cooldown > 0f && now < state.LastPlayTime + config.Cooldown)
            {
                LogCooldown(eventId);
                return true;
            }

            state.LastPlayTime = now;
            return false;
        }

        private AudioClip SelectClip(string eventId, AudioEventConfig config)
        {
            if (config.Clips == null || config.Clips.Count == 0)
            {
                return null;
            }

            AudioEventState state = GetState(eventId);
            int count = config.Clips.Count;
            int index = 0;

            switch (config.PlayMode)
            {
                case AudioPlayMode.Single:
                    index = 0;
                    break;
                case AudioPlayMode.RandomOne:
                    index = Random.Range(0, count);
                    break;
                case AudioPlayMode.RandomNoRepeat:
                    if (count == 1)
                    {
                        index = 0;
                    }
                    else
                    {
                        int next;
                        do { next = Random.Range(0, count); } while (next == state.LastClipIndex);
                        index = next;
                    }
                    break;
                case AudioPlayMode.Sequence:
                    index = state.SequenceIndex % count;
                    state.SequenceIndex = (state.SequenceIndex + 1) % count;
                    break;
            }

            state.LastClipIndex = index;
            return config.Clips[index];
        }

        private AudioPlaybackParams BuildParams(AudioEventConfig config, AudioContext ctx, AudioBus bus, float baseVolume, float pitch)
        {
            float busVolume = GetBusVolume(bus);
            Vector3 position = ctx.HasPosition ? ctx.Position : (ctx.Owner != null ? ctx.Owner.position : Vector3.zero);
            bool hasPosition = ctx.HasPosition || ctx.Owner != null;

            return new AudioPlaybackParams
            {
                Bus = bus,
                Volume = baseVolume * busVolume,
                Pitch = pitch,
                SpatialMode = config.SpatialMode,
                SpatialBlend = config.SpatialBlend,
                MinDistance = config.MinDistance,
                MaxDistance = config.MaxDistance,
                Parent = ctx.Owner,
                Position = position,
                HasPosition = hasPosition
            };
        }

        private void StopLoop(LoopKey key, AudioBus bus)
        {
            if (!_activeLoops.TryGetValue(key, out ActiveLoop loop))
            {
                return;
            }

            _activeLoops.Remove(key);
            if (loop.IsRandomInterval)
            {
                if (loop.RandomIntervalRoutine != null)
                {
                    StopCoroutine(loop.RandomIntervalRoutine);
                }
            }
            else if (loop.Handle != null && backend != null)
            {
                float fadeOut = 0f;
                if (TryGetConfig(loop.EventId, out AudioEventConfig config))
                {
                    fadeOut = config.FadeOutDuration;
                }
                backend.Stop(loop.Handle, fadeOut);
            }

            LogStop(loop.EventId, bus);
        }

        private void UpdateActiveLoopVolumes(AudioBus bus)
        {
            foreach (KeyValuePair<LoopKey, ActiveLoop> loop in _activeLoops)
            {
                if (loop.Value.IsRandomInterval || loop.Value.Handle == null)
                {
                    continue;
                }

                if (loop.Value.Bus != bus)
                {
                    continue;
                }

                float volume = loop.Value.BaseVolume * GetBusVolume(bus);
                if (backend != null)
                {
                    backend.UpdateHandleVolume(loop.Value.Handle, volume);
                }
            }
        }

        private AudioEventState GetState(string eventId)
        {
            if (!_eventStates.TryGetValue(eventId, out AudioEventState state))
            {
                state = new AudioEventState();
                _eventStates[eventId] = state;
            }

            return state;
        }

        private bool TryGetConfig(string eventId, out AudioEventConfig config)
        {
            if (database == null)
            {
                config = null;
                return false;
            }

            return database.TryGetConfig(eventId, out config);
        }

        private AudioBus ResolveBus(AudioEventConfig config, AudioContext ctx)
        {
            if (ctx.BusOverride.HasValue)
            {
                return ctx.BusOverride.Value;
            }

            if (ctx.IsUI)
            {
                return AudioBus.UI;
            }

            return config.Bus;
        }

        private AudioContext NormalizeContext(AudioContext ctx)
        {
            if (ctx.VolumeScale <= 0f) { ctx.VolumeScale = 1f; }
            if (ctx.PitchScale <= 0f) { ctx.PitchScale = 1f; }
            return ctx;
        }

        private float GetBaseVolume(AudioEventConfig config, AudioContext ctx)
        {
            float volume = config.UseVolumeRange
                ? Random.Range(config.VolumeRange.x, config.VolumeRange.y)
                : config.Volume;

            if (ctx.HasVolumeOverride)
            {
                volume = ctx.VolumeOverride;
            }
            else
            {
                volume *= ctx.VolumeScale;
            }

            return Mathf.Max(0f, volume);
        }

        private float GetPitch(AudioEventConfig config, AudioContext ctx)
        {
            float pitch = config.UsePitchRange
                ? Random.Range(config.PitchRange.x, config.PitchRange.y)
                : config.Pitch;

            if (ctx.HasPitchOverride)
            {
                pitch = ctx.PitchOverride;
            }
            else
            {
                pitch *= ctx.PitchScale;
            }

            return Mathf.Clamp(pitch, 0.1f, 3f);
        }

        private void LogMissingEvent(string eventId)
        {
            if (debugSettings != null && debugSettings.LogMissingEvents)
            {
                Debug.LogWarning($"AudioService: Missing event id {eventId}");
            }
        }

        private void LogMissingClips(string eventId)
        {
            if (debugSettings != null && debugSettings.LogMissingClips)
            {
                Debug.LogWarning($"AudioService: No clips bound for {eventId}");
            }
        }

        private void LogPlay(string eventId, AudioContext ctx, AudioClip clip)
        {
            if (debugSettings != null && debugSettings.LogPlay)
            {
                string ownerName = ctx.Owner != null ? ctx.Owner.name : "none";
                Debug.Log($"AudioService: Play {eventId} clip={clip.name} owner={ownerName}");
            }
        }

        private void LogStop(string eventId, AudioBus bus)
        {
            if (debugSettings != null && debugSettings.LogStop)
            {
                Debug.Log($"AudioService: Stop {eventId} bus={bus}");
            }
        }

        private void LogCooldown(string eventId)
        {
            if (debugSettings != null && debugSettings.LogCooldown)
            {
                Debug.Log($"AudioService: Cooldown hit for {eventId}");
            }
        }

        private class AudioEventState
        {
            public float LastPlayTime;
            public int LastClipIndex = -1;
            public int SequenceIndex;
        }

        private struct LoopKey
        {
            public readonly string EventId;
            public readonly int OwnerId;

            public LoopKey(string eventId, Transform owner)
            {
                EventId = eventId;
                OwnerId = GetOwnerId(owner);
            }

            public static int GetOwnerId(Transform owner)
            {
                return owner == null ? 0 : owner.GetInstanceID();
            }
        }

        private class ActiveLoop
        {
            public string EventId;
            public AudioBus Bus;
            public AudioHandle Handle;
            public float BaseVolume;
            public bool IsRandomInterval;
            public Coroutine RandomIntervalRoutine;
        }
    }
}
