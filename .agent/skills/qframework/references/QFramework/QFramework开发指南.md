# QFramework 框架开发完全指南

## 📌 概述

QFramework 是一套轻量级（核心代码不到 1000 行）、渐进式、高度标准化的 Unity 游戏开发架构。它融合了 **MVC**、**CQRS（读写分离）**、**事件驱动**、**分层架构** 和 **依赖注入（IOC）** 等设计理念，旨在从每个细节上提升开发效率。

### 核心设计哲学

1. **渐进式采用**：可以根据项目规模逐步引入概念，从 BindableProperty 开始，到 Command，再到完整架构
2. **高度标准化**：统一的层级规则和通信方式，便于团队协作和项目维护
3. **读写分离（CQRS）**：Command 负责写入，Query 负责查询，Event 负责通知
4. **接口驱动**：通过接口设计模块，符合 SOLID 原则中的依赖倒置原则

---

## 🏗️ 架构分层

QFramework 将代码严格分为 **四个层级**：

```
┌───────────────────────────────────────────────────────────────┐
│                    表现层 (Controller)                         │
│            IController - MonoBehaviour / EditorWindow          │
│         • 接收输入、监听事件、更新UI、处理表现逻辑               │
├───────────────────────────────────────────────────────────────┤
│                     系统层 (System)                            │
│                ISystem - AbstractSystem                        │
│         • 承载游戏逻辑规则、跨Controller共享逻辑                 │
│         • 如：成就系统、计时系统、商城系统                       │
├───────────────────────────────────────────────────────────────┤
│                     数据层 (Model)                             │
│                 IModel - AbstractModel                         │
│         • 定义数据结构、提供数据的增删查改                       │
│         • 数据的空间共享和时间共享（存储）                       │
├───────────────────────────────────────────────────────────────┤
│                     工具层 (Utility)                           │
│                       IUtility                                 │
│         • 提供基础设施：存储、网络、SDK、第三方库集成            │
└───────────────────────────────────────────────────────────────┘
```

### 层级通信规则

| 层级 | 可访问 | 可发送 | 可监听 |
|------|--------|--------|--------|
| **Controller** | System, Model, Utility | Command, Query | Event |
| **System** | System, Model, Utility | Event | Event |
| **Model** | Utility | Event | - |
| **Utility** | - | - | - |
| **Command** | System, Model, Utility | Event, Command, Query | - |
| **Query** | System, Model | Query | - |

> [!IMPORTANT]
> **核心规则**
> - Controller 修改数据 **必须** 通过 Command
> - 下层向上层通信 **必须** 用事件
> - 上层向下层通信用方法调用（仅查询）
> - Command 和 Query **不能有状态**

---

## 🎯 核心概念详解

### 1. Architecture（架构容器）

Architecture 是模块的注册中心，所有 System、Model、Utility 都在此注册：

```csharp
public class MyGameApp : Architecture<MyGameApp>
{
    protected override void Init()
    {
        // 注册顺序：System -> Model -> Utility
        // （实际初始化顺序由框架控制：先 Model，后 System）
        
        // 注册 System
        this.RegisterSystem<IAchievementSystem>(new AchievementSystem());
        this.RegisterSystem<IScoreSystem>(new ScoreSystem());
        
        // 注册 Model
        this.RegisterModel<IPlayerModel>(new PlayerModel());
        this.RegisterModel<IInventoryModel>(new InventoryModel());
        
        // 注册 Utility
        this.RegisterUtility<IStorage>(new PlayerPrefsStorage());
        this.RegisterUtility<INetworkService>(new HttpNetworkService());
    }
    
    // 可选：覆写 ExecuteCommand 实现日志、拦截等功能
    protected override void ExecuteCommand(ICommand command)
    {
        Debug.Log($"[Command] {command.GetType().Name} 执行");
        base.ExecuteCommand(command);
    }
}
```

> [!TIP]
> Architecture 本身就是一张架构图，一目了然地展示项目有哪些模块。

### 2. Controller（表现层）

Controller 是用户看到的层级，负责：
- 接收用户输入
- 发送 Command 执行交互逻辑
- 监听事件更新界面

