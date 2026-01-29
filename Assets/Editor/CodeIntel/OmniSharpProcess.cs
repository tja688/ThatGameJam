using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEditor;
using UnityCodeIntel.Editor.Models;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace UnityCodeIntel.Editor
{
    public enum ServiceStatus
    {
        Stopped,
        Starting,
        Running,
        Error
    }

    public class OmniSharpProcess
    {
        private Process _process;
        private BridgeConfig _config;
        private string _projectRoot;
        private HttpClient _httpClient;
        private readonly object _logLock = new object();
        private CancellationTokenSource _lifetimeCts;
        private SynchronizationContext _unityContext;
        private int _unityThreadId;
        private volatile bool _isStopping;

        public int Port { get; private set; }
        public int Pid => _process?.Id ?? 0;
        
        public ServiceStatus Status { get; private set; } = ServiceStatus.Stopped;
        public bool IsRunning => Status == ServiceStatus.Running;
        
        public string BaseUrl => $"http://127.0.0.1:{Port}";
        public long LastOkTimestamp { get; private set; }
        public string LogFilePath { get; private set; }
        public event Action<int, string> UnexpectedExited;

        private readonly string[] _requiredFiles = { "OmniSharp.dll", "OmniSharp.deps.json" };

        public OmniSharpProcess()
        {
            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromSeconds(2);
        }

        public void Start(string projectRoot, BridgeConfig config)
        {
            if (Status == ServiceStatus.Starting || Status == ServiceStatus.Running)
            {
                Debug.LogWarning("[CodeIntel] OmniSharp is already running or starting.");
                return;
            }

            _projectRoot = projectRoot;
            _config = config;
            _isStopping = false;
            _unityContext = SynchronizationContext.Current;
            _unityThreadId = Thread.CurrentThread.ManagedThreadId;
            try { _lifetimeCts?.Cancel(); } catch {}
            try { _lifetimeCts?.Dispose(); } catch {}
            _lifetimeCts = new CancellationTokenSource();

            // Resolve paths
            string exePath = Path.GetFullPath(Path.Combine(projectRoot, config.omnisharpExePath));
            string jsonPath = string.IsNullOrEmpty(config.omnisharpJsonPath)
                ? null
                : Path.GetFullPath(Path.Combine(projectRoot, config.omnisharpJsonPath));
            
            // 1. Pre-flight Checks
            if (!CheckRequiredFiles(exePath))
            {
                Status = ServiceStatus.Error;
                return;
            }

            string slnPath = string.IsNullOrEmpty(config.solutionPath)
                ? FindSolutionFile(projectRoot)
                : Path.GetFullPath(Path.Combine(projectRoot, config.solutionPath));

            if (string.IsNullOrEmpty(slnPath) || !File.Exists(slnPath))
            {
                Debug.LogError($"[CodeIntel] Solution file not found. Project root: {projectRoot}");
                Status = ServiceStatus.Error;
                return;
            }

            // Determine port
            Port = config.omnisharpPort > 0 ? config.omnisharpPort : GetAvailablePort();

            // 2. Port Occupancy Check
            if (IsPortOccupied(Port))
            {
                Debug.LogError($"[CodeIntel] Port {Port} is already in use. Cannot start OmniSharp.");
                Status = ServiceStatus.Error;
                return;
            }

            // Prepare arguments
            string args = $"-s \"{slnPath}\" -p {Port} -i 127.0.0.1 --hostPID {Process.GetCurrentProcess().Id} --encoding utf-8";

            string workingDir = projectRoot;
            if (!string.IsNullOrEmpty(jsonPath) && File.Exists(jsonPath))
            {
                workingDir = Path.GetDirectoryName(jsonPath);
            }

            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = exePath,
                Arguments = args,
                WorkingDirectory = workingDir,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
            startInfo.StandardOutputEncoding = Encoding.UTF8;
            startInfo.StandardErrorEncoding = Encoding.UTF8;

            // Setup logging
            string logDir = Path.Combine(projectRoot, config.logDir);
            Directory.CreateDirectory(logDir);
            string logFile = Path.Combine(logDir, $"omnisharp-{DateTime.Now:yyyyMMdd-HHmmss}.log");
            LogFilePath = logFile;

            _process = new Process { StartInfo = startInfo };
            _process.EnableRaisingEvents = true;

            _process.OutputDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data)) AppendLogLine(logFile, $"[STDOUT] {e.Data}");
            };
            _process.ErrorDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data)) AppendLogLine(logFile, $"[STDERR] {e.Data}");
            };
            
            _process.Exited += (sender, e) => 
            {
                HandleProcessExit();
            };

            try
            {
                _process.Start();
                _process.BeginOutputReadLine();
                _process.BeginErrorReadLine();
                
                Status = ServiceStatus.Starting;
                AppendLogLine(logFile, $"[BRIDGE] Started by Unity PID={Process.GetCurrentProcess().Id}, OmniSharp PID={_process.Id}, Port={Port}, Source={slnPath}, WorkingDir={workingDir}, IsCompiling={EditorApplication.isCompiling}");
                Debug.Log($"[CodeIntel] OmniSharp process started (PID: {_process.Id}). Verifying service readiness...");
                
                // 3. Post-Start Verification
                _ = PostStartVerificationAsync();
            }
            catch (Exception e)
            {
                Debug.LogError($"[CodeIntel] Failed to start OmniSharp: {e.Message}");
                Status = ServiceStatus.Error;
                _process = null;
            }
        }

        private void AppendLogLine(string logFile, string line)
        {
            try
            {
                lock (_logLock)
                {
                    File.AppendAllText(logFile, line + "\n");
                }
            }
            catch
            {
            }
        }

        private bool CheckRequiredFiles(string exePath)
        {
            if (!File.Exists(exePath))
            {
                Debug.LogError($"[CodeIntel] OmniSharp executable not found at: {exePath}");
                return false;
            }

            string dir = Path.GetDirectoryName(exePath);
            foreach (var file in _requiredFiles)
            {
                string path = Path.Combine(dir, file);
                if (!File.Exists(path))
                {
                    Debug.LogError($"[CodeIntel] Missing required dependency: {file}. Please ensure the full OmniSharp package is installed.");
                    return false;
                }
            }
            return true;
        }

        private bool IsPortOccupied(int port)
        {
            var ipGlobalProperties = IPGlobalProperties.GetIPGlobalProperties();
            var tcpConnInfoArray = ipGlobalProperties.GetActiveTcpListeners();
            return tcpConnInfoArray.Any(e => e.Port == port);
        }

        private async Task PostStartVerificationAsync()
        {
            int maxRetries = 30; // 30 seconds
            int attempt = 0;
            var token = _lifetimeCts?.Token ?? CancellationToken.None;

            while (attempt < maxRetries)
            {
                if (_isStopping || token.IsCancellationRequested) return;

                // If process died during verification
                if (_process == null || _process.HasExited)
                {
                    PostToUnityThread(() => Debug.LogError("[CodeIntel] OmniSharp process exited during startup verification."));
                    Status = ServiceStatus.Error;
                    return;
                }

                // Check health
                if (await CheckHealthAsync(force: true))
                {
                    try { await Task.Delay(600, token); } catch { return; }
                    if (_process == null || _process.HasExited)
                    {
                        PostToUnityThread(() => Debug.LogError("[CodeIntel] OmniSharp process exited immediately after reporting healthy."));
                        Status = ServiceStatus.Error;
                        return;
                    }
                    if (await CheckHealthAsync(force: true))
                    {
                        Status = ServiceStatus.Running;
                        PostToUnityThread(() => Debug.Log("[CodeIntel] Service is READY."));
                        return;
                    }
                }

                try { await Task.Delay(1000, token); } catch { return; }
                attempt++;
            }

            PostToUnityThread(() => Debug.LogError($"[CodeIntel] OmniSharp failed to respond within {maxRetries} seconds. Killing process."));
            Stop();
            Status = ServiceStatus.Error;
        }

        private void HandleProcessExit()
        {
             if (Status == ServiceStatus.Stopped) return; // Intentional stop

             int exitCode = -1;
             try { if (_process != null) exitCode = _process.ExitCode; } catch {}
             string logTail = "";
             if (exitCode != 0 && !string.IsNullOrEmpty(LogFilePath) && File.Exists(LogFilePath))
             {
                 try
                 {
                     var lines = File.ReadLines(LogFilePath).Reverse().Take(50).Reverse().ToArray();
                     logTail = string.Join("\n", lines);
                 }
                 catch
                 {
                 }
             }

             PostToUnityThread(() =>
             {
                 if (Status == ServiceStatus.Stopped) return;
                 Status = ServiceStatus.Error;

                 Debug.LogError($"[CodeIntel] OmniSharp process exited unexpectedly with code: {exitCode}");
                 if (!string.IsNullOrEmpty(logTail))
                 {
                     Debug.LogError("[CodeIntel] Last 50 lines of log:\n" + logTail);
                 }
                 try { UnexpectedExited?.Invoke(exitCode, logTail ?? ""); } catch {}

                 if (exitCode == -2147450751 || exitCode == -2147450749)
                 {
                     Debug.LogError("[CodeIntel] Hint: Exit code suggests missing .NET Runtime. Please install .NET 6.0 SDK or Runtime.");
                 }
             });
        }

        public void Stop()
        {
            Status = ServiceStatus.Stopped;
            _isStopping = true;
            try { _lifetimeCts?.Cancel(); } catch {}
            if (_process == null) return;

            try
            {
                if (!_process.HasExited)
                {
                    _process.Kill();
                    _process.WaitForExit(2000);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[CodeIntel] Error stopping OmniSharp: {e.Message}");
            }
            finally
            {
                _process = null;
                Debug.Log("[CodeIntel] OmniSharp stopped.");
            }
        }

        public void MarkAsUnhealthy()
        {
            if (Status == ServiceStatus.Running)
            {
                Status = ServiceStatus.Error;
            }
        }

        public async Task<bool> CheckHealthAsync(bool force = false)
        {
            if (!force && Status != ServiceStatus.Running && Status != ServiceStatus.Starting) return false;

            try
            {
                var response = await _httpClient.GetAsync($"{BaseUrl}/checkaliveness");
                if (response.IsSuccessStatusCode)
                {
                    LastOkTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                    return true;
                }
            }
            catch
            {
                // Ignore connection errors
            }
            return false;
        }

        private string FindSolutionFile(string root)
        {
            string[] slns = Directory.GetFiles(root, "*.sln");
            if (slns.Length == 0) return null;

            string rootName = new DirectoryInfo(root).Name;
            var matching = slns.FirstOrDefault(s =>
                string.Equals(Path.GetFileNameWithoutExtension(s), rootName, StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrEmpty(matching)) return matching;

            return slns
                .OrderByDescending(File.GetLastWriteTimeUtc)
                .FirstOrDefault();
        }

        private int GetAvailablePort()
        {
            return UnityEngine.Random.Range(20000, 30000);
        }

        public async Task<CodeLocation[]> GetDefinition(string file, int line, int col)
        {
            var req = new OmniSharpRequest
            {
                FileName = ToAbsolutePath(file),
                Line = line,
                Column = col
            };

            var response = await PostJsonAsync<OmniSharpResponse>("/goto/definition", req);

            if (response != null && !string.IsNullOrEmpty(response.FileName))
            {
                return new[] { MapToCodeLocation(response) };
            }
            return new CodeLocation[0];
        }

        public async Task<CodeLocation[]> GetReferences(string file, int line, int col, bool includeDeclaration)
        {
            var req = new OmniSharpRequest
            {
                FileName = ToAbsolutePath(file),
                Line = line,
                Column = col,
                ExcludeDeclarations = !includeDeclaration
            };

            var response = await PostJsonAsync<OmniSharpResponse>("/findusages", req);

            if (response != null && response.QuickFixes != null)
            {
                var list = new List<CodeLocation>();
                foreach (var qf in response.QuickFixes) list.Add(MapToCodeLocation(qf));
                return list.ToArray();
            }
            return new CodeLocation[0];
        }

        public async Task<CodeLocation[]> GetSymbols(string query)
        {
            var req = new OmniSharpRequest { Filter = query };
            var response = await PostJsonAsync<OmniSharpResponse>("/findsymbols", req);

            if (response != null && response.QuickFixes != null)
            {
                var list = new List<CodeLocation>();
                foreach (var qf in response.QuickFixes) list.Add(MapToCodeLocation(qf));
                return list.ToArray();
            }
            return new CodeLocation[0];
        }

        private async Task<T> PostJsonAsync<T>(string endpoint, object payload)
        {
            if (!IsRunning) return default;

            try
            {
                string json = JsonConvert.SerializeObject(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync($"{BaseUrl}{endpoint}", content);

                if (response.IsSuccessStatusCode)
                {
                    string respJson = await response.Content.ReadAsStringAsync();
                    if (string.IsNullOrEmpty(respJson)) return default;
                    return JsonConvert.DeserializeObject<T>(respJson);
                }
            }
            catch (Exception e)
            {
                PostToUnityThread(() => Debug.LogWarning($"[CodeIntel] OmniSharp request failed: {e.Message}"));
            }
            return default;
        }

        private CodeLocation MapToCodeLocation(OmniSharpResponse resp)
        {
            return new CodeLocation
            {
                fileAbs = resp.FileName,
                fileRel = ToRelativePath(resp.FileName),
                range = new UnityCodeIntel.Editor.Models.Range { startLine = resp.Line, startColumn = resp.Column, endLine = resp.Line, endColumn = resp.Column }
            };
        }

        private CodeLocation MapToCodeLocation(OmniSharpLocation loc)
        {
            return new CodeLocation
            {
                fileAbs = loc.FileName,
                fileRel = ToRelativePath(loc.FileName),
                range = new UnityCodeIntel.Editor.Models.Range { startLine = loc.Line, startColumn = loc.Column, endLine = loc.EndLine, endColumn = loc.EndColumn },
                text = loc.Text
            };
        }

        private string ToAbsolutePath(string path)
        {
            if (Path.IsPathRooted(path)) return path;
            return Path.GetFullPath(Path.Combine(_projectRoot, path));
        }

        private string ToRelativePath(string path)
        {
            if (string.IsNullOrEmpty(path)) return "";
            if (path.StartsWith(_projectRoot))
            {
                return path.Substring(_projectRoot.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).Replace("\\", "/");
            }
            return path;
        }

        private void PostToUnityThread(Action action)
        {
            if (action == null) return;
            if (_isStopping) return;

            if (_unityThreadId != 0 && Thread.CurrentThread.ManagedThreadId == _unityThreadId)
            {
                action();
                return;
            }

            var ctx = _unityContext;
            if (ctx == null) return;
            try { ctx.Post(_ => { if (!_isStopping) action(); }, null); } catch {}
        }
    }
}
