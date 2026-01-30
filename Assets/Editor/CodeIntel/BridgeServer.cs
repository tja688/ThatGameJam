using System;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityCodeIntel.Editor.Models;
using UnityEditor;
using UnityEngine;

namespace UnityCodeIntel.Editor
{
    public class BridgeServer
    {
        private HttpListener _listener;
        private OmniSharpProcess _omnisharp;
        private Thread _serverThread;
        private volatile bool _isRunning;
        private BridgeConfig _config;
        private SynchronizationContext _unityContext;
        private int _unityThreadId;

        public int Port { get; private set; }
        public bool IsRunning => _isRunning;
        public DateTime StartTime { get; private set; }

        // P1 改进: 用于 health 响应的重启原因

        private static string _lastRestartReason = "";
        public static void SetRestartReason(string reason) => _lastRestartReason = reason;

        private const string LAST_PORT_KEY = "CodeIntel_Bridge_LastPort";
        private const string RUNTIME_STATE_FILENAME = "codeintel-endpoints.json";

        public BridgeServer(OmniSharpProcess omnisharp)
        {
            _omnisharp = omnisharp;
        }

        public void Start(BridgeConfig config)
        {
            if (_isRunning) return;

            _config = config;
            _unityContext = SynchronizationContext.Current;
            _unityThreadId = Thread.CurrentThread.ManagedThreadId;
            int configuredPort = config.bridgePort > 0 ? config.bridgePort : 0;

            int preferredPort = configuredPort;
            if (configuredPort == 0)
            {
                int lastPort = EditorPrefs.GetInt(LAST_PORT_KEY, 0);
                preferredPort = lastPort > 0 ? lastPort : UnityEngine.Random.Range(30000, 40000);
            }

            int maxAttempts = configuredPort == 0 ? 30 : 1;
            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                Port = attempt == 0 ? preferredPort : UnityEngine.Random.Range(30000, 40000);
                if (IsPortOccupied(Port)) continue;

                _listener = new HttpListener();
                string prefix = $"http://{config.bindAddress}:{Port}/";
                _listener.Prefixes.Add(prefix);

                try
                {
                    _listener.Start();
                    _isRunning = true;
                    StartTime = DateTime.Now;
                    _serverThread = new Thread(HandleRequests);
                    _serverThread.IsBackground = true;
                    _serverThread.Start();
                    EditorPrefs.SetInt(LAST_PORT_KEY, Port);
                    WriteRuntimeState(isRunning: true, bridgeBaseUrl: prefix);
                    Debug.Log($"[CodeIntel] Bridge Server started at {prefix}");
                    return;
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[CodeIntel] Failed to start Bridge Server on port {Port}: {e.Message}");
                    try
                    {
                        _listener?.Stop();
                        _listener?.Close();
                    }
                    catch
                    {
                    }
                    _listener = null;
                }
            }

            Debug.LogError("[CodeIntel] Failed to start Bridge Server after multiple attempts.");
        }

        public void Stop()
        {
            _isRunning = false;
            if (_listener != null)
            {
                _listener.Stop();
                _listener.Close();
                _listener = null;
            }
            if (_serverThread != null)
            {
                try
                {
                    if (_serverThread.IsAlive)
                    {
                        if (!_serverThread.Join(1500))
                        {
                            _serverThread.Interrupt();
                            _serverThread.Join(500);
                        }
                    }
                }
                catch
                {
                }
                _serverThread = null;
            }
            WriteRuntimeState(isRunning: false, bridgeBaseUrl: "");
            Debug.Log("[CodeIntel] Bridge Server stopped.");
        }

        private void HandleRequests()
        {
            while (_isRunning && _listener != null && _listener.IsListening)
            {
                try
                {
                    var context = _listener.GetContext();
                    ProcessRequest(context);
                }
                catch (HttpListenerException)
                {
                    break;
                }
                catch (Exception e)
                {
                    PostToUnityThread(() => Debug.LogError($"[CodeIntel] Server Error: {e.Message}"));
                }
            }
        }

