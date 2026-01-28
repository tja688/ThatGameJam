using System;
using System.IO;
using UnityEngine;

namespace UnityCodeIntel.Editor
{
    [Serializable]
    public class BridgeConfig
    {
        public int bridgePort = 0;
        public string bindAddress = "127.0.0.1";
        public string token = "";
        public int omnisharpPort = 0;
        public string omnisharpExePath = "Tools/CodeIntel/omnisharp/OmniSharp.exe";
        public string omnisharpJsonPath = "Tools/CodeIntel/omnisharp.json";
        public string solutionPath = "";
        public bool autoStartOnEditorLaunch = true;
        public bool autoRestartOnCompile = true;
        public int restartDebounceMs = 8000;
        public int restartCooldownMs = 15000;
        public int maxRestartsPer10Min = 20;
        public string logDir = "Library/CodeIntelLogs";

        public static BridgeConfig Load(string path)
        {
            if (File.Exists(path))
            {
                try
                {
                    return JsonUtility.FromJson<BridgeConfig>(File.ReadAllText(path));
                }
                catch (Exception e)
                {
                    Debug.LogError($"[CodeIntel] Failed to load config: {e.Message}");
                }
            }
            return new BridgeConfig();
        }
    }
}
