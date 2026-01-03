using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Spine;
using Spine.Unity;
using UnityEditor;
using UnityEngine;

public static class SpineAnimationScanner
{
    private const string ReportPath = "Assets/Art/SpineAnimationReport.md";

    private static readonly string[] IdleKeywords = { "idle", "stand", "wait", "breathe", "standby", "static" };
    private static readonly string[] RunKeywords = { "run", "walk", "move", "locomotion" };
    private static readonly string[] JumpKeywords = { "jump", "takeoff", "up" };
    private static readonly string[] FallKeywords = { "fall", "drop", "down", "air" };
    private static readonly string[] LandKeywords = { "land", "landing" };
    private static readonly string[] ClimbKeywords = { "climb", "ladder", "rope", "hang" };
    private static readonly string[] CrouchKeywords = { "crouch", "squat" };
    private static readonly string[] HitKeywords = { "hit", "hurt", "damage", "death", "die", "knock" };
    private static readonly string[] AttackKeywords = { "attack", "atk", "slash", "shoot" };
    private static readonly string[] InteractKeywords = { "use", "interact", "pickup", "push", "pull" };
    private static readonly string[] OverlayKeywords = { "upper", "torso", "aim", "hold", "carry", "additive", "overlay", "layer", "hands", "blink", "eye", "face", "mouth", "expression" };

