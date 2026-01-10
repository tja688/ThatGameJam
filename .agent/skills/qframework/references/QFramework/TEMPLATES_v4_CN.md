# TEMPLATES — 代码骨架 (Code Skeletons) (v4)

> 请先阅读 `PLAYBOOK.md`。为项目直接复制/粘贴模版。

---

## 1. GameRootApp (单根架构)

```csharp
using QFramework;

public class GameRootApp : Architecture<GameRootApp>
{
    protected override void Init()
    {
        // 在此处注册全局的 System/Model/Utility。
        //
        // 示例:
        // this.RegisterModel<IMyModel>(new MyModel());
        // this.RegisterSystem<IMySystem>(new MySystem());
        // this.RegisterUtility<IMyUtility>(new MyUtility());
    }
}
```

---

## 2. ProjectToolkitBootstrap (启动引导 / 确定性初始化)

**用途**
- 在不修改 `Assets/QFramework/**` 的情况下运行项目侧的启动配置。
- (可选) 强制根架构尽早初始化 (`Architecture<T>.Interface` 会在首次调用时延迟触发 `Init()`)。


```csharp
using QFramework;

public static class ProjectToolkitBootstrap
{
    [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void BeforeSceneLoad()
    {
        // 强制根架构尽早初始化。
        _ = GameRootApp.Interface;

        // 可选的项目侧初始化 (仅在需要时取消注释):
        // ResKit.Init();
        //
        // 可选的工具链重写 (仅在需要时添加):
        // UIKit.Config.PanelLoaderPool = ...;
        // AudioKit.Config.AudioLoaderPool = ...;
    }
}
```

### 2.1 事件：“System 请求写入” (推荐模式)

```csharp
// 针对 TypeEventSystem 风格，建议使用 struct。
public struct RequestDoThingCommandEvent
{
    public int Value;
}
```

---

### 2.2 System: 监听 → 决策 → 发送请求事件

```csharp
using QFramework;

public interface IFeatureNameSystem : ISystem
{
}

public class FeatureNameSystem : AbstractSystem, IFeatureNameSystem
{
    protected override void OnInit()
    {
        // 长生命周期：System 随根架构一同存在。
        // 在此处注册事件监听。
        this.RegisterEvent<SomeTriggerEvent>(OnSomeTriggerEvent);
    }

    private void OnSomeTriggerEvent(SomeTriggerEvent e)
    {

        this.SendEvent(new RequestDoThingCommandEvent
        {
            Value = e.Value
        });
    }
}

// 示例触发事件 (可能由其他功能模块、系统或控制器发出)
public struct SomeTriggerEvent
{
    public int Value;
}
```

---

### 2.3 Controller: 接收请求 → 发送 Command (写入路径)

```csharp
using QFramework;
using UnityEngine;

public class FeatureNameController : MonoBehaviour, IController
{
    public IArchitecture GetArchitecture() => GameRootApp.Interface;

    private void OnEnable()
    {
        this.RegisterEvent<RequestDoThingCommandEvent>(OnRequestDoThingCommand)
            .UnRegisterWhenDisabled(gameObject);
    }

    private void OnRequestDoThingCommand(RequestDoThingCommandEvent e)
    {
        // Controller 是执行 SendCommand 的安全/默认场所。
        this.SendCommand(new DoThingCommand(e.Value));
    }
}

public class DoThingCommand : AbstractCommand
{
    private readonly int _value;

    public DoThingCommand(int value) => _value = value;

    protected override void OnExecute()
    {
        // 仅限状态变更 (写入路径)
        // var model = this.GetModel<IFeatureNameModel>();
        // model.Apply(_value);
    }
}
```

---

## 4. Controller 骨架 (MonoBehaviour 入口)

