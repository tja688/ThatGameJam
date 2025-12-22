using UnityEditor;

namespace MermaidPreviewer
{
    internal static class MermaidPreviewerPrefs
    {
        public const string MmdcPathKey = "MermaidPreviewer.MmdcPath";
        public const string PuppeteerConfigKey = "MermaidPreviewer.PuppeteerConfigPath";
        public const string RenderScaleKey = "MermaidPreviewer.RenderScale";

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

        public static float RenderScale
        {
            get => EditorPrefs.GetFloat(RenderScaleKey, 3.0f);
            set => EditorPrefs.SetFloat(RenderScaleKey, value);
        }
    }
}
