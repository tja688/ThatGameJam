#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using QFramework;
using UnityEditor;
using UnityEngine;

namespace ThatGameJam.EditorTools
{
    internal static class QFrameworkDocExporter
    {
        private const string MenuPath = "QFramework/Docs/Export Manual + API (Single MD)...";

        [MenuItem(MenuPath, priority = 2000)]
        private static void ExportSingleMarkdown()
        {
            if (EditorApplication.isCompiling)
            {
                EditorUtility.DisplayDialog("QFramework Doc Exporter",
                    "Unity is compiling scripts. Please retry after compilation finishes.", "OK");
                return;
            }

            var defaultName = $"QFramework_Docs_{DateTime.Now:yyyyMMdd_HHmm}.md";
            var savePath = EditorUtility.SaveFilePanel("Export QFramework Docs (Single Markdown)",
                Application.dataPath, defaultName, "md");

            if (string.IsNullOrWhiteSpace(savePath))
            {
                return;
            }

            var builder = new StringBuilder(4 * 1024 * 1024);
            builder.AppendLine("# QFramework Docs (Exported)");
            builder.AppendLine();
            builder.AppendLine($"- Generated: `{DateTime.Now:yyyy-MM-dd HH:mm:ss}`");
            builder.AppendLine($"- Project: `{Application.productName}`");
            builder.AppendLine();
            builder.AppendLine("## Table of Contents");
            builder.AppendLine("- [User Manual / 用户手册](#user-manual--用户手册)");
            builder.AppendLine("- [API Doc / API 文档](#api-doc--api-文档)");
            builder.AppendLine();
            builder.AppendLine("## User Manual / 用户手册");
            builder.AppendLine();

            var manualResult = ExportManual(builder);
            builder.AppendLine();
            builder.AppendLine("## API Doc / API 文档");
            builder.AppendLine();
            var apiResult = ExportApiDocs(builder);

            try
            {
                File.WriteAllText(savePath, builder.ToString(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
                EditorUtility.RevealInFinder(savePath);

                EditorUtility.DisplayDialog("QFramework Doc Exporter",
                    $"Exported successfully.\n\nUser Manual files: {manualResult.FileCount}\nAPI classes: {apiResult.ClassCount}",
                    "OK");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorUtility.DisplayDialog("QFramework Doc Exporter", $"Export failed:\n{exception.Message}", "OK");
            }
        }

        private readonly struct ManualExportResult
        {
            public int FileCount { get; }
            public string RootFolder { get; }

            public ManualExportResult(int fileCount, string rootFolder)
            {
                FileCount = fileCount;
                RootFolder = rootFolder;
            }
        }

        private static ManualExportResult ExportManual(StringBuilder builder)
        {
            var folderPath = TryGetGuidelineMarkdownRootFolder();

            if (string.IsNullOrWhiteSpace(folderPath) || !Directory.Exists(folderPath))
            {
                builder.AppendLine("> Manual markdown folder not found.");
                builder.AppendLine();
                builder.AppendLine("Expected a `Resources/EditorGuideline/PositionMarkForLoad.txt` marker under QFramework.");
                return new ManualExportResult(0, folderPath ?? string.Empty);
            }

            var mdFiles = Directory.GetFiles(folderPath, "*.md", SearchOption.AllDirectories)
                .OrderBy(path => ToUnixPath(path.Substring(folderPath.Length).TrimStart('\\', '/')))
                .ToList();

            if (mdFiles.Count == 0)
            {
                builder.AppendLine("> No `*.md` found under manual folder.");
                return new ManualExportResult(0, folderPath);
            }

            builder.AppendLine("### Manual Index");
            builder.AppendLine();

            var manualEntries = mdFiles
                .Select(filePath =>
                {
                    var relativePath = ToUnixPath(filePath.Substring(folderPath.Length).TrimStart('\\', '/'));
                    var title = relativePath.EndsWith(".md", StringComparison.OrdinalIgnoreCase)
                        ? relativePath.Substring(0, relativePath.Length - 3)
                        : relativePath;
                    var heading = $"Manual: {title}";
                    return (filePath, title, heading);
                })
                .ToList();

            foreach (var entry in manualEntries)
            {
                builder.AppendLine($"- [{EscapeMarkdownText(entry.title)}](#{Slugify(heading: entry.heading)})");
            }

            builder.AppendLine();
            builder.AppendLine("---");
            builder.AppendLine();

            foreach (var entry in manualEntries)
            {
                builder.AppendLine($"### {EscapeMarkdownText(entry.heading)}");
                builder.AppendLine();
                builder.AppendLine($"- Source: `{ToUnixPath(entry.filePath)}`");
                builder.AppendLine();

                try
                {
                    var content = File.ReadAllText(entry.filePath);
                    content = NormalizeLineEndings(content);
                    builder.AppendLine(content);
                }
                catch (Exception exception)
                {
                    builder.AppendLine($"> Failed to read: `{ToUnixPath(entry.filePath)}`");
                    builder.AppendLine($"> {EscapeMarkdownText(exception.Message)}");
                }

                builder.AppendLine();
                builder.AppendLine("---");
                builder.AppendLine();
            }

            return new ManualExportResult(mdFiles.Count, folderPath);
        }

        private readonly struct ApiExportResult
        {
            public int ClassCount { get; }

            public ApiExportResult(int classCount)
            {
                ClassCount = classCount;
            }
        }

        private static ApiExportResult ExportApiDocs(StringBuilder builder)
        {
            IReadOnlyList<Type> typesWithClassApi;

            try
            {
                typesWithClassApi = TypeCache.GetTypesWithAttribute<ClassAPIAttribute>().ToList();
            }
            catch
            {
                typesWithClassApi = GetTypesWithAttributeFallback<ClassAPIAttribute>().ToList();
            }

            var classInfos = typesWithClassApi
                .Select(type => (type, attr: type.GetCustomAttribute<ClassAPIAttribute>(inherit: false)))
                .Where(pair => pair.attr != null)
                .Select(pair => new ClassApiInfo(pair.type, pair.attr))
                .OrderBy(info => info.GroupName, StringComparer.Ordinal)
                .ThenBy(info => info.RenderOrder)
                .ThenBy(info => info.DisplayMenuName, StringComparer.Ordinal)
                .ToList();

            if (classInfos.Count == 0)
            {
                builder.AppendLine("> No `[ClassAPI]` types found. Ensure QFramework.CoreKit is present and compiled.");
                return new ApiExportResult(0);
            }

            builder.AppendLine("### API Index");
            builder.AppendLine();

            foreach (var group in classInfos.GroupBy(c => c.GroupName).OrderBy(g => g.Key, StringComparer.Ordinal))
            {
                builder.AppendLine($"- [{EscapeMarkdownText(group.Key)}](#{Slugify($"API Group: {group.Key}")})");
                foreach (var clazz in group)
                {
                    builder.AppendLine($"  - [{EscapeMarkdownText(clazz.DisplayMenuName)}](#{Slugify(clazz.GetClassHeading())})");
                }
            }

            builder.AppendLine();
            builder.AppendLine("---");
            builder.AppendLine();

            foreach (var group in classInfos.GroupBy(c => c.GroupName).OrderBy(g => g.Key, StringComparer.Ordinal))
            {
                builder.AppendLine($"### {EscapeMarkdownText($"API Group: {group.Key}")}");
                builder.AppendLine();

                foreach (var clazz in group)
                {
                    AppendClassApi(builder, clazz.Type, clazz);
                    builder.AppendLine();
                    builder.AppendLine("---");
                    builder.AppendLine();
                }
            }

            return new ApiExportResult(classInfos.Count);
        }

        private static void AppendClassApi(StringBuilder builder, Type type, ClassApiInfo classApiInfo)
        {
            builder.AppendLine($"#### {EscapeMarkdownText(classApiInfo.GetClassHeading())}");
            builder.AppendLine();
            builder.AppendLine($"- Type: `{type.FullName}`");
            builder.AppendLine($"- Namespace: `{type.Namespace}`");
            builder.AppendLine();

            var descCN = type.GetCustomAttribute<APIDescriptionCNAttribute>(inherit: false)?.Description;
            var descEN = type.GetCustomAttribute<APIDescriptionENAttribute>(inherit: false)?.Description;

            if (!string.IsNullOrWhiteSpace(descCN) || !string.IsNullOrWhiteSpace(descEN))
            {
                builder.AppendLine("**Description / 描述**");
                builder.AppendLine();
                if (!string.IsNullOrWhiteSpace(descCN))
                {
                    builder.AppendLine($"- 描述: {EscapeMarkdownText(descCN)}");
                }

                if (!string.IsNullOrWhiteSpace(descEN))
                {
                    builder.AppendLine($"- Description: {EscapeMarkdownText(descEN)}");
                }

                builder.AppendLine();
            }

            var classExample = type.GetCustomAttribute<APIExampleCodeAttribute>(inherit: false)?.Code;
            if (!string.IsNullOrWhiteSpace(classExample))
            {
                builder.AppendLine("**Example / 示例**");
                builder.AppendLine();
                builder.AppendLine("```csharp");
                builder.AppendLine(NormalizeLineEndings(classExample).Trim());
                builder.AppendLine("```");
                builder.AppendLine();
            }

            var properties = type.GetProperties()
                .Where(p => p.GetCustomAttribute<PropertyAPIAttribute>(inherit: false) != null)
                .OrderBy(p => p.Name, StringComparer.Ordinal)
                .ToList();

            var methods = type.GetMethods()
                .Where(m => m.GetCustomAttribute<MethodAPIAttribute>(inherit: false) != null)
                .OrderBy(m => m.Name, StringComparer.Ordinal)
                .ThenBy(m => m.GetParameters().Length)
                .ToList();

            if (properties.Count > 0)
            {
                builder.AppendLine("**Properties / 属性**");
                builder.AppendLine();
                foreach (var property in properties)
                {
                    AppendPropertyApi(builder, property);
                }

                builder.AppendLine();
            }

            if (methods.Count > 0)
            {
                builder.AppendLine("**Methods / 方法**");
                builder.AppendLine();
                foreach (var method in methods)
                {
                    AppendMethodApi(builder, method);
                }

                builder.AppendLine();
            }
        }

        private static void AppendPropertyApi(StringBuilder builder, PropertyInfo property)
        {
            var signature = $"{FormatTypeName(property.PropertyType)} {property.Name}";
            builder.AppendLine($"##### {EscapeMarkdownText(signature)}");
            builder.AppendLine();

            var descCN = property.GetCustomAttribute<APIDescriptionCNAttribute>(inherit: false)?.Description;
            var descEN = property.GetCustomAttribute<APIDescriptionENAttribute>(inherit: false)?.Description;

            if (!string.IsNullOrWhiteSpace(descCN))
            {
                builder.AppendLine($"- 描述: {EscapeMarkdownText(descCN)}");
            }

            if (!string.IsNullOrWhiteSpace(descEN))
            {
                builder.AppendLine($"- Description: {EscapeMarkdownText(descEN)}");
            }

            var example = property.GetCustomAttribute<APIExampleCodeAttribute>(inherit: false)?.Code;
            if (!string.IsNullOrWhiteSpace(example))
            {
                builder.AppendLine();
                builder.AppendLine("```csharp");
                builder.AppendLine(NormalizeLineEndings(example).Trim());
                builder.AppendLine("```");
            }

            builder.AppendLine();
        }

        private static void AppendMethodApi(StringBuilder builder, MethodInfo method)
        {
            var signature = FormatMethodSignature(method);
            builder.AppendLine($"##### {EscapeMarkdownText(signature)}");
            builder.AppendLine();

            var descCN = method.GetCustomAttribute<APIDescriptionCNAttribute>(inherit: false)?.Description;
            var descEN = method.GetCustomAttribute<APIDescriptionENAttribute>(inherit: false)?.Description;

            if (!string.IsNullOrWhiteSpace(descCN))
            {
                builder.AppendLine($"- 描述: {EscapeMarkdownText(descCN)}");
            }

            if (!string.IsNullOrWhiteSpace(descEN))
            {
                builder.AppendLine($"- Description: {EscapeMarkdownText(descEN)}");
            }

            var example = method.GetCustomAttribute<APIExampleCodeAttribute>(inherit: false)?.Code;
            if (!string.IsNullOrWhiteSpace(example))
            {
                builder.AppendLine();
                builder.AppendLine("```csharp");
                builder.AppendLine(NormalizeLineEndings(example).Trim());
                builder.AppendLine("```");
            }

            builder.AppendLine();
        }

        private static string TryGetGuidelineMarkdownRootFolder()
        {
            try
            {
                var marker = Resources.Load<TextAsset>("EditorGuideline/PositionMarkForLoad");
                if (marker != null)
                {
                    var markerPath = AssetDatabase.GetAssetPath(marker);
                    if (!string.IsNullOrWhiteSpace(markerPath))
                    {
                        var folder = Path.GetDirectoryName(markerPath);
                        if (!string.IsNullOrWhiteSpace(folder))
                        {
                            return folder;
                        }
                    }
                }
            }
            catch
            {
                // ignore
            }

            const string fallbackMarkerPath =
                "Assets/QFramework/Toolkits/_CoreKit/Internal/Guidline/Editor/Resources/EditorGuideline/PositionMarkForLoad.txt";

            if (File.Exists(fallbackMarkerPath))
            {
                return Path.GetDirectoryName(fallbackMarkerPath);
            }

            return string.Empty;
        }

        private static IEnumerable<Type> GetTypesWithAttributeFallback<TAttribute>() where TAttribute : Attribute
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type[] types;
                try
                {
                    types = assembly.GetTypes();
                }
                catch (ReflectionTypeLoadException exception)
                {
                    types = exception.Types.Where(t => t != null).ToArray();
                }
                catch
                {
                    continue;
                }

                foreach (var type in types)
                {
                    if (type == null) continue;
                    if (type.GetCustomAttribute<TAttribute>(inherit: false) != null)
                    {
                        yield return type;
                    }
                }
            }
        }