```csharp
public class GamePanelController : MonoBehaviour, IController
{
    // View 组件引用
    [SerializeField] private Button btnAttack;
    [SerializeField] private Text txtScore;
    
    // Model 引用（用于查询数据）
    private IPlayerModel mPlayerModel;
    
    void Start()
    {
        // 获取 Model
        mPlayerModel = this.GetModel<IPlayerModel>();
        
        // 监听输入 -> 发送 Command
        btnAttack.onClick.AddListener(() =>
        {
            this.SendCommand<AttackCommand>();
        });
        
        // 监听数据变更 -> 更新 View
        mPlayerModel.Score.RegisterWithInitValue(score =>
        {
            txtScore.text = $"分数: {score}";
        }).UnRegisterWhenGameObjectDestroyed(gameObject);
        
        // 或监听事件
        this.RegisterEvent<GameOverEvent>(e =>
        {
            Debug.Log("游戏结束");
        }).UnRegisterWhenGameObjectDestroyed(gameObject);
    }
    
    // 必须实现：指定所属架构
    public IArchitecture GetArchitecture() => MyGameApp.Interface;
}
```

### 3. Command（命令）

Command 封装交互逻辑，负责数据的 **增、删、改**：

```csharp
// 基础 Command
public class AttackCommand : AbstractCommand
{
    protected override void OnExecute()
    {
        var playerModel = this.GetModel<IPlayerModel>();
        var enemySystem = this.GetSystem<IEnemySystem>();
        
        // 修改数据
        int damage = playerModel.Attack.Value;
        enemySystem.DealDamage(damage);
        
        // 发送事件通知 UI 更新
        this.SendEvent<PlayerAttackEvent>();
    }
}

// 带参数的 Command
public class AddScoreCommand : AbstractCommand
{
    private int mScore;
    
    public AddScoreCommand(int score)
    {
        mScore = score;
    }
    
    protected override void OnExecute()
    {
        this.GetModel<IPlayerModel>().Score.Value += mScore;
    }
}

// 带返回值的 Command
public class BuyItemCommand : AbstractCommand<bool>
{
    private string mItemId;
    
    public BuyItemCommand(string itemId) => mItemId = itemId;
    
    protected override bool OnExecute()
    {
        var inventoryModel = this.GetModel<IInventoryModel>();
        var shopSystem = this.GetSystem<IShopSystem>();
        
        if (shopSystem.CanBuy(mItemId))
        {
            inventoryModel.AddItem(mItemId);
            return true;
        }
        return false;
    }
}

// 使用方式
this.SendCommand<AttackCommand>();                    // 无参数
this.SendCommand(new AddScoreCommand(100));           // 有参数
bool success = this.SendCommand(new BuyItemCommand("sword_01")); // 有返回值
```

### 4. Query（查询）

Query 负责数据的 **查**，适用于复杂的组合查询：

```csharp
public class GetTotalScoreQuery : AbstractQuery<int>
{
    protected override int OnDo()
    {
        var playerModel = this.GetModel<IPlayerModel>();
        var bonusSystem = this.GetSystem<IBonusSystem>();
        
        // 组合多个数据源
        return playerModel.BaseScore.Value + bonusSystem.GetBonus();
    }
}

// 使用方式
int totalScore = this.SendQuery(new GetTotalScoreQuery());
```

### 5. Model（数据层）

