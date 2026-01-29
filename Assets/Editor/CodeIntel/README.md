# Unity Code Intel Service（Unity 代码智能桥接服务）

把 Unity Editor 的 C# 工程语义能力（OmniSharp）通过本地 HTTP 暴露给外部 Agent/工具，用于“转定义 / 找引用 / 符号搜索”等自动化代码检索场景。

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

## HTTP API

### `GET /health`

用于探测 Bridge 是否存活、OmniSharp 是否可用、Unity 是否正在编译。

示例：

```bash
curl http://127.0.0.1:PORT/health
```

### `POST /v1/definition`

请求体：

```json
{ "file": "Assets/Scripts/Foo.cs", "line": 10, "col": 15 }
```

示例：

```bash
curl -X POST http://127.0.0.1:PORT/v1/definition ^
  -H "Content-Type: application/json" ^
  -d "{\"file\":\"Assets/Scripts/Foo.cs\",\"line\":10,\"col\":15}"
```

### `POST /v1/references`

```json
{ "file": "Assets/Scripts/Foo.cs", "line": 10, "col": 15, "includeDeclaration": true }
```

### `POST /v1/symbols`

```json
{ "query": "PlayerController" }
```

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

#### 1) Unity 显示 “Service is READY” 后又立刻退出（偶发）

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
