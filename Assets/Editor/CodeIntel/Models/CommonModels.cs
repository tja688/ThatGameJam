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
        public bool isOmniSharpReady;
        public long lastCompileTimestamp;
        public int domainReloadCount;
        public string lastRestartReason;
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
        public string symbolId; // P0: Support symbolId lookup (alternative to file/line/col)
    }

    [Serializable]
    public class SymbolsRequest
    {
        public string query;
        public string[] kind; // Optional filter
        public int limit;
    }

    /// <summary>
    /// P0 改进: 增强的 CodeLocation，包含 AI 友好的锚点信息
    /// </summary>
    [Serializable]
    public class CodeLocation
    {
        public string fileAbs;
        public string fileRel;
        public Range range;       // 整个声明范围（原有）
        public Range nameSpan;    // P0: 标识符 token 的精确范围（用于点中符号）
        public string text;       // Preview
    }

    /// <summary>
    /// P0 改进: 专门的符号搜索结果，包含更多语义信息
    /// </summary>
    [Serializable]
    public class SymbolInfo
    {
        public string symbolId;         // P0: 稳定 ID，用于后续 references 调用
        public string qualifiedName;    // P0: 全限定名，如 Namespace.Class.Method(params)
        public string kind;             // Method/Class/Property/Field 等
        public string containerName;    // 所在容器（类/命名空间）
        public string fileAbs;
        public string fileRel;
        public Range nameSpan;          // P0: 标识符 token 的精确范围
        public Range declSpan;          // 整个声明范围（可选）
        public string text;             // Preview line
    }

    /// <summary>
    /// P0 改进: 专门的引用搜索结果，包含符号确认信息
    /// </summary>
    [Serializable]
    public class ReferencesResult
    {
        public SymbolInfo symbol;       // P0: 让 AI 复核"我查到的到底是谁"
        public CodeLocation[] references;
    }

    /// <summary>
    /// P1 改进: 位置容错时返回的候选建议
    /// </summary>
    [Serializable]
    public class PositionCandidate
    {
        public string name;
        public string kind;
        public int line;
        public int col;
        public string symbolId;
    }

    /// <summary>
    /// P1 改进: 当位置无法 resolve 时返回候选列表
    /// </summary>
    [Serializable]
    public class FallbackResult
    {
        public string reason;
        public PositionCandidate[] candidates;
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
        public int MinFilterLength; // For narrower symbol searches
        public int MaxItemsToReturn; // Limit results
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
        // findsymbols 返回的额外字段
        public string Kind;
        public string ContainerQualifiedName;
        public string DisplayText; // 可能包含签名
    }

    [Serializable]
    public class OmniSharpResponse
    {
        // /goto/definition returns direct object or specific structure
        public string FileName;
        public int Line;
        public int Column;
        public int EndLine;
        public int EndColumn;

        // /findusages and /findsymbols return QuickFixes
        public OmniSharpLocation[] QuickFixes;
    }

    // P1: 用于在一行中扫描符号的请求
    [Serializable]
    public class OmniSharpHighlightRequest
    {
        public string FileName;
        public string[] Lines;
    }
}