```csharp
using QFramework;
using UnityEngine;

public class FeatureNameController : MonoBehaviour, IController
{
    public IArchitecture GetArchitecture() => GameRootApp.Interface;

    private void OnEnable()
    {
        // 示例: 带有自动清理逻辑的事件订阅
        // this.RegisterEvent<SomeEvent>(OnSomeEvent)
        //     .UnRegisterWhenDisabled(gameObject);
    }

    private void OnSomeEvent(SomeEvent e)
    {
    }
}
```

---

## 5. Command 模版

### 5.1 Command (无返回值)

```csharp
using QFramework;

public class DoThingCommand : AbstractCommand
{
    protected override void OnExecute()
    {
        // 仅限状态变更 (写入路径)
        // var model = this.GetModel<IMyModel>();
        // model.DoSomething();
    }
}
```

### 5.2 Command (带有返回值)

```csharp
using QFramework;

public class DoThingCommand : AbstractCommand<int>
{
    protected override int OnExecute()
    {
        // 执行变更并根据需要返回结果。
        return 0;
    }
}
```

---

## 6. Query 模版

### 6.1 Query (带有返回值)

**推荐文件路径:**
- `Assets/Scripts/Features/[FeatureName]/Queries/Get[Something]Query.cs`

```csharp
using QFramework;

public class GetSomethingQuery : AbstractQuery<int>
{
    protected override int OnGet()
    {
        // 只读逻辑 (禁止副作用)
        return 0;
    }
}
```

### 6.2 在 Controller / UI 面板中调用 Command/Query

```csharp
// 调用 Command:
this.SendCommand<DoThingCommand>();

// 调用 Query:
var value = this.SendQuery(new GetSomethingQuery());
```

---

## 7. Model & System 骨架

### 7.1 Model
```csharp
using QFramework;

public interface IFeatureNameModel : IModel
{
    // 暴露只读状态 + Bindables
    // BindableProperty<int> Count { get; }
}

public class FeatureNameModel : AbstractModel, IFeatureNameModel
{
    // 示例:
    // public BindableProperty<int> Count { get; } = new BindableProperty<int>(0);

    protected override void OnInit()
    {
        // 在此处初始化状态
    }
}
```

### 7.2 System

```csharp
using QFramework;

public interface IFeatureNameSystem : ISystem
{
}

public class FeatureNameSystem : AbstractSystem, IFeatureNameSystem
{
    protected override void OnInit()
    {
        // 在此处编写编排逻辑。避免持有 Controller 的引用。
    }
}
```

---

## 8. Utility 骨架 (仅限基础设施)

```csharp
using QFramework;

public interface IFeatureNameUtility : IUtility
{
}

public class FeatureNameUtility : IFeatureNameUtility
{
    // 仅限基础设施逻辑：存储/网络/SDK 包装等。
}
```

### 8.1 SingletonKit Utility (仅限底层基础设施单例)

```csharp
using QFramework;

public class MyInfraSingleton : Singleton<MyInfraSingleton>
{
    // 仅限基础设施使用；禁止成为跨层的“服务定位器”。
    public void Setup() { }
}
```

---

## 9. 消息总线与数据绑定 (Events & Bindables)

### 9.1 Event (针对 TypeEventSystem 风格首选 `struct`)

```csharp
public struct SomethingChangedEvent
{
    public int Value;
}
```

### 9.2 BindableProperty (UI 绑定的推荐方式)

```csharp
using QFramework;

// 在 Model 中:
public BindableProperty<int> Count { get; } = new BindableProperty<int>(0);

// 在 Controller/UI 中:
Count.Register(v => { /* 更新 UI */ })
     .UnRegisterWhenGameObjectDestroyed(gameObject);
```

---

## 11. RootApp 注册代码片段 (集成功能)

```csharp
protected override void Init()
{
    // Model 层注册
    this.RegisterModel<IFeatureNameModel>(new FeatureNameModel());

    // System 层注册
    this.RegisterSystem<IFeatureNameSystem>(new FeatureNameSystem());

    // Utility 层注册 (可选)
    // this.RegisterUtility<IFeatureNameUtility>(new FeatureNameUtility());
}
```
