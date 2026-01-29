using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

namespace UnityCodeIntel.Editor
{
    [InitializeOnLoad]
    public static class CodeIntelManager
    {
        public static OmniSharpProcess OmniSharp { get; private set; }
        public static BridgeServer Bridge { get; private set; }
        public static BridgeConfig Config { get; private set; }
        public static string BridgeBaseUrl => Bridge != null && Bridge.IsRunning && Config != null
            ? $"http://{Config.bindAddress}:{Bridge.Port}/"
            : "";
        public static string RuntimeStatePath => GetRuntimeStatePath();
        
        private static string _projectRoot;
        private const string PID_KEY = "CodeIntel_OmniSharp_PID";
        private const string RUNTIME_STATE_FILENAME = "codeintel-endpoints.json";
        
        private static double _lastHeartbeatTime;
        private static readonly List<double> _restartTimestamps = new List<double>();
        private static bool _startRequested;
        private static double _startRequestedAt;
        private static bool _restartRequested;
        private static double _restartRequestedAt;
        private static double _nextRestartAllowedAt;
        private static bool _isShuttingDown;

        static CodeIntelManager()
        {
            _projectRoot = Directory.GetCurrentDirectory();
            Config = BridgeConfig.Load(Path.Combine(_projectRoot, "Tools/CodeIntel/bridge-config.json"));
            
            // Cleanup zombie process if any from previous domain/session
            CleanupZombieProcess();

            OmniSharp = new OmniSharpProcess();
            OmniSharp.UnexpectedExited += OnOmniSharpUnexpectedExited;
            Bridge = new BridgeServer(OmniSharp);

            EditorApplication.quitting += Shutdown;
            AssemblyReloadEvents.beforeAssemblyReload += BeforeAssemblyReload;
            EditorApplication.update += OnUpdate;
            CompilationPipeline.compilationFinished += OnCompilationFinished;
            
            if (Config.autoStartOnEditorLaunch)
            {
                EditorApplication.delayCall += () => RequestStartServices();
            }
        }

        public static void ReloadConfig()
        {
            Config = BridgeConfig.Load(Path.Combine(_projectRoot, "Tools/CodeIntel/bridge-config.json"));
        }

        public static void StartServices()
        {
            RequestStartServices();
        }

        public static void StopServices()
        {
            _startRequested = false;
            _restartRequested = false;
            Bridge?.Stop();
            OmniSharp?.Stop();
            EditorPrefs.DeleteKey(PID_KEY);
        }

        public static void BatchSmoke_StartServicesAndQuit()
        {
            StartServices();
            double startedAt = EditorApplication.timeSinceStartup;
            EditorApplication.CallbackFunction tick = null;
            tick = () =>
            {
                if (EditorApplication.timeSinceStartup - startedAt < 10.0) return;
                EditorApplication.update -= tick;
                StopServices();
                EditorApplication.Exit(0);
            };
            EditorApplication.update += tick;
        }
        
        private static string GetRuntimeStatePath()
        {
            string projectRoot = _projectRoot;
            string logDirRel = string.IsNullOrEmpty(Config?.logDir) ? "Library/CodeIntelLogs" : Config.logDir;
            return Path.Combine(projectRoot, logDirRel, RUNTIME_STATE_FILENAME);
        }

        private static void Shutdown()
        {
            _isShuttingDown = true;
            EditorApplication.update -= OnUpdate;
            CompilationPipeline.compilationFinished -= OnCompilationFinished;
            AssemblyReloadEvents.beforeAssemblyReload -= BeforeAssemblyReload;
            EditorApplication.quitting -= Shutdown;
            StopServices();
        }

        private static void BeforeAssemblyReload()
        {
            _isShuttingDown = true;
            StopServices();
        }

        private static void OnUpdate()
        {
            double now = EditorApplication.timeSinceStartup;

            if (_restartRequested && now >= _nextRestartAllowedAt)
            {
                double debounceSeconds = Math.Max(0.0, (Config?.restartDebounceMs ?? 0) / 1000.0);
                if (now - _restartRequestedAt >= debounceSeconds)
                {
                    PerformRestart();
                }
            }

            if (_startRequested)
            {
                TryStartServicesNow();
            }

            if (now - _lastHeartbeatTime > 30.0)
            {
                _lastHeartbeatTime = now;
                CheckHeartbeat();
            }
        }

