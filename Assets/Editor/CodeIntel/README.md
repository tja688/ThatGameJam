# Unity Code Intel Service（Unity 代码智能桥接服务）

把 Unity Editor 的 C# 工程语义能力（OmniSharp）通过本地 HTTP 暴露给外部 Agent/工具，用于"转定义 / 找引用 / 符号搜索"等自动化代码检索场景。

## 版本 0.2.0 更新亮点

本版本实现了针对 AI Agent 友好的重大改进：

### P0 改进（核心 - 让 AI 不再需要读文件算列号）

1. **`/v1/symbols` 返回增强的 `SymbolInfo[]`**
   - 新增 `symbolId`: 稳定 ID，可直接用于 `/v1/references` 查询
   - 新增 `qualifiedName`: 全限定名，如 `Namespace.Class.Method`
   - 新增 `nameSpan`: 标识符 token 的精确位置（startLine/startColumn）
   - 新增 `kind`: 符号类型（Method/Class/Property/Field 等）

2. **`/v1/references` 支持 `symbolId` 查询**
   - 推荐方式：使用 symbols 返回的 `symbolId`，无需 file/line/col
   - 返回增强结构：包含 `symbol`（符号信息）和 `references[]`（引用列表）

### P1 改进（体验升级）

1. **增强 `/health` 响应**
   - 新增 `unity.isOmniSharpReady`: 明确的就绪状态
   - 新增 `unity.lastRestartReason`: 上次重启原因

2. **可机读错误码**
   - `CODEINTEL_NOT_READY`: OmniSharp 未就绪
   - `CODEINTEL_COMPILING`: Unity 正在编译中

---

## 组成与数据流

- Unity Editor 负责：启动/监控 OmniSharp 进程、提供 Dashboard、在编译/域重载期间做容错与重启
- OmniSharp 负责：解析 Solution/Projects，提供语义查询接口（HTTP）
- Bridge Server 负责：对外提供稳定、简单的 API，并把请求转发到 OmniSharp

数据流：

1. 外部工具请求 Bridge：`POST /v1/...`
2. Bridge 转发到 OmniSharp：`/goto/definition`、`/findusages`、`/findsymbols`
3. Bridge 返回统一结构的结果给外部工具

## 目录结构

- `Assets/Editor/CodeIntel/`：Unity 编辑器端（进程管理、Bridge 服务、Dashboard）
- `Tools/CodeIntel/`：内核与配置（OmniSharp 分发包、`bridge-config.json`、`omnisharp.json`）
- `Library/CodeIntelLogs/`：运行日志输出目录（会自动创建）

## 安装

1. 拷贝 `Assets/Editor/CodeIntel` 到项目的 `Assets/Editor/` 下（本项目已集成可跳过）
2. 拷贝 `Tools/CodeIntel` 到项目根目录的 `Tools/` 下（本项目已集成可跳过）
3. 确认 OmniSharp 分发包完整：
   - `Tools/CodeIntel/omnisharp/OmniSharp.exe`
   - 同目录下应同时存在 `OmniSharp.dll`、`OmniSharp.deps.json` 等依赖文件
4. 确保本机具备 OmniSharp 所需的 .NET 运行时/SDK（若启动即退出通常与此相关）

## 配置（bridge-config.json）

路径：`Tools/CodeIntel/bridge-config.json`

- `bridgePort`：Bridge 对外端口，`0` 表示随机
- `bindAddress`：监听地址，建议保持 `127.0.0.1`（仅本机访问）
- `token`：可选鉴权 Token（非空时，除 `/health` 外的请求必须携带）
- `omnisharpPort`：OmniSharp 内部端口，`0` 表示随机
- `omnisharpExePath`：OmniSharp 可执行文件路径（相对项目根目录）
- `omnisharpJsonPath`：`omnisharp.json` 路径（相对项目根目录）
- `solutionPath`：可选，指定 `.sln` 路径（相对项目根目录）；为空时自动选择
- `autoStartOnEditorLaunch`：Unity 打开时自动启动
- `autoRestartOnCompile`：编译完成后自动重启（用于同步工程变更）
- `restartDebounceMs`：重启去抖（避免短时间重复触发）
- `restartCooldownMs`：重启冷却（避免崩溃循环）
- `maxRestartsPer10Min`：10 分钟内最大重启次数上限
- `logDir`：日志目录（相对项目根目录）

