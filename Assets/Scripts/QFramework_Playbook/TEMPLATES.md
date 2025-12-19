# TEMPLATES.md — Code Skeletons (Project Standard)

### Attention, if you have not read AGENTS.md yet, please read AGENTS.md before reading this document

> Copy/Paste templates for this project.
> - **Single Root Architecture**: `GameRootApp : Architecture<GameRootApp>`
> - **Vertical Slice**: `Assets/Scripts/Features/[FeatureName]/...`
> - **Do NOT** write logic in generated `*.Designer.cs` / `*.Generated.cs`.

---

## 1. GameRootApp (Single Root Architecture)

**File (recommended):**
- `Assets/Scripts/Root/GameRootApp.cs`

```csharp
using QFramework;

public class GameRootApp : Architecture<GameRootApp>
{
    protected override void Init()
    {
        // Register Global Systems/Models/Utilities here.
        //
        // Example:
        // this.RegisterModel<IMyModel>(new MyModel());
        // this.RegisterSystem<IMySystem>(new MySystem());
        // this.RegisterUtility<IMyUtility>(new MyUtility());
    }
}
````

---

## 1.1 ProjectToolkitBootstrap (Startup Bootstrap / Deterministic Init)

**Purpose**

* Run project-side startup wiring **without touching** `Assets/QFramework/**`.
* Can force the RootApp to initialize early (Architecture.Init runs lazily on first `Interface` access).

**File (recommended):**

* `Assets/Scripts/Root/ProjectToolkitBootstrap.cs`

```csharp
using QFramework;
using UnityEngine;

public static class ProjectToolkitBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void BeforeSceneLoad()
    {
        // Force Root Architecture initialization early.
        // (Architecture<T>.Interface triggers Init() once.)
        _ = GameRootApp.Interface;

        // Optional (project-specific):
        // - If your project mandates explicit toolkit initialization, do it here
        //   BEFORE any usage happens in your scenes.
        //
        // Example (only if you truly need explicit init in your project):
        // ResKit.Init();
        //
        // Note:
        // - UIKit/AudioKit may already configure their loader pools via QFramework's
        //   own BeforeSceneLoad initializers when using ResKit-based defaults.
        // - Keep this file project-owned; do not modify framework sources.
    }
}
```

---

## 2. Feature Skeleton (Folder Structure)

```text
Assets/Scripts/Features/[FeatureName]/
  Commands/
  Queries/
  Events/
  Models/
  Systems/
  Utilities/
  Controllers/
  Views/

Assets/Scripts/Features/_Shared/   (optional: shared cross-feature types)
```

---

## 3. Controller Skeleton (MonoBehaviour Entry)

**File (recommended):**

* `Assets/Scripts/Features/[FeatureName]/Controllers/[FeatureName]Controller.cs`

```csharp
using QFramework;
using UnityEngine;

public class FeatureNameController : MonoBehaviour, IController
{
    public IArchitecture GetArchitecture() => GameRootApp.Interface;

    private void OnEnable()
    {
        // Example: event subscription with auto-cleanup
        // this.RegisterEvent<SomeEvent>(OnSomeEvent)
        //     .UnRegisterWhenDisabled(gameObject);
    }

    // private void OnSomeEvent(SomeEvent e) { }
}
```

---

## 4. Command Templates

### 4.1 Command (No Return)

**File (recommended):**

* `Assets/Scripts/Features/[FeatureName]/Commands/[Verb][Noun]Command.cs`

```csharp
using QFramework;

public class DoThingCommand : AbstractCommand
{
    protected override void OnExecute()
    {
        // State mutation ONLY (write path)
        // var model = this.GetModel<IMyModel>();
        // model.DoSomething();
        //
        // Notify upward via Event/BindableProperty inside Model/System if needed.
    }
}
```

### 4.2 Command (Return Value)

```csharp
using QFramework;

public class DoThingCommand : AbstractCommand<int>
{
    protected override int OnExecute()
    {
        // Perform mutation and return a result if needed.
        return 0;
    }
}
```

---

## 5. Query Templates

### 5.1 Query (Return Value)

**File (recommended):**

* `Assets/Scripts/Features/[FeatureName]/Queries/Get[Something]Query.cs`

```csharp
using QFramework;

public class GetSomethingQuery : AbstractQuery<int>
{
    protected override int OnGet()
    {
        // Read-only logic (no side effects)
        return 0;
    }
}
```

### 5.2 Calling Command/Query from a Controller / UI Panel

```csharp
// Command:
this.SendCommand<DoThingCommand>();

// Query:
var value = this.SendQuery(new GetSomethingQuery());
```

---

## 6. Model & System Skeleton

### 6.1 Model

**File (recommended):**

* `Assets/Scripts/Features/[FeatureName]/Models/I[FeatureName]Model.cs`
* `Assets/Scripts/Features/[FeatureName]/Models/[FeatureName]Model.cs`

```csharp
using QFramework;

public interface IFeatureNameModel : IModel
{
    // Expose readonly state + Bindables
    // BindableProperty<int> Count { get; }
}

public class FeatureNameModel : AbstractModel, IFeatureNameModel
{
    // Example:
    // public BindableProperty<int> Count { get; } = new BindableProperty<int>(0);

    protected override void OnInit()
    {
        // Initialize state here
    }
}
```

### 6.2 System

**File (recommended):**

* `Assets/Scripts/Features/[FeatureName]/Systems/I[FeatureName]System.cs`
* `Assets/Scripts/Features/[FeatureName]/Systems/[FeatureName]System.cs`

```csharp
using QFramework;

public interface IFeatureNameSystem : ISystem
{
}

public class FeatureNameSystem : AbstractSystem, IFeatureNameSystem
{
    protected override void OnInit()
    {
        // Cross-feature logic / orchestration here.
        // Avoid holding Controller references.
    }
}
```

---

## 7. Utility Skeleton (Infrastructure Only)

**File (recommended):**

* `Assets/Scripts/Features/[FeatureName]/Utilities/I[FeatureName]Utility.cs`
* `Assets/Scripts/Features/[FeatureName]/Utilities/[FeatureName]Utility.cs`

```csharp
using QFramework;

public interface IFeatureNameUtility : IUtility
{
}

public class FeatureNameUtility : IFeatureNameUtility
{
    // Infrastructure only: storage/network/sdk wrappers, etc.
}
```

### 7.1 SingletonKit Utility (Allowed only for low-level infra)

```csharp
using QFramework;

public class MyInfraSingleton : Singleton<MyInfraSingleton>
{
    // Infrastructure only; must NOT become a service locator across layers.
    public void Setup() { }
}
```

---

## 8. Events & Bindables

### 8.1 Event (Prefer `struct` for TypeEventSystem style)

**File (recommended):**

* `Assets/Scripts/Features/[FeatureName]/Events/[Something]ChangedEvent.cs`

```csharp
public struct SomethingChangedEvent
{
    public int Value;
}
```

### 8.2 BindableProperty (Recommended for UI binding)

```csharp
using QFramework;

// In Model:
public BindableProperty<int> Count { get; } = new BindableProperty<int>(0);

// In Controller/UI:
Count.Register(v => { /* update UI */ })
     .UnRegisterWhenGameObjectDestroyed(gameObject);