        private static async void CheckHeartbeat()
        {
            if (_isShuttingDown) return;
            if (OmniSharp.Status == ServiceStatus.Running)
            {
                bool alive = await OmniSharp.CheckHealthAsync();
                if (_isShuttingDown) return;
                if (!alive)
                {
                    Debug.LogWarning("[CodeIntel] OmniSharp heartbeat failed. Service marked as unhealthy.");
                    OmniSharp.MarkAsUnhealthy();
                    RequestRestart();
                }
            }
        }

        private static void OnCompilationFinished(object context)
        {
            if (_isShuttingDown) return;
            if (Config == null) ReloadConfig();
            if (Config == null || !Config.autoRestartOnCompile) return;
            if (EditorUtility.scriptCompilationFailed) return;

            RequestRestart();
        }

        private static void OnOmniSharpUnexpectedExited(int exitCode, string logTail)
        {
            if (_isShuttingDown) return;
            EditorApplication.delayCall += RequestRestart;
        }

        private static void RequestStartServices()
        {
            if (_isShuttingDown) return;
            if (Config == null) ReloadConfig();

            _startRequested = true;
            _startRequestedAt = EditorApplication.timeSinceStartup;
            TryStartServicesNow();
        }

        private static void TryStartServicesNow()
        {
            if (_isShuttingDown) return;
            if (Config == null) ReloadConfig();
            if (Config == null) return;

            if (EditorApplication.isCompiling || EditorApplication.isUpdating || UnityCompilationWatcher.IsCompiling)
            {
                return;
            }

            if (!AreProjectFilesStable(_projectRoot))
            {
                return;
            }

            _startRequested = false;

            if (OmniSharp.Status != ServiceStatus.Running && OmniSharp.Status != ServiceStatus.Starting)
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

        private static bool AreProjectFilesStable(string projectRoot)
        {
            try
            {
                var slnFiles = Directory.GetFiles(projectRoot, "*.sln", SearchOption.TopDirectoryOnly);
                if (slnFiles.Length == 0) return false;

                var candidates = new List<string>(slnFiles);
                candidates.AddRange(Directory.GetFiles(projectRoot, "*.csproj", SearchOption.TopDirectoryOnly));

                DateTime newestWriteUtc = DateTime.MinValue;
                foreach (var path in candidates)
                {
                    if (!File.Exists(path)) continue;
                    var fi = new FileInfo(path);
                    if (fi.Length == 0) return false;
                    if (fi.LastWriteTimeUtc > newestWriteUtc) newestWriteUtc = fi.LastWriteTimeUtc;
                }

                if (newestWriteUtc == DateTime.MinValue) return false;
                return (DateTime.UtcNow - newestWriteUtc).TotalSeconds >= 2.0;
            }
            catch
            {
                return false;
            }
        }

        private static void RequestRestart()
        {
            if (_isShuttingDown) return;
            if (Config == null) ReloadConfig();
            if (Config == null) return;

            _restartRequested = true;
            _restartRequestedAt = EditorApplication.timeSinceStartup;
        }

        private static void PerformRestart()
        {
            if (_isShuttingDown) return;
            if (Config == null) ReloadConfig();
            if (Config == null) return;

            double now = EditorApplication.timeSinceStartup;
            _restartTimestamps.RemoveAll(t => now - t > 600.0);

            if (_restartTimestamps.Count >= Config.maxRestartsPer10Min)
            {
                _restartRequested = false;
                Debug.LogError("[CodeIntel] Max restart limit reached. Manual intervention required.");
                return;
            }

            _restartRequested = false;
            _nextRestartAllowedAt = now + Math.Max(0.0, Config.restartCooldownMs / 1000.0);
            _restartTimestamps.Add(now);

            Debug.Log("[CodeIntel] Attempting auto-restart...");
            StopServices();
            RequestStartServices();
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
