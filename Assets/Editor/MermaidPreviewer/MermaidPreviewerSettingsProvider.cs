using UnityEditor;

namespace MermaidPreviewer
{
    internal static class MermaidPreviewerSettingsProvider
    {
        [SettingsProvider]
        public static SettingsProvider CreateProvider()
        {
            return new SettingsProvider("Preferences/Mermaid Previewer", SettingsScope.User)
            {
                label = "Mermaid Previewer",
                guiHandler = OnGUI
            };
        }

        private static void OnGUI(string searchContext)
        {
            EditorGUILayout.LabelField("Mermaid CLI", EditorStyles.boldLabel);
            var mmdcPath = EditorPrefs.GetString(MermaidPreviewerPrefs.MmdcPathKey, "mmdc");
            var newMmdcPath = EditorGUILayout.TextField("mmdc Path", mmdcPath);
            if (newMmdcPath != mmdcPath)
            {
                EditorPrefs.SetString(MermaidPreviewerPrefs.MmdcPathKey, newMmdcPath);
            }

            EditorGUILayout.Space(6);
            var puppeteerPath = EditorPrefs.GetString(MermaidPreviewerPrefs.PuppeteerConfigKey, string.Empty);
            var newPuppeteerPath = EditorGUILayout.TextField("Puppeteer Config", puppeteerPath);
            if (newPuppeteerPath != puppeteerPath)
            {
                EditorPrefs.SetString(MermaidPreviewerPrefs.PuppeteerConfigKey, newPuppeteerPath);
            }

            EditorGUILayout.HelpBox("Leave mmdc path empty to use PATH. Puppeteer config is optional.", MessageType.Info);
        }
    }
}
