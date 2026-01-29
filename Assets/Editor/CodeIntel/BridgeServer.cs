using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityCodeIntel.Editor.Models;

namespace UnityCodeIntel.Editor
{
    public class BridgeServer
    {
        private HttpListener _listener;
        private OmniSharpProcess _omnisharp;
        private Thread _serverThread;
        private volatile bool _isRunning;
        private BridgeConfig _config;

        public int Port { get; private set; }
        public bool IsRunning => _isRunning;
        public DateTime StartTime { get; private set; }

        public BridgeServer(OmniSharpProcess omnisharp)
        {
            _omnisharp = omnisharp;
        }

        public void Start(BridgeConfig config)
        {
            if (_isRunning) return;

            _config = config;
            Port = config.bridgePort > 0 ? config.bridgePort : 8080;
            if (config.bridgePort == 0) Port = UnityEngine.Random.Range(30000, 40000);

            _listener = new HttpListener();
            string prefix = $"http://{config.bindAddress}:{Port}/";
            _listener.Prefixes.Add(prefix);
            
            try
            {
                _listener.Start();
                _isRunning = true;
                StartTime = DateTime.Now;
                _serverThread = new Thread(HandleRequests);
                _serverThread.Start();
                Debug.Log($"[CodeIntel] Bridge Server started at {prefix}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[CodeIntel] Failed to start Bridge Server: {e.Message}");
                _listener = null;
            }
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
            if (_serverThread != null && _serverThread.IsAlive)
            {
                _serverThread = null;
            }
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
                    Debug.LogError($"[CodeIntel] Server Error: {e.Message}");
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
                    var health = new HealthApiResponse
                    {
                        ok = true,
                        traceId = Guid.NewGuid().ToString(),
                        data = new HealthData
                        {
                            bridge = new BridgeHealth { version = "0.1.0", uptimeSeconds = (DateTime.Now - StartTime).TotalSeconds, port = Port },
                            omnisharp = new OmniSharpHealth { reachable = _omnisharp.IsRunning, pid = _omnisharp.Pid, baseUrl = _omnisharp.BaseUrl, lastOkTimestamp = _omnisharp.LastOkTimestamp },
                            unity = new UnityHealth { isCompiling = UnityEditor.EditorApplication.isCompiling }
                        }
                    };
                    responseJson = JsonUtility.ToJson(health);
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
                            responseJson = JsonUtility.ToJson(new ApiResponse<object>
                            {
                                ok = false,
                                error = new ApiError { code = "UNAUTHORIZED", message = "Missing or invalid token." }
                            });
                            WriteResponse(response, statusCode, responseJson);
                            return;
                        }
                    }

                    // Fault Tolerance Check
                    if (_omnisharp.Status != ServiceStatus.Running)
                    {
                        statusCode = 503;
                        responseJson = JsonUtility.ToJson(new ApiResponse<object> 
                        { 
                            ok = false, 
                            error = new ApiError 
                            { 
                                code = "OMNISHARP_DOWN", 
                                message = $"OmniSharp backend is not ready. Current Status: {_omnisharp.Status}. Please wait or restart services." 
                            } 
                        });
                    }
                    else if (path == "/v1/definition" && request.HttpMethod == "POST")
                    {
                        var req = ReadJsonBody<LocationRequest>(request);
                        var locations = Task.Run(() => _omnisharp.GetDefinition(req.file, req.line, req.col)).Result;
                        responseJson = JsonUtility.ToJson(new CodeLocationApiResponse { ok = true, data = locations });
                    }
                    else if (path == "/v1/references" && request.HttpMethod == "POST")
                    {
                        var req = ReadJsonBody<ReferencesRequest>(request);
                        var locations = Task.Run(() => _omnisharp.GetReferences(req.file, req.line, req.col, req.includeDeclaration)).Result;
                        responseJson = JsonUtility.ToJson(new CodeLocationApiResponse { ok = true, data = locations });
                    }
                    else if (path == "/v1/symbols" && request.HttpMethod == "POST")
                    {
                        var req = ReadJsonBody<SymbolsRequest>(request);
                        var locations = Task.Run(() => _omnisharp.GetSymbols(req.query)).Result;
                        responseJson = JsonUtility.ToJson(new CodeLocationApiResponse { ok = true, data = locations });
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
                responseJson = JsonUtility.ToJson(new ApiResponse<object> { ok = false, error = new ApiError { code = "INTERNAL_ERROR", message = e.Message } });
            }
            
            try
            {
                WriteResponse(response, statusCode, responseJson);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[CodeIntel] Failed to write response: {e.Message}");
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
                return JsonUtility.FromJson<T>(json);
            }
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
    }
}