        private void ProcessRequest(HttpListenerContext context)
        {
            var request = context.Request;
            var response = context.Response;
            var path = request.Url.AbsolutePath;

            string responseJson = "";
            int statusCode = 200;

            try

            {
                if (path == "/health" && request.HttpMethod == "GET")
                {
                    // P1 改进: 增强 health 响应，包含更多可机读状态
                    var health = new HealthApiResponse
                    {
                        ok = true,
                        traceId = Guid.NewGuid().ToString(),
                        data = new HealthData
                        {
                            bridge = new BridgeHealth { version = "0.2.0", uptimeSeconds = (DateTime.Now - StartTime).TotalSeconds, port = Port },
                            omnisharp = new OmniSharpHealth { reachable = _omnisharp.IsRunning, pid = _omnisharp.Pid, baseUrl = _omnisharp.BaseUrl, lastOkTimestamp = _omnisharp.LastOkTimestamp },
                            unity = new UnityHealth
                            {

                                isCompiling = UnityCompilationWatcher.IsCompiling,
                                isOmniSharpReady = _omnisharp.Status == ServiceStatus.Running,
                                lastRestartReason = _lastRestartReason
                            }
                        }
                    };
                    responseJson = JsonConvert.SerializeObject(health);
                }
                else
                {
                    if (!string.IsNullOrEmpty(_config.token))
                    {
                        string provided = request.Headers["X-CodeIntel-Token"];
                        if (string.IsNullOrEmpty(provided))
                        {
                            string auth = request.Headers["Authorization"];
                            if (!string.IsNullOrEmpty(auth) && auth.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                            {
                                provided = auth.Substring("Bearer ".Length).Trim();
                            }
                        }

                        if (!string.Equals(provided, _config.token, StringComparison.Ordinal))
                        {
                            statusCode = 401;
                            responseJson = JsonConvert.SerializeObject(new ApiResponse<object>
                            {
                                ok = false,
                                error = new ApiError { code = "UNAUTHORIZED", message = "Missing or invalid token." }
                            });
                            WriteResponse(response, statusCode, responseJson);
                            return;
                        }
                    }

                    // P1 改进: Fault Tolerance Check with machine-readable error codes
                    if (_omnisharp.Status != ServiceStatus.Running)
                    {
                        statusCode = 503;
                        string errorCode = UnityCompilationWatcher.IsCompiling ? "CODEINTEL_COMPILING" : "CODEINTEL_NOT_READY";
                        responseJson = JsonConvert.SerializeObject(new ApiResponse<object>
                        {

                            ok = false,

                            error = new ApiError
                            {

                                code = errorCode,

                                message = $"OmniSharp backend is not ready. Current Status: {_omnisharp.Status}. IsCompiling: {UnityCompilationWatcher.IsCompiling}. Please wait or restart services."
                            }

                        });
                    }
                    else if (path == "/v1/definition" && request.HttpMethod == "POST")
                    {
                        var req = ReadJsonBody<LocationRequest>(request);
                        var locations = Task.Run(() => _omnisharp.GetDefinition(req.file, req.line, req.col)).Result;
                        responseJson = JsonConvert.SerializeObject(new CodeLocationApiResponse { ok = true, data = locations });
                    }
                    else if (path == "/v1/references" && request.HttpMethod == "POST")
                    {
                        // P0 改进: 支持 symbolId 和增强响应
                        var req = ReadJsonBody<ReferencesRequest>(request);
                        ReferencesResult result;


                        if (!string.IsNullOrEmpty(req.symbolId))
                        {
                            // 使用 symbolId 查询（推荐方式）
                            result = Task.Run(() => _omnisharp.GetReferencesBySymbolId(req.symbolId, req.includeDeclaration)).Result;
                        }
                        else
                        {
                            // 传统 file/line/col 方式
                            result = Task.Run(() => _omnisharp.GetReferencesEnhanced(req.file, req.line, req.col, req.includeDeclaration)).Result;
                        }
                        responseJson = JsonConvert.SerializeObject(new ReferencesApiResponse { ok = true, data = result });
                    }
                    else if (path == "/v1/symbols" && request.HttpMethod == "POST")
                    {
                        // P0 改进: 返回增强的 SymbolInfo[]
                        var req = ReadJsonBody<SymbolsRequest>(request);
                        var symbols = Task.Run(() => _omnisharp.GetSymbols(req.query, req.limit > 0 ? req.limit : 50)).Result;
                        responseJson = JsonConvert.SerializeObject(new SymbolsApiResponse { ok = true, data = symbols });
                    }
                    else
                    {
                        statusCode = 404;
                        responseJson = "{\"ok\":false, \"error\":{\"code\":\"NOT_FOUND\"}}";
                    }
                }
            }
            catch (Exception e)
            {
                statusCode = 500;
                responseJson = JsonConvert.SerializeObject(new ApiResponse<object> { ok = false, error = new ApiError { code = "INTERNAL_ERROR", message = e.Message } });
            }


            try
            {
                WriteResponse(response, statusCode, responseJson);
            }
            catch (Exception e)
            {
                PostToUnityThread(() => Debug.LogWarning($"[CodeIntel] Failed to write response: {e.Message}"));
            }
        }

        private void WriteResponse(HttpListenerResponse response, int statusCode, string responseJson)
        {
            byte[] buffer = Encoding.UTF8.GetBytes(responseJson);
            response.ContentLength64 = buffer.Length;
            response.StatusCode = statusCode;
            response.ContentType = "application/json";
            response.OutputStream.Write(buffer, 0, buffer.Length);
            response.OutputStream.Close();
        }


        private T ReadJsonBody<T>(HttpListenerRequest request)
        {
            using (var reader = new StreamReader(request.InputStream, request.ContentEncoding))
            {
                string json = reader.ReadToEnd();
                return JsonConvert.DeserializeObject<T>(json);
            }
        }

        private static bool IsPortOccupied(int port)
        {
            try
            {
                var ipGlobalProperties = IPGlobalProperties.GetIPGlobalProperties();
                var tcpConnInfoArray = ipGlobalProperties.GetActiveTcpListeners();
                foreach (var ep in tcpConnInfoArray)
                {
                    if (ep.Port == port) return true;
                }
            }
            catch
            {
            }
            return false;
        }


        private void WriteRuntimeState(bool isRunning, string bridgeBaseUrl)
        {
            try
            {
                string projectRoot = Directory.GetCurrentDirectory();
                string logDirRel = string.IsNullOrEmpty(_config?.logDir) ? "Library/CodeIntelLogs" : _config.logDir;
                string logDirAbs = Path.Combine(projectRoot, logDirRel);
                Directory.CreateDirectory(logDirAbs);

                string statePath = Path.Combine(logDirAbs, RUNTIME_STATE_FILENAME);
                var state = new RuntimeState
                {
                    generatedAtUtc = DateTime.UtcNow.ToString("o"),
                    bridge = new RuntimeBridgeState
                    {
                        isRunning = isRunning,
                        bindAddress = _config?.bindAddress ?? "127.0.0.1",
                        port = isRunning ? Port : 0,
                        baseUrl = isRunning ? bridgeBaseUrl : "",
                        tokenRequired = !string.IsNullOrEmpty(_config?.token)
                    },
                    omnisharp = new RuntimeOmniSharpState
                    {
                        isRunning = _omnisharp != null && _omnisharp.IsRunning,
                        pid = _omnisharp?.Pid ?? 0,
                        port = _omnisharp?.Port ?? 0,
                        baseUrl = _omnisharp?.BaseUrl ?? ""
                    }
                };

                File.WriteAllText(statePath, JsonConvert.SerializeObject(state, Formatting.Indented));
            }
            catch
            {
            }
        }

        private void PostToUnityThread(Action action)
        {
            if (action == null) return;
            if (_unityThreadId != 0 && Thread.CurrentThread.ManagedThreadId == _unityThreadId)
            {
                action();
                return;
            }

            var ctx = _unityContext;
            if (ctx == null) return;
            try { ctx.Post(_ => action(), null); } catch { }
        }


        [Serializable]
        private class HealthApiResponse
        {
            public bool ok;
            public string traceId;
            public long elapsedMs;
            public HealthData data;
            public ApiError error;
        }

        [Serializable]
        private class CodeLocationApiResponse
        {
            public bool ok;
            public string traceId;
            public long elapsedMs;
            public CodeLocation[] data;
            public ApiError error;
        }

        // P0 改进: 专门的 Symbols 响应类型

        [Serializable]
        private class SymbolsApiResponse
        {
            public bool ok;
            public string traceId;
            public long elapsedMs;
            public SymbolInfo[] data;
            public ApiError error;
        }

        // P0 改进: 专门的 References 响应类型

        [Serializable]
        private class ReferencesApiResponse
        {
            public bool ok;
            public string traceId;
            public long elapsedMs;
            public ReferencesResult data;
            public ApiError error;
        }


        [Serializable]
        private class RuntimeState
        {
            public string generatedAtUtc;
            public RuntimeBridgeState bridge;
            public RuntimeOmniSharpState omnisharp;
        }

        [Serializable]
        private class RuntimeBridgeState
        {
            public bool isRunning;
            public string bindAddress;
            public int port;
            public string baseUrl;
            public bool tokenRequired;
        }

        [Serializable]
        private class RuntimeOmniSharpState
        {
            public bool isRunning;
            public int pid;
            public int port;
            public string baseUrl;
        }
    }
}
