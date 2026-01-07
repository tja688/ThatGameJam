# AutoGen Job System

## 概述

AutoGen Job System 是一个在 Unity Editor 中常驻运行的 Job Runner，允许外部工具或本地 AI 通过写入 JSON 文件来请求 Unity 执行自动化操作。

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
│   ├── AutoGenSettings.cs      # 配置设置
│   ├── JobRunner.cs            # 主运行器
│   ├── JobQueue.cs             # Job 队列管理
│   ├── JobModels.cs            # Job 数据模型
│   ├── JobResultModels.cs      # 结果数据模型
│   ├── TypeResolver.cs         # 类型解析
│   ├── AssetRef.cs             # Asset 引用解析
│   ├── PathUtil.cs             # 路径工具
│   ├── Logging.cs              # 日志系统
│   └── Guards.cs               # 执行门槛检查
├── Commands/                   # 命令系统
│   ├── IJobCommand.cs          # 命令接口
│   ├── CommandRegistry.cs      # 命令注册表
│   └── Builtins/               # 内置命令
│       ├── Cmd_ImportAssets.cs
│       ├── Cmd_CreateScriptableObject.cs
│       ├── Cmd_CreateGameObject.cs
│       ├── Cmd_AddComponent.cs
│       ├── Cmd_SetTransform.cs
│       ├── Cmd_SetSerializedProperty.cs
│       ├── Cmd_CreateOrEditPrefab.cs
│       ├── Cmd_InstantiatePrefabInScene.cs
│       ├── Cmd_SaveAssets.cs
│       ├── Cmd_PingObject.cs
│       └── SerializedPropertyHelper.cs
├── UI/                         # 用户界面
│   ├── MenuItems.cs            # 菜单项
│   └── AutoGenJobsWindow.cs    # 编辑器窗口
└── Tests/                      # 测试
    └── EditMode/
        └── JobRunnerTests.cs
```

## 快速开始

1. **打开 Unity 编辑器**：Runner 会自动启动并开始监听 `AutoGenJobs/inbox/` 目录

2. **投递 Job**：将 `.job.json` 文件放入 `inbox/` 目录

3. **查看结果**：在 `results/` 目录查看执行结果和日志

4. **使用菜单**：`Tools > AutoGen Jobs` 提供快捷操作

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
      "args": { "name": "MyObject" },
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
| `CreateGameObject` | 创建 GameObject |
| `AddComponent` | 添加组件 |
| `SetTransform` | 设置 Transform |
| `SetSerializedProperty` | 设置序列化属性（核心通用命令） |
| `CreateOrEditPrefab` | 创建或编辑 Prefab |
| `InstantiatePrefabInScene` | 在场景中实例化 Prefab |
| `SaveAssets` | 保存资产 |
| `PingObject` | 高亮显示对象 |
| `SelectObject` | 选中对象 |

## 变量引用

- 命令可以通过 `"out": { "varName": "$variable" }` 导出引用
- 后续命令通过 `{ "ref": "$variable" }` 使用引用

## 值类型支持

`SetSerializedProperty` 支持以下值类型：

- 基本类型：`int`, `float`, `bool`, `string`
- 向量：`Vector2`, `Vector3`, `Vector4` - 使用数组 `[x, y, z]` 或对象 `{"x": 0, "y": 0}`
- 颜色：`Color` - 使用数组 `[r, g, b, a]` 或对象 `{"r": 0, "g": 0, "b": 0, "a": 1}`
- 枚举：`{ "enum": "EnumType", "name": "ValueName" }`
- Asset 引用：`{ "assetGuid": "..." }` 或 `{ "assetPath": "Assets/..." }`
- 变量引用：`{ "ref": "$variable" }`
- 数组：`[element1, element2, ...]`

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

## 投递协议

外部工具投递 Job 时应：

1. 先写入临时文件 `*.pending`
2. 写完后原子重命名为 `*.job.json`

Unity Runner 只扫描 `*.job.json` 文件。
