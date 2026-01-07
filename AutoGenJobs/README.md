# AutoGen Job System

## 概述

AutoGen Job System 是一个在 Unity Editor 中常驻运行的 Job Runner，允许外部工具或本地 AI 通过写入 JSON 文件来请求 Unity 执行自动化操作。

## 版本信息

- **Runner Version**: 3
- **Schema Version**: 1

## 目录结构

```
AutoGenJobs/                    # 项目根目录下的 Job 文件目录
├── inbox/                      # 投递新 Job 的目录
├── working/                    # 正在执行的 Job
├── done/                       # 执行完成的 Job
├── results/                    # 执行结果（JSON + 日志）
├── dead/                       # 无法处理的 Job（死信）
└── examples/                   # 示例 Job 文件

Assets/Editor/AutoGenJobs/      # Unity Editor 脚本
├── Core/                       # 核心类
├── Commands/                   # 命令系统
│   └── Builtins/               # 内置命令
├── UI/                         # 用户界面
└── Tests/EditMode/             # 测试
```

## 快速开始

1. **打开 Unity 编辑器**：Runner 会自动启动并开始监听 `AutoGenJobs/inbox/` 目录

2. **投递 Job**：将 `.job.json` 文件放入 `inbox/` 目录

3. **查看结果**：在 `results/` 目录查看执行结果和日志

4. **使用菜单**：`Tools > AutoGen Jobs` 提供快捷操作

## 投递协议（重要）

外部工具投递 Job 时**必须**遵循以下协议，避免半写入问题：

1. 先写入临时文件 `*.job.json.pending`
2. 写完并 fsync 后，**原子重命名**为 `*.job.json`

Unity Runner 只扫描 `*.job.json` 文件，会忽略 `.pending` 文件。

## Job 文件格式

```json
{
  "schemaVersion": 1,
  "jobType": "AutoGen",
  "jobId": "unique_job_id",
  "createdAtUtc": "2026-01-07T12:00:00Z",
  "runnerMinVersion": 3,
  "requiresTypes": ["UnityEngine.SpriteRenderer"],
  "projectWriteRoot": "Assets/AutoGen",
  "dryRun": false,
  "commands": [
    {
      "cmd": "CreateGameObject",
      "args": { "name": "MyObject", "ensure": true },
      "out": { "go": "$myObject" }
    }
  ],
  "meta": {
    "author": "your-name",
    "note": "description"
  }
}
```

## 内置命令

| 命令 | 说明 |
|------|------|
| `ImportAssets` | 导入/刷新资产 |
| `CreateScriptableObject` | 创建 ScriptableObject |
| `CreateGameObject` | 创建 GameObject（支持 ensure 模式） |
| `AddComponent` | 添加组件 |
| `SetTransform` | 设置 Transform |
| `SetSerializedProperty` | 设置序列化属性（核心通用命令） |
| `CreateOrEditPrefab` | 创建或编辑 Prefab（嵌套隔离模式） |
| `InstantiatePrefabInScene` | 在场景中实例化 Prefab（支持 ensure 模式） |
| `SaveAssets` | 保存资产 |
| `PingObject` | 高亮显示对象 |
| `SelectObject` | 选中对象 |

## 变量引用

- 命令可以通过 `"out": { "varName": "$variable" }` 导出引用
- 后续命令通过 `{ "ref": "$variable" }` 使用引用

## Ensure 模式（幂等操作）

`CreateGameObject` 和 `InstantiatePrefabInScene` 支持 `ensure` 参数：

```json
{
  "cmd": "CreateGameObject",
  "args": {
    "name": "MyObject",
    "ensure": true,
    "ensureTag": "unique_marker_123"
  }
}
```

- `ensure: true`：如果对象已存在（通过路径或标记查找），复用而不是重复创建
- `ensureTag`：可选的唯一标记，用于更可靠的查找

这对于崩溃恢复和重复执行非常重要。

## SetSerializedProperty 支持范围

### ✅ 完全支持的类型

