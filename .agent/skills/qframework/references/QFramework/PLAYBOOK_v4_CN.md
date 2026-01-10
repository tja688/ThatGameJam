# QFramework 项目规范手册 (QFramework Project Playbook) (v4)

## 📌 概述

QFramework 是一套轻量级（核心代码不到 1000 行）、渐进式、高度标准化的 Unity 游戏开发架构。它融合了 **MVC**、**CQRS（读写分离）**、**事件驱动**、**分层架构** 和 **依赖注入（IOC）** 等设计理念，旨在从每个细节上提升开发效率。

### 核心设计哲学

1. **渐进式采用**：可以根据项目规模逐步引入概念，从 BindableProperty 开始，到 Command，再到完整架构
2. **高度标准化**：统一的层级规则和通信方式，便于团队协作和项目维护
3. **读写分离（CQRS）**：Command 负责写入，Query 负责查询，Event 负责通知
4. **接口驱动**：通过接口设计模块，符合 SOLID 原则中的依赖倒置原则

---

### 3.3 日志与调试 (强制要求)

- **【禁止】**: 直接使用 `UnityEngine.Debug.Log`, `LogWarning`, `LogError`。
- **【必须】**: 使用 `QFramework.LogKit` API。
  - 静态写法: `LogKit.I/W/E`
  - 链式写法: `.LogInfo()/.LogWarning()/.LogError()`
- **理由**: `LogKit` 处理了 Unity 控制台堆栈跳转问题 (支持点击跳转)，并支持通过 `LogKit.Level` 进行全局日志级别过滤。

---

## 4. 项目基石 (Project foundation)

### 4.1 单根架构 (Single Root Architecture)

- **必须** 使用 **唯一** 的根架构入口：
  - `GameRootApp : Architecture<GameRootApp>`
- **禁止** 为单个功能创建多个 Root Architecture (隔离的 Demo 场景除外)。

### 4.2 基础文件的幂等性 (一次性设置)

在创建或编辑任何“基础入口”文件前，**必须** 检查它们是否已存在：

- `Assets/Scripts/Root/GameRootApp.cs`
- `Assets/Scripts/Root/ProjectToolkitBootstrap.cs`

如果已经存在：
- **必须** 复用现有文件。
- **禁止** 创建第二个承担相同职责的入口点。

### 4.3 基础文件变更准入 (何时允许修改 Root/Bootstrap)

仅在以下情况下允许编辑 `GameRootApp` 或 `ProjectToolkitBootstrap`：

1. 注册新的 **全局** 模块 (被多个功能公用的 Model/System/Utility)。
2. 调整启动时的确定性或初始化顺序。
3. 配置项目侧的工具链初始化 (无需修改 `Assets/QFramework/**`)。

**禁止** 将特定功能的业务逻辑放入 Root/Bootstrap。

## 📋 开发规范最佳实践

### 文件组织结构

```
Assets/
└── Scripts/
    └── UI                                # 复杂且变更需求大的UI逻辑 
    └── Independent                       # 特殊且独立的模块
    └── Game/
        ├── App/
        │   └── MyGameApp.cs              # Architecture 定义
        ├── Commands/
        │   ├── Player/
        │   │   ├── AttackCommand.cs
        │   │   └── MoveCommand.cs
        │   └── Game/
        │       ├── StartGameCommand.cs
        │       └── PauseGameCommand.cs
        ├── Queries/
        │   └── GetPlayerStatsQuery.cs
        ├── Events/
        │   └── GameEvents.cs             # 所有事件定义
        ├── Models/
        │   ├── IPlayerModel.cs           # 接口
        │   └── PlayerModel.cs            # 实现
        ├── Systems/
        │   ├── IAchievementSystem.cs
        │   └── AchievementSystem.cs
        ├── Utilities/
        │   ├── IStorage.cs
        │   └── PlayerPrefsStorage.cs
        └── Controllers/
            ├── UI/
            │   └── GamePanelController.cs # UI适配器层
            ├── Player/
            │   └── PlayerController.cs
            └── Camera/
                └── CameraController.cs               
```

