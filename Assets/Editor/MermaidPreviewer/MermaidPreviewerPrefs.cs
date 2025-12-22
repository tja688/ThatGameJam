using UnityEditor;

namespace MermaidPreviewer
{
    internal static class MermaidPreviewerPrefs
    {
        public const string MmdcPathKey = "MermaidPreviewer.MmdcPath";
        public const string PuppeteerConfigKey = "MermaidPreviewer.PuppeteerConfigPath";

        public static string MmdcPath
        {
            get
            {
                var path = EditorPrefs.GetString(MmdcPathKey, "mmdc");
                return string.IsNullOrWhiteSpace(path) ? "mmdc" : path;
            }
        }

        public static string PuppeteerConfigPath
        {
            get
            {
                var path = EditorPrefs.GetString(PuppeteerConfigKey, string.Empty);
                return string.IsNullOrWhiteSpace(path) ? string.Empty : path;
            }
        }
    }
}
