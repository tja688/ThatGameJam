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
using UnityCodeIntel.Editor.Models;
using UnityEditor;
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
            try { _lifetimeCts?.Cancel(); } catch { }
            try { _lifetimeCts?.Dispose(); } catch { }
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
            try { if (_process != null) exitCode = _process.ExitCode; } catch { }
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
                try { UnexpectedExited?.Invoke(exitCode, logTail ?? ""); } catch { }

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
            try { _lifetimeCts?.Cancel(); } catch { }
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

        /// <summary>
        /// P0 改进: 返回增强的 SymbolInfo 数组，包含 symbolId 和精确的 nameSpan
        /// </summary>
        public async Task<SymbolInfo[]> GetSymbols(string query, int limit = 50)
        {
            var req = new OmniSharpRequest
            {
                Filter = query,
                MaxItemsToReturn = limit > 0 ? limit : 50
            };
            var response = await PostJsonAsync<OmniSharpResponse>("/findsymbols", req);

            if (response != null && response.QuickFixes != null)
            {
                var list = new List<SymbolInfo>();
                foreach (var qf in response.QuickFixes)
                {
                    list.Add(MapToSymbolInfo(qf));
                }
                return list.ToArray();
            }
            return new SymbolInfo[0];
        }

        /// <summary>
        /// P0 改进: 根据 symbolId 获取引用（无需 file/line/col）
        /// </summary>
        public async Task<ReferencesResult> GetReferencesBySymbolId(string symbolId, bool includeDeclaration)
        {
            // symbolId 格式: "file|line|col|name"
            var parts = symbolId.Split('|');
            if (parts.Length < 3)
            {
                return new ReferencesResult
                {
                    symbol = null,
                    references = new CodeLocation[0]
                };
            }

            string file = parts[0];
            if (!int.TryParse(parts[1], out int line)) line = 1;
            if (!int.TryParse(parts[2], out int col)) col = 1;

            return await GetReferencesEnhanced(file, line, col, includeDeclaration);
        }

        /// <summary>
        /// P0/P1 改进: 增强的引用查询，返回完整的符号信息
        /// </summary>
        public async Task<ReferencesResult> GetReferencesEnhanced(string file, int line, int col, bool includeDeclaration)
        {
            var req = new OmniSharpRequest
            {
                FileName = ToAbsolutePath(file),
                Line = line,
                Column = col,
                ExcludeDeclarations = !includeDeclaration
            };

            var response = await PostJsonAsync<OmniSharpResponse>("/findusages", req);

            var result = new ReferencesResult
            {
                symbol = null,
                references = new CodeLocation[0]
            };

            if (response != null && response.QuickFixes != null && response.QuickFixes.Length > 0)
            {
                var refs = new List<CodeLocation>();
                SymbolInfo symbolInfo = null;

                foreach (var qf in response.QuickFixes)
                {
                    var loc = MapToCodeLocation(qf);
                    refs.Add(loc);

                    // 第一个结果通常是声明，用它构建符号信息
                    if (symbolInfo == null && includeDeclaration)
                    {
                        symbolInfo = MapToSymbolInfo(qf);
                    }
                }

                result.symbol = symbolInfo;
                result.references = refs.ToArray();
            }

            return result;
        }

        /// <summary>
        /// P1 改进: 带位置容错的引用查询
        /// </summary>
        public async Task<(ReferencesResult result, FallbackResult fallback)> GetReferencesWithFallback(
            string file, int line, int col, bool includeDeclaration)
        {
            // 首先尝试精确位置
            var result = await GetReferencesEnhanced(file, line, col, includeDeclaration);
            if (result.references.Length > 0)
            {
                return (result, null);
            }

            // P1: 位置容错 - 在同一行扫描可能的符号位置
            var candidates = await ScanLineForSymbols(file, line, col);
            if (candidates != null && candidates.Length > 0)
            {
                // 尝试最近的候选
                var nearest = candidates[0];
                var retryResult = await GetReferencesEnhanced(file, nearest.line, nearest.col, includeDeclaration);
                if (retryResult.references.Length > 0)
                {
                    return (retryResult, new FallbackResult
                    {
                        reason = $"Original position ({line},{col}) had no symbol. Auto-adjusted to ({nearest.line},{nearest.col}) on '{nearest.name}'.",
                        candidates = candidates
                    });
                }

                // 返回候选列表让 AI 选择
                return (null, new FallbackResult
                {
                    reason = "Position did not resolve to a symbol. Here are nearby candidates.",
                    candidates = candidates
                });
            }

            return (result, null);
        }

        /// <summary>
        /// P1: 扫描某行附近的符号位置（简化实现：基于列偏移尝试）
        /// </summary>
        private async Task<PositionCandidate[]> ScanLineForSymbols(string file, int line, int originalCol)
        {
            var candidates = new List<PositionCandidate>();
            string absPath = ToAbsolutePath(file);

            // 读取目标行
            string lineText = "";
            try
            {
                if (System.IO.File.Exists(absPath))
                {
                    var lines = await System.IO.File.ReadAllLinesAsync(absPath);
                    if (line > 0 && line <= lines.Length)
                    {
                        lineText = lines[line - 1];
                    }
                }
            }
            catch
            {
                return candidates.ToArray();
            }

            if (string.IsNullOrEmpty(lineText)) return candidates.ToArray();

            // 简单的标识符扫描（正则匹配 C# 标识符）
            var regex = new System.Text.RegularExpressions.Regex(@"\b([A-Za-z_][A-Za-z0-9_]*)\b");
            var matches = regex.Matches(lineText);

            foreach (System.Text.RegularExpressions.Match m in matches)
            {
                int startCol = m.Index + 1; // 1-based
                candidates.Add(new PositionCandidate
                {
                    name = m.Value,
                    kind = "identifier",
                    line = line,
                    col = startCol,
                    symbolId = $"{file}|{line}|{startCol}|{m.Value}"
                });
            }

            // 按与原始列的距离排序
            candidates = candidates
                .OrderBy(c => Math.Abs(c.col - originalCol))
                .Take(10)
                .ToList();

            return candidates.ToArray();
        }

        /// <summary>
        /// P0: 将 OmniSharp 返回转为增强的 SymbolInfo
        /// </summary>
        private SymbolInfo MapToSymbolInfo(OmniSharpLocation loc)
        {
            string name = ExtractSymbolName(loc.Text ?? loc.DisplayText ?? "");
            string qualifiedName = BuildQualifiedName(loc.ContainerQualifiedName, name, loc.Kind);

            return new SymbolInfo
            {
                symbolId = $"{loc.FileName}|{loc.Line}|{loc.Column}|{name}",
                qualifiedName = qualifiedName,
                kind = loc.Kind ?? "Unknown",
                containerName = loc.ContainerQualifiedName ?? "",
                fileAbs = loc.FileName,
                fileRel = ToRelativePath(loc.FileName),
                nameSpan = new UnityCodeIntel.Editor.Models.Range
                {
                    startLine = loc.Line,
                    startColumn = loc.Column,
                    endLine = loc.EndLine > 0 ? loc.EndLine : loc.Line,
                    endColumn = loc.EndColumn > 0 ? loc.EndColumn : loc.Column + name.Length
                },
                declSpan = new UnityCodeIntel.Editor.Models.Range
                {
                    startLine = loc.Line,
                    startColumn = loc.Column,
                    endLine = loc.EndLine > 0 ? loc.EndLine : loc.Line,
                    endColumn = loc.EndColumn > 0 ? loc.EndColumn : loc.Column
                },
                text = loc.Text ?? loc.DisplayText ?? ""
            };
        }

        /// <summary>
        /// 从代码行或显示文本中提取符号名
        /// </summary>
        private string ExtractSymbolName(string text)
        {
            if (string.IsNullOrEmpty(text)) return "Unknown";

            // 尝试匹配常见模式: "public void MethodName(..." => "MethodName"
            var methodMatch = System.Text.RegularExpressions.Regex.Match(text, @"\b(\w+)\s*\(");
            if (methodMatch.Success) return methodMatch.Groups[1].Value;

            // 匹配属性/字段: "public int PropertyName" => "PropertyName"
            var propMatch = System.Text.RegularExpressions.Regex.Match(text, @"\b(\w+)\s*[{;=]");
            if (propMatch.Success) return propMatch.Groups[1].Value;

            // 匹配类/接口: "public class ClassName" => "ClassName"
            var classMatch = System.Text.RegularExpressions.Regex.Match(text, @"(?:class|interface|struct|enum)\s+(\w+)");
            if (classMatch.Success) return classMatch.Groups[1].Value;

            // 回退：取第一个标识符
            var idMatch = System.Text.RegularExpressions.Regex.Match(text, @"\b([A-Za-z_][A-Za-z0-9_]*)\b");
            return idMatch.Success ? idMatch.Groups[1].Value : text.Trim();
        }

        /// <summary>
        /// 构建全限定名
        /// </summary>
        private string BuildQualifiedName(string container, string name, string kind)
        {
            if (string.IsNullOrEmpty(container))
                return name;
            return $"{container}.{name}";
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
            try { ctx.Post(_ => { if (!_isStopping) action(); }, null); } catch { }
        }
    }
}