| 类型 | 值格式 |
|------|--------|
| `int` / `long` | `123` |
| `float` / `double` | `1.5` |
| `bool` | `true` / `false` |
| `string` | `"text"` |
| `Vector2` | `[x, y]` 或 `{"x": 0, "y": 0}` |
| `Vector3` | `[x, y, z]` 或 `{"x": 0, "y": 0, "z": 0}` |
| `Vector4` | `[x, y, z, w]` |
| `Vector2Int` / `Vector3Int` | 同上（整数） |
| `Color` | `[r, g, b, a]` 或 `{"r": 0, "g": 0, "b": 0, "a": 1}` |
| `Rect` | `[x, y, width, height]` |
| `Bounds` | `{"center": [x,y,z], "size": [x,y,z]}` |
| `Quaternion` | `[x, y, z, w]` |
| `LayerMask` | `123`（整数值） |
| `Enum` | `{"enum": "EnumType", "name": "ValueName"}` |
| `ObjectReference` | `{"assetGuid": "..."}` 或 `{"assetPath": "Assets/..."}` 或 `{"ref": "$var"}` |
| `Array` | `[element1, element2, ...]` |
| 嵌套对象 | `{"field1": value1, "field2": value2}` |

### ⚠️ 部分支持 / 需要注意

| 类型 | 说明 |
|------|------|
| `ObjectReference` 直接路径 | 使用 `{"assetPath": "..."}` 而非裸字符串 |
| `Sprite` | 使用 `{"assetPath": "Assets/xxx.png"}`，会自动加载第一个 Sprite 子资产 |

### ❌ 暂不支持的类型

以下类型目前无法通过 `SetSerializedProperty` 设置：

- `AnimationCurve`
- `Gradient`
- `ManagedReference` (SerializeReference)
- `ExposedReference<T>`
- `Hash128`
- 自定义 PropertyDrawer 的复杂类型

如需支持这些类型，请提交功能请求。

## Prefab 编辑隔离模式

`CreateOrEditPrefab` 命令使用特殊的隔离模式：

- 嵌套命令的对象查找**仅限于 Prefab 内部**
- 不会意外操作到当前场景
- 以下命令在 Prefab 编辑中被禁止：
  - `InstantiatePrefabInScene`
  - `CreateOrEditPrefab`（不允许嵌套）
  - `SaveAssets`
  - `ImportAssets`

## 崩溃恢复

系统支持崩溃恢复机制：

- 每个执行中的 Job 会写入 `.state.json` 状态文件
- 启动时检查 `working/` 目录中的遗留 Job
- 超过最大重试次数（3次）或超时（5分钟）的 Job 会进入 `dead/`
- 使用 `ensure` 模式的命令可以避免重复创建对象

## 安全策略

- 只允许写入 `Assets/AutoGen/` 下的资产
- 所有路径都会被验证和标准化
- 违规写入会被拒绝并进入死信

## 配置

通过 EditorPrefs 存储配置，可在编辑器窗口中调整：

- `EnableRunner`: 启用/禁用 Runner
- `VerboseLogging`: 详细日志
- `TickIntervalMs`: 扫描间隔（默认 200ms）
- `MaxJobsPerTick`: 每次最多处理 Job 数（默认 1）
- `AllowedWriteRoots`: 允许写入的根目录

## 错误处理

### WAITING 状态（可恢复）
- Unity 正在编译
- Runner 版本不足
- 所需类型未找到（等待编译完成）

### FAILED 状态（进入 dead/）
- JSON 解析失败
- Schema 版本不支持
- 写入路径不合法
- 命令执行失败
- 超过最大重试次数

## 菜单项

- `Tools > AutoGen Jobs > Show Window` - 打开状态窗口
- `Tools > AutoGen Jobs > Open Inbox` - 打开投递目录
- `Tools > AutoGen Jobs > Open Results` - 打开结果目录
- `Tools > AutoGen Jobs > Toggle Runner` - 启用/禁用 Runner
- `Tools > AutoGen Jobs > Process Next Job` - 手动处理一个 Job
