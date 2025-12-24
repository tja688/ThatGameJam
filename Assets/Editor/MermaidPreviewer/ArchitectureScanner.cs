using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace MermaidPreviewer
{
    internal static class ArchitectureScanner
    {
        private static readonly string[] FolderBuckets =
        {
            "Controllers",
            "Systems",
            "Models",
            "Commands",
            "Queries",
            "Events",
            "Utilities",
            "Infrastructure"
        };

        private static readonly string[] CategoryOrder =
        {
            "Controllers",
            "Systems",
            "Models",
            "Commands",
            "Queries",
            "Events",
            "Utilities",
            "Infrastructure",
            "Misc"
        };

        private static readonly Dictionary<string, string> SuffixBuckets = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "Controller", "Controllers" },
            { "System", "Systems" },
            { "Model", "Models" },
            { "Command", "Commands" },
            { "Query", "Queries" },
            { "Event", "Events" }
        };

        public static bool ScanAndWrite(ArchitectureScannerSettings settings, out ArchitectureScanIndex index, out string error)
        {
            index = null;
            error = null;

            if (settings == null)
            {
                error = "Settings not loaded.";
                return false;
            }

            settings.EnsureDefaults();

            var scanRootFull = ResolveToFullPath(settings.scanRootPath);
            if (string.IsNullOrEmpty(scanRootFull) || !Directory.Exists(scanRootFull))
            {
                error = $"Scan root not found: {settings.scanRootPath}";
                return false;
            }

            var outputRootFull = ResolveToFullPath(settings.outputFolderPath);
            if (string.IsNullOrEmpty(outputRootFull))
            {
                error = "Output folder is invalid.";
                return false;
            }

            Directory.CreateDirectory(outputRootFull);

            var files = CollectFiles(scanRootFull, settings);
            var featureMap = BuildFeatureMap(files);
            BuildDependencies(featureMap);

            index = WriteOutputs(featureMap, settings, outputRootFull);
            return true;
        }

        public static bool TryLoadIndex(string outputFolderPath, out ArchitectureScanIndex index)
        {
            index = null;

            var outputRootFull = ResolveToFullPath(outputFolderPath);
            if (string.IsNullOrEmpty(outputRootFull))
            {
                return false;
            }

            var indexPath = Path.Combine(outputRootFull, "scan_index.json");
            if (!File.Exists(indexPath))
            {
                return false;
            }

            var json = File.ReadAllText(indexPath);
            index = JsonUtility.FromJson<ArchitectureScanIndex>(json);
            if (index == null)
            {
                return false;
            }

            if (index.features == null)
            {
                index.features = new List<ArchitectureFeatureIndex>();
            }

            return true;
        }

        public static string ResolveToFullPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return null;
            }

            if (Path.IsPathRooted(path))
            {
                return Path.GetFullPath(path);
            }

            var projectRoot = Directory.GetParent(Application.dataPath).FullName;
            return Path.GetFullPath(Path.Combine(projectRoot, path));
        }

        private static List<FileScanInfo> CollectFiles(string scanRootFull, ArchitectureScannerSettings settings)
        {
            var results = new List<FileScanInfo>();
            var files = Directory.GetFiles(scanRootFull, "*.cs", SearchOption.AllDirectories);

            foreach (var file in files)
            {
                if (IsExcluded(file, settings.excludePatterns))
                {
                    continue;
                }

                var text = File.ReadAllText(file);
                var featureName = ResolveFeatureName(file, text, settings);
                if (string.IsNullOrEmpty(featureName))
                {
                    featureName = "Misc";
                }

                var componentName = Path.GetFileNameWithoutExtension(file);
                var category = ResolveCategory(file, componentName);
                var referencedFeatures = ExtractReferencedFeatures(text, settings);

                results.Add(new FileScanInfo
                {
                    Path = file,
                    FeatureName = featureName,
                    ComponentName = componentName,
                    Category = category,
                    ReferencedFeatures = referencedFeatures
                });
            }

            return results;
        }

        private static Dictionary<string, FeatureData> BuildFeatureMap(List<FileScanInfo> files)
        {
            var map = new Dictionary<string, FeatureData>(StringComparer.OrdinalIgnoreCase);

            foreach (var file in files)
            {
                if (!map.TryGetValue(file.FeatureName, out var feature))
                {
                    feature = new FeatureData(file.FeatureName);
                    map.Add(file.FeatureName, feature);
                }

                feature.Files.Add(file);
                feature.Components.Add(new ComponentData(file.ComponentName, file.Category, file.Path));
            }

            return map;
        }

        private static void BuildDependencies(Dictionary<string, FeatureData> features)
        {
            var featureNames = new HashSet<string>(features.Keys, StringComparer.OrdinalIgnoreCase);

            foreach (var feature in features.Values)
            {
                foreach (var file in feature.Files)
                {
                    foreach (var candidate in file.ReferencedFeatures)
                    {
                        if (featureNames.Contains(candidate) && !string.Equals(candidate, feature.Name, StringComparison.OrdinalIgnoreCase))
                        {
                            feature.Dependencies.Add(candidate);
                        }
                    }
                }
            }
        }

        private static ArchitectureScanIndex WriteOutputs(Dictionary<string, FeatureData> features, ArchitectureScannerSettings settings, string outputRootFull)
        {
            var index = new ArchitectureScanIndex
            {
                timestamp = DateTime.UtcNow.ToString("o"),
                scanRoot = settings.scanRootPath,
                l0MermaidPath = NormalizeRelativePath("L0_features_overview.mmd"),
                features = new List<ArchitectureFeatureIndex>()
            };

            var orderedFeatures = new List<FeatureData>(features.Values);
            orderedFeatures.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));

            var l0Path = Path.Combine(outputRootFull, index.l0MermaidPath);
            WriteFile(l0Path, GenerateL0Mermaid(orderedFeatures));

            foreach (var feature in orderedFeatures)
            {
                var featureFolderRel = NormalizeRelativePath(Path.Combine("Features", feature.Name));
                var featureFolderFull = Path.Combine(outputRootFull, featureFolderRel);
                Directory.CreateDirectory(featureFolderFull);

                var l1Rel = NormalizeRelativePath(Path.Combine(featureFolderRel, "L1_internal.mmd"));
                var l2Rel = NormalizeRelativePath(Path.Combine(featureFolderRel, "L2_external.mmd"));

                WriteFile(Path.Combine(outputRootFull, l1Rel), GenerateL1Mermaid(feature));
                WriteFile(Path.Combine(outputRootFull, l2Rel), GenerateL2Mermaid(feature));

                var featureIndex = new ArchitectureFeatureIndex
                {
                    id = SanitizeId(feature.Name),
                    name = feature.Name,
                    l1MermaidPath = l1Rel,
                    l2MermaidPath = l2Rel,
                    components = new List<ArchitectureComponentIndex>()
                };

                var components = new List<ComponentData>(feature.Components);
                components.Sort((a, b) =>
                {
                    var categoryCompare = string.Compare(a.Category, b.Category, StringComparison.OrdinalIgnoreCase);
                    return categoryCompare != 0
                        ? categoryCompare
                        : string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase);
                });

                foreach (var component in components)
                {
                    featureIndex.components.Add(new ArchitectureComponentIndex
                    {
                        name = component.Name,
                        category = component.Category,
                        l2MermaidPath = string.Empty
                    });
                }

                index.features.Add(featureIndex);
            }

            var indexPath = Path.Combine(outputRootFull, "scan_index.json");
            WriteFile(indexPath, JsonUtility.ToJson(index, true));
            return index;
        }

        private static string GenerateL0Mermaid(List<FeatureData> features)
        {
            var sb = new StringBuilder();
            sb.AppendLine("%% L0 Features Overview");
            sb.AppendLine("graph TD");

            foreach (var feature in features)
            {
                sb.AppendLine($"    {FormatNodeId("Feature", feature.Name)}[\"{feature.Name}\"]");
            }

            foreach (var feature in features)
            {
                var dependencies = new List<string>(feature.Dependencies);
                dependencies.Sort(StringComparer.OrdinalIgnoreCase);

                foreach (var dependency in dependencies)
                {
                    sb.AppendLine($"    {FormatNodeId("Feature", feature.Name)} --> {FormatNodeId("Feature", dependency)}");
                }
            }

            return sb.ToString();
        }

        private static string GenerateL1Mermaid(FeatureData feature)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"%% L1 {feature.Name} Internal");
            sb.AppendLine("graph TD");
            sb.AppendLine($"    subgraph {SanitizeLabel(feature.Name)}");

            var grouped = new Dictionary<string, List<ComponentData>>(StringComparer.OrdinalIgnoreCase);
            foreach (var component in feature.Components)
            {
                if (!grouped.TryGetValue(component.Category, out var list))
                {
                    list = new List<ComponentData>();
                    grouped.Add(component.Category, list);
                }

                list.Add(component);
            }

            foreach (var category in CategoryOrder)
            {
                if (!grouped.TryGetValue(category, out var list) || list.Count == 0)
                {
                    continue;
                }

                list.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
                sb.AppendLine($"        subgraph {category}");

                foreach (var component in list)
                {
                    sb.AppendLine($"            {FormatNodeId(GetComponentPrefix(category), component.Name)}[\"{component.Name}\"]");
                }

                sb.AppendLine("        end");
            }

            sb.AppendLine("    end");
            return sb.ToString();
        }

        private static string GenerateL2Mermaid(FeatureData feature)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"%% L2 {feature.Name} External Calls");
            sb.AppendLine("graph TD");

            sb.AppendLine($"    {FormatNodeId("Feature", feature.Name)}[\"{feature.Name}\"]");

            var dependencies = new List<string>(feature.Dependencies);
            dependencies.Sort(StringComparer.OrdinalIgnoreCase);

            foreach (var dependency in dependencies)
            {
                sb.AppendLine($"    {FormatNodeId("Feature", dependency)}[\"{dependency}\"]");
                sb.AppendLine($"    {FormatNodeId("Feature", feature.Name)} --> {FormatNodeId("Feature", dependency)}");
            }

            return sb.ToString();
        }

        private static string ResolveFeatureName(string path, string text, ArchitectureScannerSettings settings)
        {
            if (settings.featureMode == ArchitectureFeatureMode.Namespace)
            {
                var namespaceValue = ExtractNamespace(text);
                var feature = ExtractFeatureFromNamespace(namespaceValue, settings.namespacePrefix);
                if (!string.IsNullOrEmpty(feature))
                {
                    return feature;
                }
            }

            return ExtractFeatureFromPath(path, settings.featurePathToken);
        }

        private static string ResolveCategory(string path, string componentName)
        {
            var normalized = NormalizePath(path);
            foreach (var bucket in FolderBuckets)
            {
                var token = $"/{bucket}/";
                if (normalized.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return bucket;
                }
            }

            foreach (var suffix in SuffixBuckets)
            {
                if (componentName.EndsWith(suffix.Key, StringComparison.OrdinalIgnoreCase))
                {
                    return suffix.Value;
                }
            }

            return "Misc";
        }

        private static string ExtractFeatureFromPath(string path, string token)
        {
            if (string.IsNullOrEmpty(path) || string.IsNullOrEmpty(token))
            {
                return null;
            }

            var normalized = NormalizePath(path);
            var marker = $"/{token}/";
            var index = normalized.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
            if (index < 0)
            {
                return null;
            }

            var start = index + marker.Length;
            var nextSlash = normalized.IndexOf('/', start);
            if (nextSlash < 0)
            {
                nextSlash = normalized.Length;
            }

            return normalized.Substring(start, nextSlash - start);
        }

        private static string ExtractNamespace(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return null;
            }

            using (var reader = new StringReader(text))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    var trimmed = line.Trim();
                    if (!trimmed.StartsWith("namespace ", StringComparison.Ordinal))
                    {
                        continue;
                    }

                    trimmed = trimmed.Substring("namespace ".Length).Trim();
                    var braceIndex = trimmed.IndexOf(' ');
                    if (braceIndex > 0)
                    {
                        trimmed = trimmed.Substring(0, braceIndex);
                    }

                    braceIndex = trimmed.IndexOf('{');
                    if (braceIndex > 0)
                    {
                        trimmed = trimmed.Substring(0, braceIndex);
                    }

                    return trimmed.Trim();
                }
            }

            return null;
        }

        private static string ExtractFeatureFromNamespace(string namespaceValue, string namespacePrefix)
        {
            if (string.IsNullOrEmpty(namespaceValue) || string.IsNullOrEmpty(namespacePrefix))
            {
                return null;
            }

            var prefix = namespacePrefix.TrimEnd('.') + ".";
            if (!namespaceValue.StartsWith(prefix, StringComparison.Ordinal))
            {
                return null;
            }

            var remainder = namespaceValue.Substring(prefix.Length);
            var dotIndex = remainder.IndexOf('.');
            return dotIndex >= 0 ? remainder.Substring(0, dotIndex) : remainder;
        }

        private static HashSet<string> ExtractReferencedFeatures(string text, ArchitectureScannerSettings settings)
        {
            var results = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var ns in ExtractUsingNamespaces(text))
            {
                AddFeatureFromNamespace(ns, settings.namespacePrefix, results);
                AddFeatureFromFeaturesNamespace(ns, results);
            }

            AddFeatureTokens(text, settings.namespacePrefix, results);
            AddFeatureTokens(text, "Features", results);

            return results;
        }

        private static IEnumerable<string> ExtractUsingNamespaces(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                yield break;
            }

            using (var reader = new StringReader(text))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    var trimmed = line.Trim();
                    if (!trimmed.StartsWith("using ", StringComparison.Ordinal))
                    {
                        continue;
                    }

                    trimmed = trimmed.Substring("using ".Length).Trim();
                    if (trimmed.StartsWith("static ", StringComparison.Ordinal))
                    {
                        trimmed = trimmed.Substring("static ".Length).Trim();
                    }

                    var semicolon = trimmed.IndexOf(';');
                    if (semicolon >= 0)
                    {
                        trimmed = trimmed.Substring(0, semicolon).Trim();
                    }

                    if (!string.IsNullOrEmpty(trimmed))
                    {
                        yield return trimmed;
                    }
                }
            }
        }

        private static void AddFeatureFromNamespace(string namespaceValue, string namespacePrefix, HashSet<string> results)
        {
            if (string.IsNullOrEmpty(namespaceValue) || string.IsNullOrEmpty(namespacePrefix))
            {
                return;
            }

            var prefix = namespacePrefix.TrimEnd('.') + ".";
            if (!namespaceValue.StartsWith(prefix, StringComparison.Ordinal))
            {
                return;
            }

            var remainder = namespaceValue.Substring(prefix.Length);
            var dotIndex = remainder.IndexOf('.');
            var feature = dotIndex >= 0 ? remainder.Substring(0, dotIndex) : remainder;
            if (!string.IsNullOrEmpty(feature))
            {
                results.Add(feature);
            }
        }

        private static void AddFeatureFromFeaturesNamespace(string namespaceValue, HashSet<string> results)
        {
            if (string.IsNullOrEmpty(namespaceValue))
            {
                return;
            }

            var marker = ".Features.";
            var index = namespaceValue.IndexOf(marker, StringComparison.Ordinal);
            if (index < 0)
            {
                return;
            }

            var remainder = namespaceValue.Substring(index + marker.Length);
            var dotIndex = remainder.IndexOf('.');
            var feature = dotIndex >= 0 ? remainder.Substring(0, dotIndex) : remainder;
            if (!string.IsNullOrEmpty(feature))
            {
                results.Add(feature);
            }
        }

        private static void AddFeatureTokens(string text, string prefix, HashSet<string> results)
        {
            if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(prefix))
            {
                return;
            }

            var marker = prefix.TrimEnd('.') + ".";
            var index = 0;
            while (index < text.Length)
            {
                index = text.IndexOf(marker, index, StringComparison.Ordinal);
                if (index < 0)
                {
                    break;
                }

                var start = index + marker.Length;
                var feature = ReadIdentifier(text, start);
                if (!string.IsNullOrEmpty(feature))
                {
                    results.Add(feature);
                }

                index = start;
            }
        }

        private static string ReadIdentifier(string text, int startIndex)
        {
            var end = startIndex;
            while (end < text.Length)
            {
                var ch = text[end];
                if (!(char.IsLetterOrDigit(ch) || ch == '_'))
                {
                    break;
                }

                end++;
            }

            return end > startIndex ? text.Substring(startIndex, end - startIndex) : null;
        }

        private static bool IsExcluded(string path, List<string> patterns)
        {
            if (patterns == null || patterns.Count == 0 || string.IsNullOrEmpty(path))
            {
                return false;
            }

            var normalized = NormalizePath(path);
            foreach (var pattern in patterns)
            {
                if (string.IsNullOrWhiteSpace(pattern))
                {
                    continue;
                }

                var token = pattern.Replace("**", string.Empty).Replace("*", string.Empty).Replace("\\", "/").Trim('/');
                if (string.IsNullOrEmpty(token))
                {
                    continue;
                }

                if (normalized.IndexOf($"/{token}/", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }

                if (normalized.EndsWith($"/{token}", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static string NormalizePath(string path)
        {
            return path.Replace('\\', '/');
        }

        private static void WriteFile(string path, string contents)
        {
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(path, contents ?? string.Empty);
        }

        private static string NormalizeRelativePath(string path)
        {
            return path.Replace('\\', '/');
        }

        private static string SanitizeId(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return "Unknown";
            }

            var sb = new StringBuilder();
            foreach (var ch in value)
            {
                sb.Append(char.IsLetterOrDigit(ch) || ch == '_' ? ch : '_');
            }

            return sb.Length == 0 ? "Unknown" : sb.ToString();
        }

        private static string SanitizeLabel(string value)
        {
            return string.IsNullOrEmpty(value) ? "Unknown" : value.Replace('"', '_');
        }

        private static string FormatNodeId(string prefix, string name)
        {
            return $"{prefix}::{SanitizeId(name)}";
        }

        private static string GetComponentPrefix(string category)
        {
            switch (category)
            {
                case "Controllers":
                    return "Controller";
                case "Systems":
                    return "System";
                case "Models":
                    return "Model";
                case "Commands":
                    return "Command";
                case "Queries":
                    return "Query";
                case "Events":
                    return "Event";
                case "Utilities":
                    return "Utility";
                case "Infrastructure":
                    return "Infrastructure";
                default:
                    return "Component";
            }
        }

        private sealed class FileScanInfo
        {
            public string Path;
            public string FeatureName;
            public string ComponentName;
            public string Category;
            public HashSet<string> ReferencedFeatures;
        }

        private sealed class FeatureData
        {
            public FeatureData(string name)
            {
                Name = name;
            }

            public string Name { get; }
            public List<FileScanInfo> Files { get; } = new List<FileScanInfo>();
            public List<ComponentData> Components { get; } = new List<ComponentData>();
            public HashSet<string> Dependencies { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }

        private sealed class ComponentData
        {
            public ComponentData(string name, string category, string path)
            {
                Name = name;
                Category = category;
                Path = path;
            }

            public string Name { get; }
            public string Category { get; }
            public string Path { get; }
        }
    }
}
