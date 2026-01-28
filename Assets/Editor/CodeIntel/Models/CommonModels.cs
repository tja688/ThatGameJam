using System;

namespace UnityCodeIntel.Editor.Models
{
    [Serializable]
    public class ApiResponse<T>
    {
        public bool ok;
        public string traceId;
        public long elapsedMs;
        public T data;
        public ApiError error;
    }

    [Serializable]
    public class ApiError
    {
        public string code;
        public string message;
    }

    [Serializable]
    public class HealthData
    {
        public BridgeHealth bridge;
        public OmniSharpHealth omnisharp;
        public UnityHealth unity;
    }

    [Serializable]
    public class BridgeHealth
    {
        public string version;
        public double uptimeSeconds;
        public int port;
    }

    [Serializable]
    public class OmniSharpHealth
    {
        public bool reachable;
        public int pid;
        public string baseUrl;
        public long lastOkTimestamp;
    }

    [Serializable]
    public class UnityHealth
    {
        public bool isCompiling;
        public long lastCompileTimestamp;
        public int domainReloadCount;
    }

    // Phase 2 Models
    [Serializable]
    public class LocationRequest
    {
        public string file;
        public int line;
        public int col;
    }

    [Serializable]
    public class ReferencesRequest : LocationRequest
    {
        public bool includeDeclaration;
    }

    [Serializable]
    public class SymbolsRequest
    {
        public string query;
        public string[] kind; // Optional filter
        public int limit;
    }

    [Serializable]
    public class CodeLocation
    {
        public string fileAbs;
        public string fileRel;
        public Range range;
        public string text; // Preview
    }

    [Serializable]
    public class Range
    {
        public int startLine;
        public int startColumn;
        public int endLine;
        public int endColumn;
    }

    // OmniSharp DTOs (Internal use for forwarding)
    [Serializable]
    public class OmniSharpRequest
    {
        public string FileName;
        public int Line;
        public int Column;
        public bool ExcludeDeclarations;
        public string Filter; // For symbols
    }

    [Serializable]
    public class OmniSharpLocation
    {
        public string FileName;
        public int Line;
        public int Column;
        public int EndLine;
        public int EndColumn;
        public string Text;
    }

    [Serializable]
    public class OmniSharpResponse
    {
        // /goto/definition returns direct object or specific structure
        public string FileName;
        public int Line;
        public int Column;
        
        // /findusages and /findsymbols return QuickFixes
        public OmniSharpLocation[] QuickFixes;
    }
}
