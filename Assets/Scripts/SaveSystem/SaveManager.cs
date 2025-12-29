using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using UnityEngine;

namespace ThatGameJam.SaveSystem
{
    public class SaveManager : MonoBehaviour
    {
        [SerializeField] private bool logEnabled = true;
        [SerializeField] private bool includeInactiveParticipants = true;

        public static SaveManager Instance { get; private set; }
        public bool IsRestoring { get; private set; }

        public event Action RestoreStarted;
        public event Action RestoreFinished;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            SaveLog.Enabled = logEnabled;
        }

        public void Save()
        {
            SaveLog.Enabled = logEnabled;
            var stopwatch = Stopwatch.StartNew();

            var participants = CollectParticipants();
            var snapshot = new SaveSnapshot
            {
                version = SaveKeys.Version,
                timestamp = DateTime.UtcNow.ToString("o")
            };
            var seenKeys = new HashSet<string>(StringComparer.Ordinal);

            for (var i = 0; i < participants.Count; i++)
            {
                var participant = participants[i];
                if (participant == null || string.IsNullOrEmpty(participant.SaveKey))
                {
                    continue;
                }

                if (!seenKeys.Add(participant.SaveKey))
                {
                    SaveLog.Warn($"Duplicate SaveKey skipped: {participant.SaveKey}");
                    continue;
                }

                var json = participant.CaptureToJson();
                snapshot.blocks.Add(new SaveBlock
                {
                    key = participant.SaveKey,
                    json = json
                });
            }

            ES3.Save(SaveKeys.SnapshotKey, snapshot, SaveKeys.Settings);
            stopwatch.Stop();

            SaveLog.Info($"Saved {snapshot.blocks.Count} blocks in {stopwatch.ElapsedMilliseconds} ms.");
            if (SaveLog.Enabled && snapshot.blocks.Count > 0)
            {
                var detail = new StringBuilder();
                for (var i = 0; i < snapshot.blocks.Count; i++)
                {
                    var block = snapshot.blocks[i];
                    if (block == null || string.IsNullOrEmpty(block.key))
                    {
                        continue;
                    }

                    if (detail.Length > 0)
                    {
                        detail.Append(", ");
                    }

                    var size = block.json != null ? block.json.Length : 0;
                    detail.Append($"{block.key}({size})");
                }

                if (detail.Length > 0)
                {
                    SaveLog.Info($"Blocks: {detail}");
                }
            }
        }

        public void Load()
        {
            SaveLog.Enabled = logEnabled;

            if (!HasSave())
            {
                SaveLog.Warn("Load requested but no save exists.");
                return;
            }

            var snapshot = ES3.Load<SaveSnapshot>(SaveKeys.SnapshotKey, SaveKeys.Settings);
            if (snapshot == null)
            {
                SaveLog.Warn("Loaded snapshot is null.");
                return;
            }

            var blocks = new Dictionary<string, string>(StringComparer.Ordinal);
            if (snapshot.blocks != null)
            {
                for (var i = 0; i < snapshot.blocks.Count; i++)
                {
                    var block = snapshot.blocks[i];
                    if (block == null || string.IsNullOrEmpty(block.key))
                    {
                        continue;
                    }

                    blocks[block.key] = block.json;
                }
            }
            SaveLog.Info($"Loaded snapshot with {blocks.Count} blocks.");

            var participants = CollectParticipants();
            participants.Sort(CompareParticipants);

            IsRestoring = true;
            RestoreStarted?.Invoke();

            try
            {
                for (var i = 0; i < participants.Count; i++)
                {
                    var participant = participants[i];
                    if (participant == null || string.IsNullOrEmpty(participant.SaveKey))
                    {
                        continue;
                    }

                    if (blocks.TryGetValue(participant.SaveKey, out var json))
                    {
                        participant.RestoreFromJson(json);
                    }
                    else
                    {
                        SaveLog.Warn($"Missing save block: {participant.SaveKey}");
                    }
                }
            }
            finally
            {
                IsRestoring = false;
                RestoreFinished?.Invoke();
            }
        }

        public bool HasSave()
        {
            return ES3.KeyExists(SaveKeys.SnapshotKey, SaveKeys.Settings);
        }

        public void Delete()
        {
            SaveLog.Enabled = logEnabled;
            if (!HasSave())
            {
                SaveLog.Warn("Delete requested but no save exists.");
                return;
            }

            ES3.DeleteKey(SaveKeys.SnapshotKey, SaveKeys.Settings);
            SaveLog.Info("Deleted save snapshot.");
        }

        private List<ISaveParticipant> CollectParticipants()
        {
            var results = new List<ISaveParticipant>();
            var seen = new HashSet<ISaveParticipant>();

            var registry = SaveRegistry.GetParticipants();
            for (var i = 0; i < registry.Count; i++)
            {
                var participant = registry[i];
                if (participant != null && seen.Add(participant))
                {
                    results.Add(participant);
                }
            }

            var all = includeInactiveParticipants
                ? Resources.FindObjectsOfTypeAll<MonoBehaviour>()
                : FindObjectsOfType<MonoBehaviour>();

            for (var i = 0; i < all.Length; i++)
            {
                var behaviour = all[i];
                if (behaviour == null)
                {
                    continue;
                }

                if (includeInactiveParticipants && !behaviour.gameObject.scene.IsValid())
                {
                    continue;
                }

                if (behaviour is ISaveParticipant participant && seen.Add(participant))
                {
                    results.Add(participant);
                }
            }

            return results;
        }

        private static int CompareParticipants(ISaveParticipant a, ISaveParticipant b)
        {
            if (ReferenceEquals(a, b)) return 0;
            if (a == null) return 1;
            if (b == null) return -1;

            var orderCompare = a.LoadOrder.CompareTo(b.LoadOrder);
            if (orderCompare != 0) return orderCompare;

            return string.CompareOrdinal(a.SaveKey, b.SaveKey);
        }
    }
}