        private static string FormatMethodSignature(MethodInfo method)
        {
            var parameters = method.GetParameters()
                .Select(p => $"{FormatTypeName(p.ParameterType)} {p.Name}")
                .ToArray();

            var methodName = method.Name;
            if (method.IsGenericMethodDefinition)
            {
                var args = method.GetGenericArguments().Select(t => t.Name).ToArray();
                methodName += "<" + string.Join(", ", args) + ">";
            }

            return $"{FormatTypeName(method.ReturnType)} {methodName}({string.Join(", ", parameters)})";
        }

        private static string FormatTypeName(Type type)
        {
            if (type == null) return "void";

            if (type == typeof(void)) return "void";
            if (type == typeof(string)) return "string";
            if (type == typeof(int)) return "int";
            if (type == typeof(float)) return "float";
            if (type == typeof(double)) return "double";
            if (type == typeof(bool)) return "bool";
            if (type == typeof(object)) return "object";

            if (type.IsByRef)
            {
                return "ref " + FormatTypeName(type.GetElementType());
            }

            if (type.IsArray)
            {
                return FormatTypeName(type.GetElementType()) + "[]";
            }

            if (type.IsGenericType)
            {
                var name = type.Name;
                var tick = name.IndexOf('`');
                if (tick >= 0)
                {
                    name = name.Substring(0, tick);
                }

                var args = type.GetGenericArguments().Select(FormatTypeName).ToArray();
                return $"{name}<{string.Join(", ", args)}>";
            }

            return type.Name;
        }

