# AutoGen Job System 迁移与部署指南

## 📦 打包结构

完整的 AutoGen Job System 已打包为标准 Unity Package：

```
Packages/com.autogen.jobs/           ← 核心 Package（必需）
├── package.json                      
├── README.md                         
├── Editor/                           ← Unity Editor 代码
│   ├── AutoGen.Jobs.Editor.asmdef
│   ├── Core/                         ← 10 个核心脚本
│   ├── Commands/                     ← 命令接口和内置命令
│   ├── Setup/                        ← 初始化脚本
│   └── UI/                           ← 编辑器界面
├── Samples~/                         ← 示例（不自动导入）
│   ├── Examples/                     ← 示例 Job + Python SDK
│   └── Skills/                       ← Agent Skills 文档
└── Documentation~/                   ← 文档
```

## 🚀 部署到新项目

### 方式 1：复制 Package（推荐）

1. **复制 Package 目录**
   ```
   从: Packages/com.autogen.jobs/
   到: 新项目/Packages/com.autogen.jobs/
   ```

2. **打开 Unity，等待编译**

3. **初始化工作区**
   - 菜单: `Tools > AutoGen Jobs > Initialize Workspace`
   - 这会自动创建所需的目录结构

4. **安装 Skills**
   - 菜单: `Tools > AutoGen Jobs > Install Skills`
   - 或手动复制 `Samples~/Skills/` 到项目根 `.agent/skills/`

### 方式 2：Git 子模块

```bash
# 在新项目中添加子模块
git submodule add https://github.com/your-repo/autogen-jobs.git Packages/com.autogen.jobs
```

### 方式 3：Unity Package Manager (UPM)

在 `Packages/manifest.json` 中添加：

```json
{
  "dependencies": {
    "com.autogen.jobs": "https://github.com/your-repo/autogen-jobs.git?path=Packages/com.autogen.jobs"
  }
}
```

## 📁 运行时工作目录

这些目录在项目运行时需要存在，由 `Initialize Workspace` 自动创建：

```
项目根目录/
├── AutoGenJobs/                ← Job 文件系统（项目根目录下）
│   ├── inbox/                  ← AI 投递 Job 的目录
│   ├── working/                ← 执行中的 Job
│   ├── done/                   ← 已完成的 Job
│   ├── results/                ← 执行结果和日志
│   ├── dead/                   ← 失败的 Job
│   └── examples/               ← 示例文件
│
├── Assets/
│   └── AutoGen/                ← 自动生成的资产（Assets 内）
│       ├── Prefabs/
│       └── Configs/
│
└── .agent/
    └── skills/                 ← Agent Skills（可选，用于 AI 集成）
```

**注意**：
- `AutoGenJobs/` 在项目根目录，不在 Assets 内（避免 Unity 导入 JSON）
- `Assets/AutoGen/` 在 Assets 内（用于存放生成的资产）
- 这两个目录可以加入 `.gitignore`（除了 examples）

## 🔧 依赖项

Package 依赖 `Newtonsoft.Json`，会自动添加到项目：

```json
// 在 package.json 中已声明
"dependencies": {
  "com.unity.nuget.newtonsoft-json": "3.2.1"
}
```

## ✅ 验证安装

1. **检查 Runner 状态**
   - 菜单: `Tools > AutoGen Jobs > Show Status`
   - 应显示: Status = "Running"

2. **检查命令注册**
   - 菜单: `Tools > AutoGen Jobs > List Commands`
   - 应列出 11 个内置命令

3. **测试执行**
   - 复制 `AutoGenJobs/examples/example_hello.job.json` 到 `AutoGenJobs/inbox/`
   - 几秒后场景中应出现 `HelloAutoGen` 对象

## 📋 快速清单

### 必需文件（Package 核心）
- [x] `Packages/com.autogen.jobs/` - 完整目录

### 运行时目录（Initialize Workspace 创建）
- [x] `AutoGenJobs/inbox/`
- [x] `AutoGenJobs/working/`
- [x] `AutoGenJobs/done/`
- [x] `AutoGenJobs/results/`
- [x] `AutoGenJobs/dead/`
- [x] `Assets/AutoGen/`

### AI 集成（可选）
- [ ] `.agent/skills/*.md`
- [ ] `.cursor/rules/unity-autogen.mdc`

## 🗑️ 清理旧文件

如果你从开发项目迁移，可以删除以下文件（已整合到 Package）：

```
可删除:
├── Assets/Editor/AutoGenJobs/     ← 已移入 Package
│
保留（Package 之外）:
├── AutoGenJobs/                   ← 运行时工作目录，保留
│   ├── inbox/
│   ├── working/
│   └── ...
├── Assets/AutoGen/                ← 运行时资产目录，保留
└── .agent/skills/                 ← Skills 文档，保留
```

## 📝 .gitignore 建议

```gitignore
# AutoGen 运行时文件（可选忽略）
AutoGenJobs/working/
AutoGenJobs/done/
AutoGenJobs/results/
AutoGenJobs/dead/

# 保留 inbox 和 examples（方便协作）
!AutoGenJobs/inbox/
!AutoGenJobs/examples/

# 生成的资产（可选忽略，看团队需求）
# Assets/AutoGen/
```

## 🔄 从当前项目迁移

如果你在 `Assets/Editor/AutoGenJobs/` 有旧代码：

1. 确认 `Packages/com.autogen.jobs/` 已包含所有最新代码
2. 删除 `Assets/Editor/AutoGenJobs/` 目录
3. 删除 `Assets/Editor/AutoGenJobs.meta`
4. Unity 重新编译
5. 验证 Runner 正常运行

## 🌐 发布为独立仓库

如果你想将 Package 发布为独立仓库：

```bash
# 创建新仓库
mkdir autogen-jobs-package
cd autogen-jobs-package
git init

# 复制 Package 内容
cp -r 原项目/Packages/com.autogen.jobs/* .

# 提交
git add .
git commit -m "Initial release"
git remote add origin https://github.com/你的用户名/autogen-jobs-package.git
git push -u origin main
```

然后其他项目可以通过 UPM Git URL 安装：
```
https://github.com/你的用户名/autogen-jobs-package.git
```
