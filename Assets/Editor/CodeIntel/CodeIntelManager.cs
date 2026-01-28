using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace UnityCodeIntel.Editor
{
    [InitializeOnLoad]
    public static class CodeIntelManager
    {
        public static OmniSharpProcess OmniSharp { get; private set; }
        public static BridgeServer Bridge { get; private set; }
        public static BridgeConfig Config { get; private set; }
        
        private static string _projectRoot;
        private const string PID_KEY = "CodeIntel_OmniSharp_PID";
        
        private static float _lastHeartbeatTime;
        private static List<float> _restartTimestamps = new List<float>();

        static CodeIntelManager()
        {
            _projectRoot = Directory.GetCurrentDirectory();
            Config = BridgeConfig.Load(Path.Combine(_projectRoot, "Tools/CodeIntel/bridge-config.json"));
            
            // Cleanup zombie process if any from previous domain/session
            CleanupZombieProcess();

            OmniSharp = new OmniSharpProcess();
            Bridge = new BridgeServer(OmniSharp);

            EditorApplication.quitting += Shutdown;
            AppDomain.CurrentDomain.DomainUnload += OnDomainUnload;
            EditorApplication.update += OnUpdate;
            
            if (Config.autoStartOnEditorLaunch)
            {
                EditorApplication.delayCall += StartServices;
            }
        }

        public static void ReloadConfig()
        {
            Config = BridgeConfig.Load(Path.Combine(_projectRoot, "Tools/CodeIntel/bridge-config.json"));
        }

        public static void StartServices()
        {
            if (Config == null) ReloadConfig();
            
            if (!OmniSharp.IsRunning)
            {
                OmniSharp.Start(_projectRoot, Config);
                if (OmniSharp.Pid > 0)
                {
                    EditorPrefs.SetInt(PID_KEY, OmniSharp.Pid);
                }
            }

            if (!Bridge.IsRunning)
            {
                Bridge.Start(Config);
            }
        }

        public static void StopServices()
        {
            Bridge?.Stop();
            OmniSharp?.Stop();
            EditorPrefs.DeleteKey(PID_KEY);
        }

        private static void Shutdown()
        {
            StopServices();
        }

        private static void OnDomainUnload(object sender, EventArgs e)
        {
            StopServices();
        }

        private static void OnUpdate()
        {
            if (Time.realtimeSinceStartup - _lastHeartbeatTime > 30.0f)
            {
                _lastHeartbeatTime = Time.realtimeSinceStartup;
                CheckHeartbeat();
            }
        }

        private static async void CheckHeartbeat()
        {
            // Only check if we think it's running
            if (OmniSharp.Status == ServiceStatus.Running)
            {
                bool alive = await OmniSharp.CheckHealthAsync();
                if (!alive)
                {
                    Debug.LogWarning("[CodeIntel] OmniSharp heartbeat failed. Service marked as unhealthy.");
                    OmniSharp.MarkAsUnhealthy();
                    AttemptAutoRestart();
                }
            }
        }

        private static void AttemptAutoRestart()
        {
            // Simple logic: if we have auto-restart enabled (borrowing autoRestartOnCompile flag or just assuming default behavior for resilience)
            // Using maxRestartsPer10Min to throttle.
            
            float now = Time.realtimeSinceStartup;
            _restartTimestamps.RemoveAll(t => now - t > 600f); // 10 minutes

            if (_restartTimestamps.Count >= Config.maxRestartsPer10Min)
            {
                Debug.LogError("[CodeIntel] Max restart limit reached. Manual intervention required.");
                return;
            }

            Debug.Log("[CodeIntel] Attempting auto-restart...");
            
            // Stop first
            StopServices();
            
            // Wait a bit before starting
            EditorApplication.delayCall += () => 
            {
                // Simple delay using another delayCall to ensure next frame
                EditorApplication.delayCall += () =>
                {
                    StartServices();
                    _restartTimestamps.Add(Time.realtimeSinceStartup);
                };
            };
        }

        private static void CleanupZombieProcess()
        {
            if (EditorPrefs.HasKey(PID_KEY))
            {
                int pid = EditorPrefs.GetInt(PID_KEY);
                if (pid > 0)
                {
                    try
                    {
                        var proc = System.Diagnostics.Process.GetProcessById(pid);
                        if (!proc.HasExited)
                        {
                            proc.Kill();
                            Debug.Log($"[CodeIntel] Cleaned up zombie OmniSharp process (PID: {pid})");
                        }
                    }
                    catch
                    {
                        // Process already gone or access denied
                    }
                }
                EditorPrefs.DeleteKey(PID_KEY);
            }
        }
    }
}