Model 定义数据结构，建议使用 [BindableProperty](file:///c:/Users/jinji/Documents/GitHub/ThatGameJam/Assets/QFramework/Framework/Scripts/QFramework.cs#694-750) 实现数据变更通知：

```csharp
// 接口定义（推荐）
public interface IPlayerModel : IModel
{
    BindableProperty<int> Score { get; }
    BindableProperty<int> Health { get; }
    BindableProperty<int> Level { get; }
}

// 实现
public class PlayerModel : AbstractModel, IPlayerModel
{
    public BindableProperty<int> Score { get; } = new BindableProperty<int>(0);
    public BindableProperty<int> Health { get; } = new BindableProperty<int>(100);
    public BindableProperty<int> Level { get; } = new BindableProperty<int>(1);
    
    protected override void OnInit()
    {
        // 从存储加载数据
        var storage = this.GetUtility<IStorage>();
        
        Score.SetValueWithoutEvent(storage.LoadInt("Score", 0));
        Level.SetValueWithoutEvent(storage.LoadInt("Level", 1));
        
        // 数据变更时自动存储
        Score.Register(value => storage.SaveInt("Score", value));
        Level.Register(value => storage.SaveInt("Level", value));
    }
    
    protected override void OnDeinit()
    {
        // 架构销毁时的清理逻辑
    }
}
```

### 6. System（系统层）

System 承载跨 Controller 共享的游戏逻辑：

```csharp
public interface IAchievementSystem : ISystem
{
    void CheckAchievements();
}

public class AchievementSystem : AbstractSystem, IAchievementSystem
{
    protected override void OnInit()
    {
        var playerModel = this.GetModel<IPlayerModel>();
        
        // 监听数据变化，触发成就检测
        playerModel.Score.Register(score =>
        {
            if (score >= 1000)
            {
                UnlockAchievement("score_1000");
            }
        });
        
        // 监听事件
        this.RegisterEvent<EnemyKilledEvent>(e =>
        {
            CheckKillAchievements(e.EnemyType);
        });
    }
    
    public void CheckAchievements()
    {
        // 主动检查成就
    }
    
    private void UnlockAchievement(string achievementId)
    {
        this.SendEvent(new AchievementUnlockedEvent { Id = achievementId });
    }
}
```

### 7. Utility（工具层）

Utility 封装基础设施，不依赖任何上层模块：

```csharp
public interface IStorage : IUtility
{
    void SaveInt(string key, int value);
    int LoadInt(string key, int defaultValue = 0);
    void SaveString(string key, string value);
    string LoadString(string key, string defaultValue = "");
}

public class PlayerPrefsStorage : IStorage
{
    public void SaveInt(string key, int value) => PlayerPrefs.SetInt(key, value);
    public int LoadInt(string key, int defaultValue = 0) => PlayerPrefs.GetInt(key, defaultValue);
    public void SaveString(string key, string value) => PlayerPrefs.SetString(key, value);
    public string LoadString(string key, string defaultValue = "") => PlayerPrefs.GetString(key, defaultValue);
}

// 切换实现只需修改注册
// this.RegisterUtility<IStorage>(new PlayerPrefsStorage());
// this.RegisterUtility<IStorage>(new EasySaveStorage()); // 换成 EasySave
```

---

## 🔧 内置工具

### 1. BindableProperty（数据绑定）

```csharp
// 创建
var health = new BindableProperty<int>(100);

// 监听变化
health.Register(newValue => Debug.Log($"血量: {newValue}"))
      .UnRegisterWhenGameObjectDestroyed(gameObject);

// 带初始值回调
health.RegisterWithInitValue(value => UpdateHealthUI(value));

// 修改值（会触发事件）
health.Value = 80;

// 修改值但不触发事件
health.SetValueWithoutEvent(100);
```

### 2. TypeEventSystem（类型事件系统）

```csharp
// 定义事件（推荐用 struct）
public struct PlayerDiedEvent
{
    public string Reason;
}

// 全局注册
TypeEventSystem.Global.Register<PlayerDiedEvent>(e =>
{
    Debug.Log($"玩家死亡: {e.Reason}");
}).UnRegisterWhenGameObjectDestroyed(gameObject);

// 发送事件
TypeEventSystem.Global.Send(new PlayerDiedEvent { Reason = "被怪物击杀" });
TypeEventSystem.Global.Send<PlayerDiedEvent>(); // 无参数发送

// 场景卸载时自动注销
TypeEventSystem.Global.Register<SomeEvent>(e => { })
    .UnRegisterWhenCurrentSceneUnloaded();
```

### 3. EasyEvent（轻量事件）

```csharp
// 比 TypeEventSystem 更轻量，适合简单场景
public class Enemy : MonoBehaviour
{
    public EasyEvent OnDied = new EasyEvent();
    public EasyEvent<int> OnDamaged = new EasyEvent<int>();
    
    public void TakeDamage(int damage)
    {
        OnDamaged.Trigger(damage);
        if (health <= 0) OnDied.Trigger();
    }
}

// 使用
enemy.OnDied.Register(() => Debug.Log("敌人死亡"))
     .UnRegisterWhenGameObjectDestroyed(gameObject);
```

### 4. IOCContainer（依赖注入容器）

```csharp
var container = new IOCContainer();

// 注册
container.Register<ILogger>(new ConsoleLogger());
container.Register(new GameConfig());

// 获取
var logger = container.Get<ILogger>();
var config = container.Get<GameConfig>();
```

---



### 代码模式

#### 模式1：简单数据绑定（推荐用于单一数据）

```csharp
// Model
public BindableProperty<int> Score { get; } = new BindableProperty<int>();

// Controller 直接监听
mModel.Score.RegisterWithInitValue(UpdateScoreUI);
```

#### 模式2：事件驱动（推荐用于复杂逻辑）

```csharp
// Command 中修改数据后发送事件
protected override void OnExecute()
{
    mModel.ComplexData = CalculateNewData();
    this.SendEvent<DataChangedEvent>();
}

// Controller 监听事件
this.RegisterEvent<DataChangedEvent>(e => RefreshUI());
```

#### 模式3：接口设计模块（推荐）

```csharp
// 始终通过接口注册和获取
this.RegisterModel<IPlayerModel>(new PlayerModel());
var model = this.GetModel<IPlayerModel>();

// 好处：方便替换实现、符合依赖倒置原则
```

---

## 🛠️ 推荐工具包

QFramework 除了核心架构外，还提供了丰富的工具包：

### 1. UIKit（界面管理）⭐⭐⭐

- 自动代码生成：根据 Prefab 层级自动生成 UI 脚本
- 界面管理：Open、Close、Stack 管理
- 层级管理：自动处理 Canvas 排序

```csharp
// 打开界面
UIKit.OpenPanel<UIGamePanel>();

// 关闭界面
UIKit.ClosePanel<UIGamePanel>();

// 传递数据
UIKit.OpenPanel<UIGamePanel>(new GamePanelData { Score = 100 });
```

### 2. ActionKit（时序动作系统）⭐⭐⭐

用于编排复杂的时序逻辑：

```csharp
// 序列执行
ActionKit.Sequence()
    .Delay(1.0f)
    .Callback(() => Debug.Log("1秒后"))
    .Delay(0.5f)
    .Callback(() => UIKit.OpenPanel<UIResultPanel>())
    .Start(this);

// 并行执行
ActionKit.Parallel()
    .Append(ActionKit.Delay(1f))
    .Append(ActionKit.Callback(() => PlaySound()))
    .Start(this);

// 重复执行
ActionKit.Repeat(3)
    .Callback(() => Debug.Log("重复3次"))
    .Start(this);
```

### 3. ResKit（资源管理）⭐⭐

- 支持 AssetBundle 和 Resources 加载
- 引用计数自动管理
- 模拟模式便于开发

```csharp
var loader = ResLoader.Allocate();

// 同步加载
var prefab = loader.LoadSync<GameObject>("PlayerPrefab");

// 异步加载
loader.LoadAsync<Sprite>("Icon", sprite => { });

// 自动释放
loader.Recycle2Cache();
```

### 4. SingletonKit（单例工具）⭐⭐

```csharp
// Mono 单例
public class GameManager : MonoSingleton<GameManager> { }

// C# 单例
public class ConfigManager : Singleton<ConfigManager> { }

// 持久化单例（跨场景不销毁）
public class AudioManager : PersistentMonoSingleton<AudioManager> { }
```

### 5. FluentAPI（链式API）⭐

```csharp
// GameObject 操作
gameObject
    .Show()
    .LocalPosition(0, 0, 0)
    .LocalScale(1)
    .Name("Player");

// Transform 操作
transform
    .Parent(parentTransform)
    .LocalIdentity()
    .DestroyChildren();
```

---

## ⚡ 快速上手流程

### Step 1：定义 Architecture

```csharp
public class MyGameApp : Architecture<MyGameApp>
{
    protected override void Init()
    {
        this.RegisterModel<IGameModel>(new GameModel());
    }
}
```

### Step 2：定义 Model

```csharp
public interface IGameModel : IModel
{
    BindableProperty<int> Score { get; }
}

public class GameModel : AbstractModel, IGameModel
{
    public BindableProperty<int> Score { get; } = new BindableProperty<int>();
    protected override void OnInit() { }
}
```

### Step 3：定义 Command

```csharp
public class AddScoreCommand : AbstractCommand
{
    private int mScore;
    public AddScoreCommand(int score) => mScore = score;
    
    protected override void OnExecute()
    {
        this.GetModel<IGameModel>().Score.Value += mScore;
    }
}
```

### Step 4：实现 Controller

```csharp
public class GameController : MonoBehaviour, IController
{
    [SerializeField] private Button btnAdd;
    [SerializeField] private Text txtScore;
    
    void Start()
    {
        var model = this.GetModel<IGameModel>();
        
        btnAdd.onClick.AddListener(() =>
        {
            this.SendCommand(new AddScoreCommand(10));
        });
        
        model.Score.RegisterWithInitValue(score =>
        {
            txtScore.text = score.ToString();
        }).UnRegisterWhenGameObjectDestroyed(gameObject);
    }
    
    public IArchitecture GetArchitecture() => MyGameApp.Interface;
}
```

---

## 📝 总结

QFramework 的核心设计思想：

1. **分层清晰**：Controller → System → Model → Utility，上层依赖下层
2. **读写分离**：Command 写，Query 读，Event 通知
3. **数据驱动**：BindableProperty 实现响应式数据绑定
4. **接口优先**：通过接口设计模块，便于替换和测试
5. **标准化**：统一的规则便于团队协作和项目维护

记住这个核心流程：

```
[用户输入] → Controller → Command → Model → Event → Controller → [UI更新]
```

掌握这套架构后，你会发现：
- 代码职责清晰，易于维护
- Bug 只会在一个 Command 里乱，不会影响全局
- 团队协作效率大幅提升
- 项目规模再大也能轻松管理

**心中有架构，代码自然美！**
