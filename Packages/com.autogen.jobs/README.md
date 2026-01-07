# AutoGen Job System

Unity Editor 自动化 Job 系统，允许外部 AI/工具通过 JSON 文件远程操控 Unity Editor。

## 功能特性

- 🎮 创建/编辑 GameObject（场景对象）
- 📦 创建/编辑 Prefab（预制体）
- 📋 创建 ScriptableObject（配置资产）
- ⚙️ 设置任意序列化属性
- 🔗 链式任务（通过变量引用）
- 🛡️ 安全：限制写入路径、崩溃恢复、幂等操作

## 安装方式

### 方式 1：Unity Package Manager（推荐）

1. 打开 Unity Package Manager (`Window > Package Manager`)
2. 点击 `+` > `Add package from disk...`
3. 选择 `Packages/com.autogen.jobs/package.json`

或者通过 Git URL：
```
https://github.com/your-repo/autogen-jobs.git?path=Packages/com.autogen.jobs
```

### 方式 2：手动复制

1. 将 `Packages/com.autogen.jobs/` 复制到目标项目的 `Packages/` 目录

## 安装后配置

安装包后，还需要配置工作目录和 Skills：

### 1. 初始化工作目录

在 Unity 菜单中执行：`Tools > AutoGen Jobs > Initialize Workspace`

或手动创建以下目录结构：

```
项目根目录/
├── AutoGenJobs/
│   ├── inbox/      ← AI 投递 Job 的目录
│   ├── working/    ← 执行中的 Job
│   ├── done/       ← 已完成的 Job
│   ├── results/    ← 执行结果和日志
│   └── dead/       ← 失败的 Job
│
└── Assets/
    └── AutoGen/    ← 自动生成的资产目录
        ├── Prefabs/
        └── Configs/
```

### 2. 安装 Agent Skills

从 Package Samples 中导入 Skills：

1. 在 Package Manager 中选择 `AutoGen Job System`
2. 展开 `Samples` 部分
3. 点击 `Import` 导入 Skills

或手动复制 Skills 文件到项目根目录：

```
从: Packages/com.autogen.jobs/Samples~/Skills/
到: .agent/skills/
```

### 3. 验证安装

1. 重启 Unity Editor
2. 打开 `Tools > AutoGen Jobs > Show Window`
3. 确认 Status 显示 "Running"
4. 执行 `Tools > AutoGen Jobs > List Commands` 查看已注册命令

## 快速测试

1. 打开 `Tools > AutoGen Jobs > Open Inbox`
2. 复制以下内容保存为 `test.job.json`：

```json
{
  "schemaVersion": 1,
  "jobId": "test_001",
  "projectWriteRoot": "Assets/AutoGen",
  "commands": [
    {
      "cmd": "CreateGameObject",
      "args": { "name": "AutoGen_Test", "ensure": true }
    }
  ]
}
```

3. 回到 Unity，几秒后场景中应出现 `AutoGen_Test` 对象
4. 在 `AutoGenJobs/results/test_001.result.json` 查看执行结果

## 目录结构说明

```
Packages/com.autogen.jobs/
├── package.json                 ← 包配置
├── Editor/                      ← Unity Editor 脚本
│   ├── AutoGen.Jobs.Editor.asmdef
│   ├── Core/                    ← 核心类
│   │   ├── JobRunner.cs         ← 主运行器
│   │   ├── JobQueue.cs          ← 队列管理
│   │   ├── JobModels.cs         ← 数据模型
│   │   └── ...
│   ├── Commands/                ← 命令系统
│   │   ├── IJobCommand.cs       ← 命令接口
│   │   ├── CommandRegistry.cs   ← 命令注册
│   │   └── Builtins/            ← 内置命令
│   └── UI/                      ← 编辑器界面
├── Samples~/                    ← 示例（需手动导入）
│   ├── Examples/                ← 示例 Job 和 Python SDK
│   └── Skills/                  ← Agent Skills 文档
└── Documentation~/              ← 文档
```

## 内置命令

| 命令 | 说明 |
|------|------|
| `CreateGameObject` | 创建场景对象 |
| `AddComponent` | 添加组件 |
| `SetTransform` | 设置变换 |
| `SetSerializedProperty` | 设置任意属性 |
| `CreateScriptableObject` | 创建 SO 资产 |
| `CreateOrEditPrefab` | 创建/编辑预制体 |
| `InstantiatePrefabInScene` | 实例化到场景 |
| `SaveAssets` | 保存资产 |
| `ImportAssets` | 导入资产 |
| `PingObject` | 高亮对象 |
| `SelectObject` | 选中对象 |

## 许可证

MIT License
