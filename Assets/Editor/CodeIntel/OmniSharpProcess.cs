// Triggering restart after full package deployment
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using UnityCodeIntel.Editor.Models;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace UnityCodeIntel.Editor
{
    public class OmniSharpProcess
    {
        private Process _process;
        private BridgeConfig _config;
        private string _projectRoot;
        private HttpClient _httpClient;

        public int Port { get; private set; }
        public int Pid => _process?.Id ?? 0;
        public bool IsRunning => _process != null && !_process.HasExited;
        public string BaseUrl => $"http://127.0.0.1:{Port}";
        public long LastOkTimestamp { get; private set; }
        public string LogFilePath { get; private set; }

        public OmniSharpProcess()
        {
            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromSeconds(2);
        }

        public void Start(string projectRoot, BridgeConfig config)
        {
            if (IsRunning)
            {
                Debug.LogWarning("[CodeIntel] OmniSharp is already running.");
                return;
            }

            _projectRoot = projectRoot;
            _config = config;

            // Resolve paths
            string exePath = Path.GetFullPath(Path.Combine(projectRoot, config.omnisharpExePath));
            string slnPath = string.IsNullOrEmpty(config.solutionPath)
                ? FindSolutionFile(projectRoot)
                : Path.GetFullPath(Path.Combine(projectRoot, config.solutionPath));

            if (!File.Exists(exePath))
            {
                Debug.LogError($"[CodeIntel] OmniSharp executable not found at: {exePath}");
                return;
            }

            // Check for OmniSharp.dll (common failure point if packaging is incomplete)
            string dllPath = Path.Combine(Path.GetDirectoryName(exePath), "OmniSharp.dll");
            if (!File.Exists(dllPath))
            {
                Debug.LogError($"[CodeIntel] OmniSharp.dll missing at: {dllPath}. Please ensure you have downloaded the full OmniSharp package, not just the bootstrapper.");
                return;
            }

            if (string.IsNullOrEmpty(slnPath) || !File.Exists(slnPath))
            {
                Debug.LogError($"[CodeIntel] Solution file not found. Project root: {projectRoot}");
                return;
            }

            // Determine port
            Port = config.omnisharpPort > 0 ? config.omnisharpPort : GetAvailablePort();

            // Prepare arguments
            // -s {sln} -p {port} --hostPID {pid} --encoding utf-8
            string args = $"-s \"{slnPath}\" -p {Port} --hostPID {Process.GetCurrentProcess().Id} --encoding utf-8";

            if (!string.IsNullOrEmpty(config.omnisharpJsonPath))
            {
                string configPath = Path.GetFullPath(Path.Combine(projectRoot, config.omnisharpJsonPath));
                if (File.Exists(configPath))
                {
                    // Some OmniSharp versions support --config
                    // But usually it picks up omnisharp.json from working dir.
                    // We will set WorkingDirectory to where omnisharp.json is, or project root.
                }
            }

            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = exePath,
                Arguments = args,
                WorkingDirectory = projectRoot,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            // Setup logging
            string logDir = Path.Combine(projectRoot, config.logDir);
            Directory.CreateDirectory(logDir);
            string logFile = Path.Combine(logDir, $"omnisharp-{DateTime.Now:yyyyMMdd-HHmmss}.log");
            LogFilePath = logFile;

            _process = new Process { StartInfo = startInfo };

            _process.OutputDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data)) File.AppendAllText(logFile, $"[STDOUT] {e.Data}\n");
            };
            _process.ErrorDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data)) File.AppendAllText(logFile, $"[STDERR] {e.Data}\n");
            };

            try
            {
                _process.Start();
                _process.BeginOutputReadLine();
                _process.BeginErrorReadLine();
                Debug.Log($"[CodeIntel] OmniSharp started on port {Port}. PID: {_process.Id}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[CodeIntel] Failed to start OmniSharp: {e.Message}");
                _process = null;
            }
        }

        public void Stop()
        {
            if (_process == null) return;

            try
            {
                if (!_process.HasExited)
                {
                    _process.Kill(); // For now, just kill. Graceful shutdown can be added later.
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

        public async Task<bool> CheckHealthAsync()
        {
            if (!IsRunning) return false;

            try
            {
                var response = await _httpClient.GetAsync($"{BaseUrl}/checkaliveness"); // OmniSharp endpoint
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
            return slns.Length > 0 ? slns[0] : null;
        }

        private int GetAvailablePort()
        {
            // Simple random port for now, or finding a free one.
            // Using 0 in HttpListener lets OS pick, but for OmniSharp we need to pass it.
            // Let's pick a random one in range.
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
                string json = JsonUtility.ToJson(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync($"{BaseUrl}{endpoint}", content);

                if (response.IsSuccessStatusCode)
                {
                    string respJson = await response.Content.ReadAsStringAsync();
                    // OmniSharp can return null or empty for some things
                    if (string.IsNullOrEmpty(respJson)) return default;
                    return JsonUtility.FromJson<T>(respJson);
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[CodeIntel] OmniSharp request failed: {e.Message}");
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
    }
}
