using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace UnityCodeIntel.Editor
{
    public class CodeIntelWindow : EditorWindow
    {
        [MenuItem("Window/CodeIntel/Dashboard")]
        public static void ShowWindow()
        {
            GetWindow<CodeIntelWindow>("CodeIntel");
        }

        private Vector2 _scrollPos;
        private string _logContent = "";
        private float _lastLogUpdate = 0;
        private bool _autoScroll = true;

        private void OnEnable()
        {
            // Optional: Subscribe to events if any
        }

        private void OnGUI()
        {
            GUILayout.Label("Unity Code Intel Service", EditorStyles.boldLabel);

            DrawStatus();
            DrawControls();
            DrawLogs();
        }

        private void DrawStatus()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Status", EditorStyles.boldLabel);

            var bridge = CodeIntelManager.Bridge;
            var omnisharp = CodeIntelManager.OmniSharp;

            // Bridge Status
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.PrefixLabel("Bridge Server:");
                if (bridge.IsRunning)
                    GUILayout.Label($"Running (Port: {bridge.Port})", EditorStyles.wordWrappedLabel);
                else
                    GUILayout.Label("Stopped", EditorStyles.wordWrappedLabel);
            }

            // OmniSharp Status
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.PrefixLabel("OmniSharp:");
                
                var status = omnisharp.Status;
                if (status == ServiceStatus.Running)
                {
                    GUILayout.Label($"Running (PID: {omnisharp.Pid}, Port: {omnisharp.Port})", EditorStyles.wordWrappedLabel);
                }
                else if (status == ServiceStatus.Starting)
                {
                    GUILayout.Label("Starting... (Verifying Health)", EditorStyles.wordWrappedLabel);
                }
                else if (status == ServiceStatus.Error)
                {
                    Color oldColor = GUI.color;
                    GUI.color = Color.red;
                    GUILayout.Label("Error / Unhealthy", EditorStyles.wordWrappedLabel);
                    GUI.color = oldColor;
                }
                else
                {
                    GUILayout.Label("Stopped", EditorStyles.wordWrappedLabel);
                }
            }
            
            // Health Check Status (Last successful ping)
            if (omnisharp.Status == ServiceStatus.Running || omnisharp.Status == ServiceStatus.Starting)
            {
                 long lastOk = omnisharp.LastOkTimestamp;
                 string lastOkStr = lastOk > 0 ? DateTimeOffset.FromUnixTimeMilliseconds(lastOk).LocalDateTime.ToString("HH:mm:ss") : "Never";
                 EditorGUILayout.LabelField("Last Health Check:", lastOkStr);
            }
        }

        private void DrawControls()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Controls", EditorStyles.boldLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Start Services"))
                {
                    CodeIntelManager.StartServices();
                }

                if (GUILayout.Button("Stop Services"))
                {
                    CodeIntelManager.StopServices();
                }
                
                if (GUILayout.Button("Reload Config"))
                {
                    CodeIntelManager.ReloadConfig();
                }
            }
            
            if (GUILayout.Button("Check Health"))
            {
                _ = CodeIntelManager.OmniSharp.CheckHealthAsync(force: true);
            }
        }

        private void DrawLogs()
        {
            EditorGUILayout.Space();
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Logs (OmniSharp)", EditorStyles.boldLabel);
                _autoScroll = EditorGUILayout.Toggle("Auto Scroll", _autoScroll);
            }

            string logPath = CodeIntelManager.OmniSharp.LogFilePath;
            if (string.IsNullOrEmpty(logPath))
            {
                EditorGUILayout.HelpBox("OmniSharp not started yet.", MessageType.Info);
                return;
            }

            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos, GUILayout.Height(300));
            EditorGUILayout.TextArea(_logContent);
            EditorGUILayout.EndScrollView();
            
            if (GUILayout.Button("Open Log File"))
            {
                if (File.Exists(logPath)) EditorUtility.RevealInFinder(logPath);
            }
        }

        private void Update()
        {
            // Refresh logs every 1 second
            if (Time.realtimeSinceStartup - _lastLogUpdate > 1.0f)
            {
                UpdateLogs();
                _lastLogUpdate = Time.realtimeSinceStartup;
                Repaint();
            }
        }

        private void UpdateLogs()
        {
            string logPath = CodeIntelManager.OmniSharp.LogFilePath;
            if (!string.IsNullOrEmpty(logPath) && File.Exists(logPath))
            {
                try
                {
                    // Read last 50 lines
                    var lines = File.ReadLines(logPath).Reverse().Take(50).Reverse().ToArray();
                    _logContent = string.Join("\n", lines);
                    
                    if (_autoScroll) _scrollPos.y = float.MaxValue;
                }
                catch
                {
                    // Ignore file access errors (e.g. being written to)
                }
            }
        }
    }
}