```

---

## 9. UIKit Panel Logic Skeleton (Partial Class Only)

**Files created by UIKit generator:**

* `[PanelName].cs` (write logic here)
* `[PanelName].Designer.cs` (generated; do NOT edit)

```csharp
using QFramework;
using UnityEngine;

public partial class MyUIPanel : UIPanel, IController
{
    public IArchitecture GetArchitecture() => GameRootApp.Interface;

    protected override void OnInit(IUIData uiData = null)
    {
        // Use generated fields (from Designer):
        // BtnClose.onClick.AddListener(() => CloseSelf());

        // Send command:
        // this.SendCommand<DoThingCommand>();

        // Subscribe event (auto cleanup):
        // this.RegisterEvent<SomethingChangedEvent>(e => { /* refresh */ })
        //     .UnRegisterWhenGameObjectDestroyed(gameObject);
    }

    protected override void OnOpen(IUIData uiData = null) { }
    protected override void OnShow() { }
    protected override void OnHide() { }
    protected override void OnClose() { }
}
```

> Note: Never write business logic in `*.Designer.cs` / `*.Generated.cs`.

---

## 10. RootApp Registration Snippet (Integrate a Feature)

```csharp
protected override void Init()
{
    // Model
    this.RegisterModel<IFeatureNameModel>(new FeatureNameModel());

    // System
    this.RegisterSystem<IFeatureNameSystem>(new FeatureNameSystem());

    // Utility (optional)
    // this.RegisterUtility<IFeatureNameUtility>(new FeatureNameUtility());
}
```

```
::contentReference[oaicite:1]{index=1}
```
