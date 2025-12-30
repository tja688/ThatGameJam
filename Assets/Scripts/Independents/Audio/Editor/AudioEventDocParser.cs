using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;

namespace ThatGameJam.Independents.Audio.Editor
{
    public static class AudioEventDocParser
    {
        public class EventDocEntry
        {
            public string Id;
            public string DisplayName;
            public string Group;
        }

        private static readonly Regex HeadingRegex = new Regex(@"^###\s+(SFX-[A-Z]+-\d+)\s*(.*)$", RegexOptions.Compiled);
        private static readonly Regex IdRegex = new Regex(@"SFX-[A-Z]+-\d+", RegexOptions.Compiled);

        public static List<EventDocEntry> Load()
        {
            Dictionary<string, EventDocEntry> entries = new Dictionary<string, EventDocEntry>();

            string mapPath = Path.Combine(Application.dataPath, "Audio Doc", "SFX_Dotting_Map.md");
            if (File.Exists(mapPath))
            {
                ParseMap(mapPath, entries);
            }

            string indexPath = Path.Combine(Application.dataPath, "Audio Doc", "SFX_Dotting_Index.md");
            if (File.Exists(indexPath))
            {
                ParseIndex(indexPath, entries);
            }

            List<EventDocEntry> list = new List<EventDocEntry>(entries.Values);
            list.Sort((a, b) => string.CompareOrdinal(a.Id, b.Id));
            return list;
        }

        private static void ParseMap(string path, Dictionary<string, EventDocEntry> entries)
        {
            foreach (string line in File.ReadLines(path))
            {
                Match match = HeadingRegex.Match(line);
                if (!match.Success)
                {
                    continue;
                }

                string id = match.Groups[1].Value.Trim();
                string name = match.Groups[2].Value.Trim();
                AddOrUpdate(entries, id, name);
            }
        }

        private static void ParseIndex(string path, Dictionary<string, EventDocEntry> entries)
        {
            string text = File.ReadAllText(path);
            MatchCollection matches = IdRegex.Matches(text);
            foreach (Match match in matches)
            {
                string id = match.Value.Trim();
                AddOrUpdate(entries, id, string.Empty);
            }
        }

        private static void AddOrUpdate(Dictionary<string, EventDocEntry> entries, string id, string name)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return;
            }

            if (!entries.TryGetValue(id, out EventDocEntry entry))
            {
                entry = new EventDocEntry
                {
                    Id = id,
                    DisplayName = name,
                    Group = ToGroup(id)
                };
                entries.Add(id, entry);
            }
            else if (string.IsNullOrWhiteSpace(entry.DisplayName) && !string.IsNullOrWhiteSpace(name))
            {
                entry.DisplayName = name;
            }
        }

        public static string ToGroup(string eventId)
        {
            if (string.IsNullOrWhiteSpace(eventId))
            {
                return "Other";
            }

            string[] parts = eventId.Split('-');
            if (parts.Length < 2)
            {
                return "Other";
            }

            string prefix = parts[1];
            switch (prefix)
            {
                case "PLR":
                    return "Player";
                case "ENM":
                    return "Enemy";
                case "INT":
                    return "Interactable";
                case "ENV":
                    return "Environment";
                case "UI":
                    return "UI";
                case "META":
                    return "Meta";
                default:
                    return "Other";
            }
        }
    }
}