        private static string NormalizeLineEndings(string text)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;
            return text.Replace("\r\n", "\n").Replace('\r', '\n');
        }

        private static string ToUnixPath(string path)
        {
            return string.IsNullOrEmpty(path) ? string.Empty : path.Replace('\\', '/');
        }

        private static string EscapeMarkdownText(string text)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;
            return text.Replace("\\", "\\\\").Replace("`", "\\`").Replace("|", "\\|");
        }

        private static string Slugify(string heading)
        {
            if (string.IsNullOrWhiteSpace(heading)) return string.Empty;

            var normalized = heading.Trim().ToLowerInvariant();
            var buffer = new StringBuilder(normalized.Length);

            bool lastWasDash = false;
            foreach (var ch in normalized)
            {
                if ((ch >= 'a' && ch <= 'z') || (ch >= '0' && ch <= '9') || ch >= 0x4e00 && ch <= 0x9fff)
                {
                    buffer.Append(ch);
                    lastWasDash = false;
                }
                else if (char.IsWhiteSpace(ch) || ch == '-' || ch == '_' || ch == '/' || ch == '.')
                {
                    if (!lastWasDash)
                    {
                        buffer.Append('-');
                        lastWasDash = true;
                    }
                }
            }

            var result = buffer.ToString().Trim('-');
            return string.IsNullOrEmpty(result) ? "section" : result;
        }

        private readonly struct ClassApiInfo
        {
            public Type Type { get; }
            public string GroupName { get; }
            public string DisplayMenuName { get; }
            public string DisplayClassName { get; }
            public int RenderOrder { get; }

            public ClassApiInfo(Type type, ClassAPIAttribute attribute)
            {
                Type = type;
                GroupName = attribute.GroupName ?? string.Empty;
                DisplayMenuName = attribute.DisplayMenuName ?? type.Name;
                DisplayClassName = attribute.DisplayClassName;
                RenderOrder = attribute.RenderOrder;
            }

            public string GetClassHeading()
            {
                var className = string.IsNullOrWhiteSpace(DisplayClassName) ? Type.Name : DisplayClassName;
                return $"{DisplayMenuName} ({className})";
            }
        }
    }
}
#endif