`Tools/CodeIntel/omnisharp.json` 用于 OmniSharp 自身配置（例如排除 `Library/Temp/Obj` 等目录，显著降低扫描量与内存压力）。

## 启动与使用

1. 打开 Unity 项目
2. 进入菜单：`Window > CodeIntel > Dashboard`
3. 启动方式
   - 自动：若 `autoStartOnEditorLaunch=true`，Unity 启动后会自动拉起服务
   - 手动：点击 `Start Services`
4. 外部工具访问：`http://127.0.0.1:{BridgePort}/...`

### 端口发现（Port Discovery）

当 `bridgePort=0`（随机端口）时，为了避免外部工具/脚本需要 `netstat` 或反复猜端口，Bridge 会在启动后写入一个固定位置的运行时状态文件，供外部读取。

- 运行时状态文件：`{ProjectRoot}/{logDir}/codeintel-endpoints.json`（默认：`Library/CodeIntelLogs/codeintel-endpoints.json`）
- 内容包含：Bridge 的 `baseUrl/port`、是否需要 token，以及 OmniSharp 的 `baseUrl/port/pid`
- Dashboard：会显示 `Bridge Base URL`，并提供 Copy；同时显示该运行时状态文件路径并可 Reveal
- 当 `bridgePort=0` 时：会优先复用上次成功启动的端口；若端口被占用则自动换一个端口重试

---

## HTTP API

### `GET /health`

用于探测 Bridge 是否存活、OmniSharp 是否可用、Unity 是否正在编译。

**响应结构：**

```json
{
  "ok": true,
  "traceId": "uuid",
  "data": {
    "bridge": { "version": "0.2.0", "uptimeSeconds": 123.4, "port": 32204 },
    "omnisharp": { "reachable": true, "pid": 12345, "baseUrl": "http://127.0.0.1:20000", "lastOkTimestamp": 1706600000000 },
    "unity": { 
      "isCompiling": false, 
      "isOmniSharpReady": true,
      "lastRestartReason": ""
    }
  }
}
```

### `POST /v1/symbols` ⭐ AI 友好

按名字/关键字搜索符号，返回增强的 `SymbolInfo[]`。

**请求体：**

```json
{ "query": "PlayerController", "limit": 50 }
```

**响应结构：**

```json
{
  "ok": true,
  "data": [
    {
      "symbolId": "C:/Project/Assets/Scripts/Player.cs|10|12|PlayerController",
      "qualifiedName": "Game.PlayerController",
      "kind": "Class",
      "containerName": "Game",
      "fileAbs": "C:/Project/Assets/Scripts/Player.cs",
      "fileRel": "Assets/Scripts/Player.cs",
      "nameSpan": { "startLine": 10, "startColumn": 12, "endLine": 10, "endColumn": 28 },
      "declSpan": { "startLine": 10, "startColumn": 1, "endLine": 50, "endColumn": 1 },
      "text": "public class PlayerController : MonoBehaviour"
    }
  ]
}
```

**关键字段说明：**

- `symbolId`: 稳定 ID，可直接传给 `/v1/references` 的 `symbolId` 参数
- `nameSpan.startLine/startColumn`: 标识符 token 起点，可用于精确的 line/col 查询
- `qualifiedName`: 全限定名，方便 AI 理解符号层级

### `POST /v1/references` ⭐ AI 友好

查找符号的所有引用位置。**推荐使用 `symbolId` 方式**。

**请求体（推荐 - 使用 symbolId）：**

```json
{ 
  "symbolId": "C:/Project/Assets/Scripts/Player.cs|10|12|PlayerController",
  "includeDeclaration": true 
}
```

**请求体（传统 - 使用 file/line/col）：**

```json
{ 
  "file": "Assets/Scripts/Player.cs", 
  "line": 10, 
  "col": 12, 
  "includeDeclaration": true 
}
```