### 命名规范

| 类型 | 命名规范 | 示例 |
|------|----------|------|
| Architecture | `XxxApp` | `CounterApp`, `MyGameApp` |
| Model 接口 | `IXxxModel` | `IPlayerModel`, `IInventoryModel` |
| System 接口 | `IXxxSystem` | `IAchievementSystem`, `IScoreSystem` |
| Utility 接口 | `IXxx` | `IStorage`, `INetworkService` |
| Command | `XxxCommand` | `AttackCommand`, `AddScoreCommand` |
| Query | `XxxQuery` / `GetXxxQuery` | `GetTotalScoreQuery` |
| Event | `XxxEvent` | `PlayerDiedEvent`, `ScoreChangedEvent` |
---

## 6. 架构规则

QFramework系统设计架构分为四层及其规则：

表现层：ViewController层。IController接口，负责接收输入和状态变化时的表现，一般情况下，MonoBehaviour 均为表现层
可以获取System
可以获取Model
可以发送Command
可以监听Event
系统层：System层。ISystem接口，帮助IController承担一部分逻辑，在多个表现层共享的逻辑，比如计时系统、商城系统、成就系统等
可以获取System
可以获取Model
可以监听Event
可以发送Event
数据层：Model层。IModel接口，负责数据的定义、数据的增删查改方法的提供
可以获取Utility
可以发送Event
工具层：Utility层。IUtility接口，负责提供基础设施，比如存储方法、序列化方法、网络连接方法、蓝牙方法、SDK、框架继承等。啥都干不了，可以集成第三方库，或者封装API
除了四个层级，还有一个核心概念——Command
可以获取System
可以获取Model
可以发送Event
可以发送Command
层级规则：
IController 更改 ISystem、IModel 的状态必须用Command
ISystem、IModel状态发生变更后通知IController必须用事件或BindableProperty
IController可以获取ISystem、IModel对象来进行数据查询
ICommand不能有状态
上层可以直接获取下层，下层不能获取上层对象
下层向上层通信用事件
上层向下层通信用方法调用（只是做查询，状态变更用Command），IController的交互逻辑为特别情况，只能用Command
---

## 7. CQRS 规则

### 7.1 状态变更路径 (强制要求)

- Command 和 Query **必须** 是 **无状态对象 (Stateless objects)**。

### 7.2 禁止直接写入 (强制要求)

- Controller 和 System **禁止** 直接设置 Model 的状态 (例如禁止使用 `BindableProperty.Value = ...`)。
- **例外**: Model 内部的 `OnInit()` 可以设置其初始值。

### 7.3 数据读取 (建议)

- 读取操作不建议全部把各种读取都通过 Query 进行，仅当发生复杂的组合式批读取逻辑存在时才进行使用（排行榜等系统）

---

## 8. 通信与生命周期

### 8.1 事件对称性 (强制要求)

每一次订阅 (Subscription) **必须** 对应一个取消订阅 (Unsubscription)。

首选模式：
- 使用 QFramework 提供的自动注销辅助工具 (`UnRegisterWhenDisabled`, `UnRegisterWhenGameObjectDestroyed`)。
- 否则，必须存储 `IUnRegister` 并在 `OnDisable` / `OnDestroy` 中调用。

### 8.2 防止泄露 (强制要求)

- 避免短生命周期对象订阅长生命周期的事件而不注销。
- 如有疑虑，请将订阅绑定到 GameObject 的生命周期。

---

## 9. 单例政策

- **【禁止】**: 使用全局“服务定位器”式的单例来绕过架构边界。
- **【允许】**: 仅在 **Utility 层内部** 使用底层基础设施单例 (例如使用 `SingletonKit` 进行缓存管理)。

## 12. 证据与 API 验证政策

当实现涉及到任何 QFramework API (类/方法/扩展/配置) 时：

**本地 API 引用 (本仓库的权威依据):**
   - QFramework_Docs_API.md