    [MenuItem("Tools/Spine/Scan Character Animations")]
    private static void Scan()
    {
        var asset = Selection.activeObject as SkeletonDataAsset;
        if (asset == null)
        {
            var guids = AssetDatabase.FindAssets("t:SkeletonDataAsset");
            if (guids.Length == 1)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[0]);
                asset = AssetDatabase.LoadAssetAtPath<SkeletonDataAsset>(path);
            }
        }

        if (asset == null)
        {
            EditorUtility.DisplayDialog("Spine Scan", "Select a SkeletonDataAsset in the Project window.", "OK");
            return;
        }

        var data = asset.GetSkeletonData(true);
        if (data == null)
        {
            EditorUtility.DisplayDialog("Spine Scan", "Failed to load SkeletonData from the selected asset.", "OK");
            return;
        }

        var rows = BuildRows(data);
        var report = BuildReport(data, rows);

        var directory = Path.GetDirectoryName(ReportPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(ReportPath, report, Encoding.UTF8);
        Debug.Log($"[Spine] Animation report written to {ReportPath}");
    }

    private static List<AnimationRow> BuildRows(SkeletonData data)
    {
        var rows = new List<AnimationRow>(data.Animations.Count);
        foreach (var anim in data.Animations)
        {
            var name = anim.Name;
            var (category, confidence) = Classify(name);
            var loop = GuessLoop(category, name);
            var track = category == "Overlay" || IsOverlayName(name) ? "1" : "0";
            rows.Add(new AnimationRow
            {
                Name = name,
                Duration = anim.Duration,
                Category = category,
                Confidence = confidence,
                Loop = loop,
                Track = track
            });
        }

        rows.Sort((a, b) => string.CompareOrdinal(a.Name, b.Name));
        return rows;
    }

    private static string BuildReport(SkeletonData data, List<AnimationRow> rows)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# Spine Animation Report");
        sb.AppendLine();
        sb.AppendLine($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine();
        sb.AppendLine("Source: Selected SkeletonDataAsset");
        sb.AppendLine();
        sb.AppendLine("## Summary");
        sb.AppendLine($"- Skins: {FormatList(data.Skins, s => s.Name)}");
        sb.AppendLine($"- Events: {FormatList(data.Events, e => e.Name)}");
        sb.AppendLine($"- Animations: {rows.Count} total");
        sb.AppendLine();
        sb.AppendLine("## Animation List");
        sb.AppendLine("| Name | Duration (s) | Loop | Category | Confidence | Track |");
        sb.AppendLine("| --- | ---: | :---: | :--- | :---: | :---: |");
        foreach (var row in rows)
        {
            sb.AppendLine($"| {row.Name} | {row.Duration:0.###} | {row.Loop} | {row.Category} | {row.Confidence} | {row.Track} |");
        }

        sb.AppendLine();
        sb.AppendLine("## Recommended Mapping (Draft)");
        sb.AppendLine("### Base (Track 0)");
        sb.AppendLine($"- Idle: {PickFirstContains(rows, "standby", "idle", "static_front")}");
        sb.AppendLine($"- Run/Walk: {PickFirstContains(rows, "/walk", "run")}");
        sb.AppendLine("- JumpUp: (missing)");
        sb.AppendLine("- Fall: (missing)");
        sb.AppendLine("- Land: (missing)");
        sb.AppendLine($"- ClimbMove (vertical): {PickFirstContains(rows, "climb_vert", "climb_vertical")}");
        sb.AppendLine($"- ClimbMove (horizontal): {PickFirstContains(rows, "climb_horiz", "climb_horizontal")}");
        sb.AppendLine($"- ClimbIdle: {PickFirstContains(rows, "static_back_hang", "static_back_stand")}");
        sb.AppendLine();
        sb.AppendLine("### Overlay (Track 1)");
        sb.AppendLine($"- Blink/Face: {PickFirstContains(rows, "blink")}");
        sb.AppendLine($"- UpperBody/Hands: {PickFirstContains(rows, "hand", "upper")}");
        sb.AppendLine("- Breath/IdleAdd: (missing)");
        sb.AppendLine("- HitReact: (missing)");

        var unknowns = rows.FindAll(r => r.Category == "Unknown");
        if (unknowns.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("## Unknown or Ambiguous");
            foreach (var row in unknowns)
            {
                sb.AppendLine($"- {row.Name}");
            }
        }

        sb.AppendLine();
        sb.AppendLine("## Notes");
        sb.AppendLine("- This report uses naming heuristics only (route A).");
        sb.AppendLine("- If animation names are unclear, use the runtime browser in SpineCharacterAnimDriver (enableDebugBrowser).");
        return sb.ToString();
    }

    private static (string category, string confidence) Classify(string name)
    {
        var lower = name.ToLowerInvariant();
        if (ContainsAny(lower, OverlayKeywords))
        {
            return ("Overlay", "medium");
        }
        if (ContainsAny(lower, IdleKeywords)) return ("Idle", "medium");
        if (ContainsAny(lower, RunKeywords)) return ("Run", "medium");
        if (ContainsAny(lower, JumpKeywords)) return ("JumpUp", "medium");
        if (ContainsAny(lower, FallKeywords)) return ("Fall", "medium");
        if (ContainsAny(lower, LandKeywords)) return ("Land", "medium");
        if (ContainsAny(lower, ClimbKeywords)) return ("Climb", "medium");
        if (ContainsAny(lower, CrouchKeywords)) return ("Crouch", "medium");
        if (ContainsAny(lower, HitKeywords)) return ("Hit", "medium");
        if (ContainsAny(lower, AttackKeywords)) return ("Attack", "medium");
        if (ContainsAny(lower, InteractKeywords)) return ("Interact", "medium");
        return ("Unknown", "low");
    }

    private static string GuessLoop(string category, string name)
    {
        if (category == "Idle" || category == "Run" || category == "Climb")
        {
            return "yes";
        }
        if (category == "JumpUp" || category == "Fall" || category == "Land" || category == "Hit" || category == "Attack")
        {
            return "no";
        }
        return name.ToLowerInvariant().Contains("blink") ? "yes" : "maybe";
    }

    private static bool IsOverlayName(string name)
    {
        return ContainsAny(name.ToLowerInvariant(), OverlayKeywords);
    }

    private static bool ContainsAny(string value, IEnumerable<string> candidates)
    {
        foreach (var candidate in candidates)
        {
            if (value.Contains(candidate))
            {
                return true;
            }
        }
        return false;
    }

    private static string PickFirstContains(List<AnimationRow> rows, params string[] tokens)
    {
        foreach (var token in tokens)
        {
            foreach (var row in rows)
            {
                if (row.Name.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return row.Name;
                }
            }
        }
        return "(missing)";
    }

    private static string FormatList<T>(IEnumerable<T> list, Func<T, string> getName)
    {
        if (list == null)
        {
            return "(none)";
        }

        var names = new List<string>();
        foreach (var item in list)
        {
            names.Add(getName(item));
        }

        return names.Count == 0 ? "(none)" : string.Join(", ", names);
    }

    private sealed class AnimationRow
    {
        public string Name;
        public float Duration;
        public string Loop;
        public string Category;
        public string Confidence;
        public string Track;
    }
}