**响应结构：**

```json
{
  "ok": true,
  "data": {
    "symbol": {
      "symbolId": "...",
      "qualifiedName": "Game.PlayerController",
      "kind": "Class",
      ...
    },
    "references": [
      {
        "fileAbs": "C:/Project/Assets/Scripts/GameManager.cs",
        "fileRel": "Assets/Scripts/GameManager.cs",
        "range": { "startLine": 15, "startColumn": 8, "endLine": 15, "endColumn": 24 },
        "nameSpan": { "startLine": 15, "startColumn": 8, "endLine": 15, "endColumn": 24 },
        "text": "private PlayerController player;"
      }
    ]
  }
}
```

**关键改进：**

- `symbol`: 让 AI 复核"我查到的到底是谁"
- `references`: 每条包含精确的 range 和 text

### `POST /v1/definition`

给定"文件+位置"，转到定义。

**请求体：**

```json
{ "file": "Assets/Scripts/Foo.cs", "line": 10, "col": 15 }
```

**响应结构：**

```json
{
  "ok": true,
  "data": [
    {
      "fileAbs": "C:/Project/Assets/Scripts/Player.cs",
      "fileRel": "Assets/Scripts/Player.cs",
      "range": { "startLine": 10, "startColumn": 1, "endLine": 10, "endColumn": 50 },
      "text": "public class PlayerController : MonoBehaviour"
    }
  ]
}
```

---

## 错误码

| 错误码 | HTTP 状态 | 说明 |
|--------|-----------|------|
| `CODEINTEL_NOT_READY` | 503 | OmniSharp 未就绪，请等待或重启服务 |
| `CODEINTEL_COMPILING` | 503 | Unity 正在编译，请等待编译完成 |
| `UNAUTHORIZED` | 401 | 缺少或无效的 Token |
| `NOT_FOUND` | 404 | 未知的 API 路径 |
| `INTERNAL_ERROR` | 500 | 内部错误 |

---

## AI Agent 推荐工作流

1. **首先检查 `/health`**，确认 `unity.isOmniSharpReady == true`
2. **搜索符号**：`POST /v1/symbols { "query": "目标符号名" }`
3. **获取 symbolId**：从返回的 `data[].symbolId` 选择目标符号
4. **查找引用**：`POST /v1/references { "symbolId": "...", "includeDeclaration": true }`
5. **无需读文件算列号**：所有位置信息都由 API 提供

---

## Token 鉴权

当 `bridge-config.json` 的 `token` 非空时：

- `/health` 不需要 token
- 其余请求必须携带 token

支持两种方式（二选一）：

- Header：`X-CodeIntel-Token: <token>`
- Header：`Authorization: Bearer <token>`

## 日志与排障

### 日志位置

- OmniSharp 日志：`{ProjectRoot}/{logDir}/omnisharp-*.log`
- Dashboard 面板会滚动显示最近日志

### 常见问题

#### 1) Unity 显示 "Service is READY" 后又立刻退出（偶发）

这类情况通常发生在 Unity 启动/编译阶段，`.sln/.csproj` 正在生成或被刷新，OmniSharp 在加载工程时容易出现短时不稳定。

本版本做了两类增强来降低概率：

- 启动门控：编译/更新期间不会立即拉起 OmniSharp，会等待工程文件稳定后再启动
- 配置生效：确保 `Tools/CodeIntel/omnisharp.json` 被 OmniSharp 读取（用于排除 `Library/Temp` 等目录，减少扫描压力）

若仍出现：

- 先打开对应 `omnisharp-*.log`，搜索 `Unhandled Exception`、`MSBuild`、`ProjectManager` 相关异常
- 临时把 `autoStartOnEditorLaunch` 设为 `false`，等 Unity 完成导入/编译后手动点 `Start Services`

#### 2) 端口被占用

- 修改 `bridgePort` / `omnisharpPort`（或保持为 `0` 随机）

#### 3) 启动即退出且提示缺少 .NET Runtime

- 安装合适版本的 .NET Runtime/SDK（看日志中的 exit code 提示）
